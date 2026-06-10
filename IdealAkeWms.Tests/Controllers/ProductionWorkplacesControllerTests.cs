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

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");
        userSvc.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        var appSettings = new Mock<IAppSettingRepository>();
        appSettings.Setup(a => a.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var controller = new ProductionWorkplacesController(repo, userRepo.Object, userSvc.Object, appSettings.Object, ctx);
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
