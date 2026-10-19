using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class ProductionWorkplacesControllerTests
{
    private static ProductionWorkplacesController CreateController(ApplicationDbContext ctx)
    {
        var repo = new ProductionWorkplaceRepository(ctx);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.GetActiveUsersAsync()).ReturnsAsync(new List<User>());

        var workStepRepo = new WorkStepRepository(ctx);

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        userSvc.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        var appSettings = new Mock<IAppSettingRepository>();
        appSettings.Setup(a => a.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var controller = new ProductionWorkplacesController(repo, userRepo.Object, workStepRepo, userSvc.Object, appSettings.Object, ctx);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Edit_PersistsBdeUseCustomShiftPlan()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace
        {
            Name = "WB",
            BdeAktiv = true,
            BdeUseCustomShiftPlan = false,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = wp.Id,
            Name = wp.Name,
            BdeAktiv = true,
            BdeUseCustomShiftPlan = true
        };
        await controller.Edit(wp.Id, vm);

        var updated = await ctx.ProductionWorkplaces.FindAsync(wp.Id);
        updated!.BdeUseCustomShiftPlan.Should().BeTrue();
    }

    [Fact]
    public async Task Edit_SyncsWorkStepAssignments()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace { Name = "WB", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.WorkSteps.AddRange(
            new WorkStep { Id = 10, Code = "VK", Name = "Kuehlung" },
            new WorkStep { Id = 11, Code = "VL", Name = "Lueftung" });
        await ctx.SaveChangesAsync();
        ctx.ProductionWorkplaceWorkSteps.Add(new ProductionWorkplaceWorkStep { ProductionWorkplaceId = wp.Id, WorkStepId = 10 });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var vm = new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = wp.Name };

        await controller.Edit(wp.Id, vm, new[] { 11 });

        var junctions = ctx.ProductionWorkplaceWorkSteps.Where(x => x.ProductionWorkplaceId == wp.Id).ToList();
        junctions.Should().ContainSingle(x => x.WorkStepId == 11);
        junctions.Single().CreatedBy.Should().Be("tester");
    }

    // ----- v1.63.0 (Spec 2026-10-19-bde-stufe-1, FA-a8 AK 19/20, FA-c1 AK 25, FA-b11 AK 9) -----

    private static ProductionWorkplace Wp(string name, string? sageNr = null, string? code = null) => new()
    {
        Name = name, SageArbeitsplatznummer = sageNr, ArbeitsschrittCode = code, BdeAktiv = true,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
    };

    [Fact]
    public async Task Edit_NonSageWorkplace_SavesTrimmedCode_EmptyIsNull_AndOption_AK19()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp("Entgraten 2 (Test)");
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        await controller.Edit(wp.Id, new ProductionWorkplaceEditViewModel
            { Id = wp.Id, Name = wp.Name, BdeAktiv = true, ArbeitsschrittCode = " EG ", BdeNurAgFertigmeldung = true });
        ctx.ChangeTracker.Clear();
        var saved = await ctx.ProductionWorkplaces.SingleAsync();
        saved.ArbeitsschrittCode.Should().Be("EG");
        saved.BdeNurAgFertigmeldung.Should().BeTrue();
        saved.ModifiedBy.Should().Be("tester");

        await CreateController(ctx).Edit(wp.Id, new ProductionWorkplaceEditViewModel { Id = wp.Id, Name = wp.Name, ArbeitsschrittCode = "  " });
        ctx.ChangeTracker.Clear();
        (await ctx.ProductionWorkplaces.SingleAsync()).ArbeitsschrittCode.Should().BeNull();
    }

    [Fact]
    public async Task Create_SavesCodeAndOption_DefaultOff_AK19_AK25()
    {
        var ctx = TestDbContextFactory.Create();

        await CreateController(ctx).Create(new ProductionWorkplaceEditViewModel { Name = "Neu", ArbeitsschrittCode = " KA" });
        await CreateController(ctx).Create(new ProductionWorkplaceEditViewModel { Name = "Ohne" });

        var all = await ctx.ProductionWorkplaces.AsNoTracking().ToListAsync();
        all.Single(w => w.Name == "Neu").ArbeitsschrittCode.Should().Be("KA");
        all.Single(w => w.Name == "Ohne").ArbeitsschrittCode.Should().BeNull();
        all.Should().OnlyContain(w => !w.BdeNurAgFertigmeldung && w.SageArbeitsplatznummer == null);
    }

    [Fact]
    public async Task Edit_SageWorkplace_ManipulatedPost_LeavesCodeAndSageNumberUnchanged_AK20()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = Wp("Entgraten Fläche", "140", "EG");
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        // readonly entfernt, Kuerzel geaendert UND Sage-Nummer im Formular geleert
        await CreateController(ctx).Edit(wp.Id, new ProductionWorkplaceEditViewModel
            { Id = wp.Id, Name = wp.Name, BdeAktiv = true, ArbeitsschrittCode = "XX", SageArbeitsplatznummer = null });

        ctx.ChangeTracker.Clear();
        var saved = await ctx.ProductionWorkplaces.SingleAsync();
        saved.ArbeitsschrittCode.Should().Be("EG");
        saved.SageArbeitsplatznummer.Should().Be("140");
    }

    [Fact]
    public async Task Index_SageMissingFilter_NotEmpty_ReturnsOnlyMarked_BeforePagination_AK9()
    {
        var ctx = TestDbContextFactory.Create();
        var alt = Wp("Flächen entgr beendet", "2150", "EG");
        alt.SageMissingSince = new DateTime(2026, 10, 19);
        ctx.ProductionWorkplaces.AddRange(alt, Wp("Entgraten Fläche", "140", "EG"), Wp("AKE"));
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_sage-missing=" + Uri.EscapeDataString("!(leer)"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var model = (await controller.Index(pageSize: 1) as ViewResult)!.Model as List<ProductionWorkplace>;

        model!.Should().ContainSingle(w => w.SageArbeitsplatznummer == "2150");
        (controller.ViewBag.Pagination as PaginationState)!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Name = "Werkbank Kuehlung", Hall = "H1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplace { Name = "Werkbank Elektrik", Hall = "H2", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplace { Name = "Montage", Hall = "H3", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_name=Elektrik");
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await controller.Index() as ViewResult;

        var model = result!.Model as List<ProductionWorkplace>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Werkbank Elektrik");
        var pagination = controller.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
