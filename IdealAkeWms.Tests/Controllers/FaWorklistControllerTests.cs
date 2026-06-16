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

/// <summary>
/// Controller-Tests fuer die FA-Abarbeitungsliste (<see cref="FaWorklistController"/>, v1.22.0).
/// Filter = EIN Arbeitsgang (statt Werkbank), Werkbank ist Info-Spalte.
/// Echte Repositories + InMemory-DbContext (Muster FaCompletionControllerTests), weil
/// die Worklist Navigation-Properties (WorkStep, Specs, SelectedOption) braucht.
/// </summary>
public class FaWorklistControllerTests
{
    private static (ApplicationDbContext ctx, FaWorklistController ctrl, Mock<ICurrentUserService> userMock) Build()
    {
        var ctx = TestDbContextFactory.Create();

        // Feature-Gate: FaCompletionAktiv muss true sein, sonst redirect AccessDenied.
        ctx.AppSettings.Add(new AppSetting
        {
            Key = AppSettingKeys.FaCompletionAktiv,
            Value = "true",
            Description = "test"
        });
        ctx.SaveChanges();

        var prodRepo = new ProductionOrderRepository(ctx);
        var faWorkStepRepo = new FaWorkStepRepository(ctx);
        var workStepRepo = new WorkStepRepository(ctx);
        var attrRepo = new FaAttributeRepository(ctx);
        var settingsRepo = new AppSettingRepository(ctx);
        var holidayRepo = new HolidayRepository(ctx);
        var enaioRepo = new EnaioDmsDocumentRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var articleAttrRepo = new ArticleAttributeRepository(ctx);
        var userRepo = new UserRepository(ctx);
        var workplaceRepo = new ProductionWorkplaceRepository(ctx);

        // BOM kommt aus SAGE/OSEON (Dapper) -> Mock mit einer Position.
        var bomMock = new Mock<IBomRepository>();
        bomMock.Setup(b => b.GetBomItemsAsync(It.IsAny<string>()))
            .ReturnsAsync(new BomQueryResult(new List<BomItem>
            {
                new()
                {
                    Artikelnummer = "ART-1",
                    Position = "10",
                    Ressourcenummer = "RES-1",
                    Bezeichnung1 = "Teil 1",
                    Menge = 2m
                }
            }, "SAGE"));

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(x => x.GetDisplayName()).Returns("Max Mustermann");
        userMock.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\max");
        userMock.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns((int?)null);

        var readOnlyBomBuilder = new ReadOnlyBomBuilder(
            prodRepo, bomMock.Object, stockRepo, articleAttrRepo, userRepo);

        var ctrl = new FaWorklistController(
            prodRepo, faWorkStepRepo, workStepRepo, attrRepo, userRepo,
            workplaceRepo, settingsRepo, holidayRepo, new BusinessDayService(), enaioRepo,
            readOnlyBomBuilder, userMock.Object);

        ctrl.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (ctx, ctrl, userMock);
    }

    private static ProductionWorkplace SeedWorkplace(ApplicationDbContext ctx, string name)
    {
        var wp = new ProductionWorkplace
        {
            Name = name,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        return wp;
    }

    private static WorkStep SeedWorkStep(ApplicationDbContext ctx, string code, string name, int sortOrder = 0)
    {
        var ws = new WorkStep
        {
            Code = code,
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.WorkSteps.Add(ws);
        ctx.SaveChanges();
        return ws;
    }

    private static User SeedUser(ApplicationDbContext ctx, string name,
        int? defaultWorkStepId = null, int? defaultWorkplaceId = null)
    {
        var user = new User
        {
            Name = name,
            DefaultWorkStepId = defaultWorkStepId,
            DefaultWorkplaceId = defaultWorkplaceId,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user;
    }

    private static FaWorkStep SeedFaWorkStep(
        ApplicationDbContext ctx, int productionOrderId, int workStepId,
        bool isCompleted = false, bool isRemoved = false)
    {
        var row = new FaWorkStep
        {
            ProductionOrderId = productionOrderId,
            WorkStepId = workStepId,
            IsCompleted = isCompleted,
            IsRemoved = isRemoved,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaWorkSteps.Add(row);
        ctx.SaveChanges();
        return row;
    }

    [Fact]
    public async Task Index_FiltersByWorkStep_AcrossWorkplaces()
    {
        var (ctx, ctrl, _) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);

        // Zwei FAs auf VERSCHIEDENEN Werkbaenken, beide mit aktivem VE-FaWorkStep.
        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o1.Order.Id, vl.Id); // zusaetzlicher AG -> irrelevant fuer VE-Filter

        var result = await ctrl.Index(ve.Id);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<FaWorklistViewModel>().Subject;

        vm.SelectedWorkStepId.Should().Be(ve.Id);
        vm.SelectedWorkStep!.Code.Should().Be("VE");
        vm.Items.Should().HaveCount(2);
        vm.Items.Select(i => i.OrderNumber).Should().BeEquivalentTo(new[] { "FA-001", "FA-002" });
        vm.Items.Select(i => i.WorkplaceName).Should().BeEquivalentTo(new[] { "Werkbank 1", "Werkbank 2" });
        vm.Items.Should().OnlyContain(i => i.WorkStepCell != null);
        vm.Pagination.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Index_FiltersByWorkplace_WhenSet()
    {
        // Zwei FAs gleicher AG (VE), verschiedene Werkbaenke. Mit workplaceId-Filter
        // (UND zum AG-Filter) bleibt nur die FA auf der gewaehlten Werkbank.
        var (ctx, ctrl, _) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        var result = await ctrl.Index(ve.Id, workplaceId: wp1.Id);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.SelectedWorkplaceId.Should().Be(wp1.Id);
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-001");
        vm.Items.Single().WorkplaceName.Should().Be("Werkbank 1");
        vm.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_UsesUserDefaultWorkplace_WhenNoParam()
    {
        // Ohne ?workplaceId greift User.DefaultWorkplaceId als Zusatzfilter.
        var (ctx, ctrl, userMock) = Build();
        var wp1 = SeedWorkplace(ctx, "Werkbank 1");
        var wp2 = SeedWorkplace(ctx, "Werkbank 2");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var user = SeedUser(ctx, "vorbau1", defaultWorkStepId: ve.Id, defaultWorkplaceId: wp2.Id);
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        o1.Order.ProductionWorkplaceId = wp1.Id;
        o2.Order.ProductionWorkplaceId = wp2.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        // Aufruf OHNE workStepId/workplaceId -> beide Defaults aus dem User.
        var result = await ctrl.Index(null);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.SelectedWorkStepId.Should().Be(ve.Id);
        vm.SelectedWorkplaceId.Should().Be(wp2.Id);
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-002");
    }

    [Fact]
    public async Task Index_HidesWorkDone_UnlessShowDone()
    {
        var (ctx, ctrl, _) = Build();
        var wp = SeedWorkplace(ctx, "Werkbank 1");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var open = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-OPEN");
        var done = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-DONE");
        open.Order.ProductionWorkplaceId = wp.Id;
        done.Order.ProductionWorkplaceId = wp.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, open.Order.Id, ve.Id, isCompleted: false);
        SeedFaWorkStep(ctx, done.Order.Id, ve.Id, isCompleted: true);

        // Default: erledigter AG ausgeblendet
        var result = await ctrl.Index(ve.Id);
        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-OPEN");

        // showDone=true: beide sichtbar
        var resultShowDone = await ctrl.Index(ve.Id, showDone: true);
        var vmShowDone = (FaWorklistViewModel)((ViewResult)resultShowDone).Model!;
        vmShowDone.Items.Should().HaveCount(2);
        vmShowDone.Items.Select(i => i.OrderNumber)
            .Should().BeEquivalentTo(new[] { "FA-OPEN", "FA-DONE" });
    }

    [Fact]
    public async Task Index_HidesKommDoneOrders()
    {
        // FA mit aktivem (offenem) AG, aber komm-erledigt (IsDonePicking=true)
        // -> wie FA-Liste IMMER ausblenden, auch ohne showDone.
        var (ctx, ctrl, _) = Build();
        var wp = SeedWorkplace(ctx, "Werkbank 1");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var open = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-OPEN", isDonePicking: false);
        var kommDone = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-KOMMDONE", isDonePicking: true);
        open.Order.ProductionWorkplaceId = wp.Id;
        kommDone.Order.ProductionWorkplaceId = wp.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, open.Order.Id, ve.Id, isCompleted: false);
        SeedFaWorkStep(ctx, kommDone.Order.Id, ve.Id, isCompleted: false);

        var result = await ctrl.Index(ve.Id);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task Index_UsesUserDefaultWorkStep_WhenNoParam()
    {
        var (ctx, ctrl, userMock) = Build();
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);
        var user = SeedUser(ctx, "vorbau1", defaultWorkStepId: ve.Id);
        userMock.Setup(x => x.GetCurrentAppUserId()).Returns(user.Id);

        var open = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-DEFAULT");
        SeedFaWorkStep(ctx, open.Order.Id, ve.Id);

        // Aufruf OHNE workStepId -> Default aus User.DefaultWorkStepId.
        var result = await ctrl.Index(null);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.SelectedWorkStepId.Should().Be(ve.Id);
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-DEFAULT");
    }

    [Fact]
    public async Task Index_NoWorkStep_RendersEmptyList()
    {
        // Weder ?workStepId noch User-Default -> nur Dropdown, leere Liste.
        var (ctx, ctrl, _) = Build();
        SeedWorkStep(ctx, "VE", "Elektro", 1);

        var result = await ctrl.Index(null);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.SelectedWorkStepId.Should().BeNull();
        vm.AvailableWorkSteps.Should().ContainSingle(w => w.Code == "VE");
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var (ctx, ctrl, _) = Build();
        var wp = SeedWorkplace(ctx, "Werkbank 1");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 1);

        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-100");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "WA-200");
        o1.Order.ProductionWorkplaceId = wp.Id;
        o2.Order.ProductionWorkplaceId = wp.Id;
        ctx.SaveChanges();

        SeedFaWorkStep(ctx, o1.Order.Id, ve.Id);
        SeedFaWorkStep(ctx, o2.Order.Id, ve.Id);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_order-number=FA-100");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index(ve.Id);

        var vm = (FaWorklistViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-100");
        vm.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Bom_ReturnsReadOnlyViewModel()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-BOM", qty: 1m, articleNumber: "ART-1");

        var result = await ctrl.Bom(o.Order.Id, null);

        // Rendert die Picking-Stuecklisten-View, aber mit ReadOnly-Flag (Spec §7).
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.ViewName.Should().Be("~/Views/Picking/Bom.cshtml");
        var vm = view.Model.Should().BeOfType<BomViewModel>().Subject;
        vm.ReadOnly.Should().BeTrue();
        vm.ProductionOrderId.Should().Be(o.Order.Id);
        vm.OrderNumber.Should().Be("FA-BOM");
        vm.Items.Should().HaveCount(1);
        // Keine Picking-Daten im Read-only-Modus: kein PickingItem, kein Lagerplatz-Suggest.
        vm.Items.Single().PickingItemId.Should().BeNull();
        vm.Items.Single().SourceStorageLocationId.Should().BeNull();
    }
}
