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
/// Controller-Tests fuer den <see cref="FaCompletionController"/> nach dem v1.22.0-Umbau
/// auf FaWorkSteps + Merkmale + Werkbank. Verwendet echte Repositories + InMemory-DbContext,
/// weil Edit Navigation-Properties (WorkStep, Specs, Article) braucht — Moq waere
/// unverhaeltnismaessig.
/// </summary>
public class FaCompletionControllerTests
{
    private static (ApplicationDbContext ctx, FaCompletionController ctrl, Mock<ICurrentUserService> userMock) Build()
    {
        var ctx = TestDbContextFactory.Create();
        var prodRepo = new ProductionOrderRepository(ctx);
        var faWorkStepRepo = new FaWorkStepRepository(ctx);
        var workStepRepo = new WorkStepRepository(ctx);
        var attrRepo = new FaAttributeRepository(ctx);
        var workplaceRepo = new ProductionWorkplaceRepository(ctx);
        var enaioRepo = new EnaioDmsDocumentRepository(ctx);

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(x => x.GetDisplayName()).Returns("Max Mustermann");
        userMock.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\max");

        var ctrl = new FaCompletionController(
            prodRepo, faWorkStepRepo, workStepRepo, attrRepo, workplaceRepo, enaioRepo, userMock.Object);

        ctrl.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (ctx, ctrl, userMock);
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

    private static FaWorkStep SeedFaWorkStep(
        ApplicationDbContext ctx, int productionOrderId, int workStepId,
        bool isCompleted = false, bool isRemoved = false, bool isSpecComplete = false)
    {
        var row = new FaWorkStep
        {
            ProductionOrderId = productionOrderId,
            WorkStepId = workStepId,
            IsCompleted = isCompleted,
            IsSpecComplete = isSpecComplete,
            IsRemoved = isRemoved,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaWorkSteps.Add(row);
        ctx.SaveChanges();
        return row;
    }

    private static FaWorkStepSpec SeedSpec(ApplicationDbContext ctx, int faWorkStepId, string description)
    {
        var spec = new FaWorkStepSpec
        {
            FaWorkStepId = faWorkStepId,
            Description = description,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaWorkStepSpecs.Add(spec);
        ctx.SaveChanges();
        return spec;
    }

    // ---------------------------------------------------------------- Index

    [Fact]
    public async Task Index_ReturnsListWithCompletionSummary()
    {
        var (ctx, ctrl, _) = Build();
        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");

        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 3);

        // CompletedCount-Spalte zeigt seit v1.22.0 den Spec-Fertig-Fortschritt (IsSpecComplete).
        SeedFaWorkStep(ctx, o2.Order.Id, vk.Id, isSpecComplete: true);
        var vlRow = SeedFaWorkStep(ctx, o2.Order.Id, vl.Id);
        SeedSpec(ctx, vlRow.Id, "Lueftermotor");

        // Entfernte Zeile (IsRemoved=1) inkl. Spec darf NICHT mitzaehlen.
        var removedRow = SeedFaWorkStep(ctx, o2.Order.Id, ve.Id, isRemoved: true);
        SeedSpec(ctx, removedRow.Id, "Alt");

        var result = await ctrl.Index(null, null, null, false);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<FaCompletionListViewModel>().Subject;
        vm.Items.Should().HaveCount(2);

        var item2 = vm.Items.First(i => i.OrderNumber == "FA-002");
        item2.ApplicableCount.Should().Be(2);
        item2.CompletedCount.Should().Be(1);
        item2.SpecCount.Should().Be(1);

        var item1 = vm.Items.First(i => i.OrderNumber == "FA-001");
        item1.ApplicableCount.Should().Be(0);
        item1.CompletedCount.Should().Be(0);
        item1.SpecCount.Should().Be(0);
        _ = o1;
    }

    [Fact]
    public async Task Index_SetsHasNoWorkplaceFlag()
    {
        var (ctx, ctrl, _) = Build();
        var withWp = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-WP");
        var withoutWp = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-NOWP");

        var wp = new ProductionWorkplace
        {
            Name = "Werkbank 1",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        withWp.Order.ProductionWorkplaceId = wp.Id;
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index(null, null, null, false);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.First(i => i.OrderNumber == "FA-WP").HasNoWorkplace.Should().BeFalse();
        vm.Items.First(i => i.OrderNumber == "FA-NOWP").HasNoWorkplace.Should().BeTrue();
        _ = withoutWp;
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var (ctx, ctrl, _) = Build();
        var kuehlung = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-KUEHL");
        var montage = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-MONT");
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-NOWP");

        var wp1 = new ProductionWorkplace
        {
            Name = "Werkbank Kuehlung",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        var wp2 = new ProductionWorkplace
        {
            Name = "Montage",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        await ctx.SaveChangesAsync();

        kuehlung.Order.ProductionWorkplaceId = wp1.Id;
        montage.Order.ProductionWorkplaceId = wp2.Id;
        await ctx.SaveChangesAsync();

        // Filter auf Werkbank-Namen
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_workbench=Kuehlung");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index(null, null, null, false);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-KUEHL");
        vm.Items.Single().WorkplaceName.Should().Be("Werkbank Kuehlung");
        vm.Pagination.TotalCount.Should().Be(1);

        // Filter auf den gerenderten Badge-Text "Keine Werkbank"
        var httpCtx2 = new DefaultHttpContext();
        httpCtx2.Request.QueryString = new QueryString("?colf_workbench=keine%20werkbank");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx2 };

        var result2 = await ctrl.Index(null, null, null, false);

        var vm2 = (FaCompletionListViewModel)((ViewResult)result2).Model!;
        vm2.Items.Should().HaveCount(1);
        vm2.Items.Single().OrderNumber.Should().Be("FA-NOWP");
    }

    [Fact]
    public async Task Index_FiltersByOrderNumber_ReturnsOnlyMatching()
    {
        var (ctx, ctrl, _) = Build();
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002");
        TestDataHelper.CreateOrderWithStatuses(ctx, "WA-003");

        var result = await ctrl.Index("FA", null, null, false);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(2);
        vm.Items.Select(i => i.OrderNumber).Should().BeEquivalentTo(new[] { "FA-001", "FA-002" });
    }

    [Fact]
    public async Task Index_HidesDoneOrdersByDefault()
    {
        var (ctx, ctrl, _) = Build();
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-OPEN", isDone: false);
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-DONE", isDone: true);

        var result = await ctrl.Index(null, null, null, showDone: false);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task Index_HidesKommDoneOrders()
    {
        // "Erledigt" = IsDone (Sage) ODER IsDonePicking (App-Komm-erledigt) — wie FA-Liste.
        var (ctx, ctrl, _) = Build();
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-OPEN", isDone: false, isDonePicking: false);
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-KOMMDONE", isDone: false, isDonePicking: true);
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-SAGEDONE", isDone: true, isDonePicking: false);

        var result = await ctrl.Index(null, null, null, showDone: false);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task Index_ShowsKommDone_WhenShowDone()
    {
        var (ctx, ctrl, _) = Build();
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-OPEN", isDone: false, isDonePicking: false);
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-KOMMDONE", isDone: false, isDonePicking: true);
        TestDataHelper.CreateOrderWithStatuses(ctx, "FA-SAGEDONE", isDone: true, isDonePicking: false);

        var result = await ctrl.Index(null, null, null, showDone: true);

        var vm = (FaCompletionListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(3);
        vm.Items.Select(i => i.OrderNumber)
            .Should().BeEquivalentTo(new[] { "FA-OPEN", "FA-KOMMDONE", "FA-SAGEDONE" });
    }

    // ----------------------------------------------------------------- Edit

    [Fact]
    public async Task Edit_LoadsActiveFaWorkStepsWithAttributes()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-100");

        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 3);

        SeedFaWorkStep(ctx, o.Order.Id, vk.Id);
        SeedFaWorkStep(ctx, o.Order.Id, vl.Id);
        SeedFaWorkStep(ctx, o.Order.Id, ve.Id, isRemoved: true);

        // Werkbank-Stammdaten + Zuordnung am FA
        var wp = new ProductionWorkplace
        {
            Name = "Werkbank 1",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        o.Order.ProductionWorkplaceId = wp.Id;

        // Merkmal "Verdampfergroesse" (Dropdown) dem VK zugeordnet, Wert gesetzt
        var def = new FaAttributeDefinition
        {
            Name = "Verdampfergroesse",
            AttributeType = AttributeType.Dropdown,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeDefinitions.Add(def);
        await ctx.SaveChangesAsync();

        var opt = new FaAttributeOption
        {
            FaAttributeDefinitionId = def.Id,
            Value = "UKW 3/1",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeOptions.Add(opt);
        ctx.FaAttributeWorkSteps.Add(new FaAttributeWorkStep
        {
            FaAttributeDefinitionId = def.Id,
            WorkStepId = vk.Id
        });
        await ctx.SaveChangesAsync();

        ctx.FaAttributeValues.Add(new FaAttributeValue
        {
            ProductionOrderId = o.Order.Id,
            FaAttributeDefinitionId = def.Id,
            SelectedOptionId = opt.Id,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await ctrl.Edit(o.Order.Id, tab: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;

        // Nur aktive Zeilen als Tabs, SortOrder-sortiert; Default-Tab = erster Code
        vm.Tabs.Should().HaveCount(2);
        vm.Tabs.Select(t => t.Code).Should().ContainInOrder("VK", "VL");
        vm.ActiveTab.Should().Be("VK");

        // Werkbank-Daten
        vm.ProductionWorkplaceId.Should().Be(wp.Id);
        vm.AvailableWorkplaces.Should().ContainSingle(w => w.Name == "Werkbank 1");

        // "AG hinzufuegen": nur noch nicht aktive WorkSteps (VE ist IsRemoved => hinzufuegbar)
        vm.AvailableWorkSteps.Select(w => w.Code).Should().BeEquivalentTo(new[] { "VE" });

        // Merkmal am VK-Tab inkl. Optionen + aktuellem Wert; VL-Tab ohne Merkmale
        var vkTab = vm.Tabs.First(t => t.Code == "VK");
        var attr = vkTab.Attributes.Should().ContainSingle().Subject;
        attr.DefinitionId.Should().Be(def.Id);
        attr.Name.Should().Be("Verdampfergroesse");
        attr.AttributeType.Should().Be(AttributeType.Dropdown);
        attr.Options.Should().ContainSingle(x => x.Value == "UKW 3/1");
        attr.SelectedOptionId.Should().Be(opt.Id);

        vm.Tabs.First(t => t.Code == "VL").Attributes.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_TabParam_SelectsCorrectActiveTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-101");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        SeedFaWorkStep(ctx, o.Order.Id, vk.Id);
        SeedFaWorkStep(ctx, o.Order.Id, vl.Id);

        var result = await ctrl.Edit(o.Order.Id, tab: "VL");

        var vm = (FaCompletionEditViewModel)((ViewResult)result).Model!;
        vm.ActiveTab.Should().Be("VL");
    }

    [Fact]
    public async Task Edit_InvalidId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.Edit(999_999, tab: null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_InvalidTab_FallsBackToFirstTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-102");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        SeedFaWorkStep(ctx, o.Order.Id, vk.Id);

        var result = await ctrl.Edit(o.Order.Id, tab: "BLA");

        var vm = (FaCompletionEditViewModel)((ViewResult)result).Model!;
        vm.ActiveTab.Should().Be("VK");
    }

    [Fact]
    public async Task Edit_WithoutFaWorkSteps_ReturnsEmptyTabs()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-LEER");
        SeedWorkStep(ctx, "VK", "Kuehlung", 1);

        var result = await ctrl.Edit(o.Order.Id, tab: null);

        var vm = (FaCompletionEditViewModel)((ViewResult)result).Model!;
        vm.Tabs.Should().BeEmpty();
        vm.AvailableWorkSteps.Should().ContainSingle(w => w.Code == "VK");
    }

    // --------------------------------------------------------- SetWorkplace

    [Fact]
    public async Task SetWorkplace_UpdatesProductionOrder()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-WB");
        var wp = new ProductionWorkplace
        {
            Name = "Werkbank 2",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var result = await ctrl.SetWorkplace(o.Order.Id, wp.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.ProductionOrders.Find(o.Order.Id)!;
        reloaded.ProductionWorkplaceId.Should().Be(wp.Id);
        reloaded.ModifiedBy.Should().Be("Max Mustermann");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\max");
    }

    [Fact]
    public async Task SetWorkplace_Null_ClearsAssignment()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-WBNULL");
        var wp = new ProductionWorkplace
        {
            Name = "Werkbank 3",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        o.Order.ProductionWorkplaceId = wp.Id;
        await ctx.SaveChangesAsync();

        await ctrl.SetWorkplace(o.Order.Id, null);

        ctx.ChangeTracker.Clear();
        ctx.ProductionOrders.Find(o.Order.Id)!.ProductionWorkplaceId.Should().BeNull();
    }

    [Fact]
    public async Task SetWorkplace_UnknownOrder_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.SetWorkplace(999_999, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    // --------------------------------------------------- SaveAttributeValue

    [Fact]
    public async Task SaveAttributeValue_UpsertsValue()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ATTR");
        var def = new FaAttributeDefinition
        {
            Name = "Leitungsausgang",
            AttributeType = AttributeType.Dropdown,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeDefinitions.Add(def);
        await ctx.SaveChangesAsync();
        var opt = new FaAttributeOption
        {
            FaAttributeDefinitionId = def.Id,
            Value = "Standard",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeOptions.Add(opt);
        await ctx.SaveChangesAsync();

        var result = await ctrl.SaveAttributeValue(o.Order.Id, def.Id, opt.Id, null);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        var value = ctx.FaAttributeValues.Single();
        value.ProductionOrderId.Should().Be(o.Order.Id);
        value.FaAttributeDefinitionId.Should().Be(def.Id);
        value.SelectedOptionId.Should().Be(opt.Id);
        value.CreatedBy.Should().Be("Max Mustermann");
    }

    [Fact]
    public async Task SaveAttributeValue_RedirectsToSameTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ATTRTAB");
        var def = new FaAttributeDefinition
        {
            Name = "Leitungsausgang",
            AttributeType = AttributeType.Dropdown,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeDefinitions.Add(def);
        await ctx.SaveChangesAsync();
        var opt = new FaAttributeOption
        {
            FaAttributeDefinitionId = def.Id,
            Value = "Standard",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeOptions.Add(opt);
        await ctx.SaveChangesAsync();

        var result = await ctrl.SaveAttributeValue(o.Order.Id, def.Id, opt.Id, null, tab: "VE");

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);
        redirect.RouteValues!["tab"].Should().Be("VE");
    }

    [Fact]
    public async Task SaveAttributeValue_BothNull_RemovesValueRow()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ATTRCLR");
        var def = new FaAttributeDefinition
        {
            Name = "Ventil aussenliegend",
            AttributeType = AttributeType.Boolean,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.FaAttributeDefinitions.Add(def);
        await ctx.SaveChangesAsync();
        ctx.FaAttributeValues.Add(new FaAttributeValue
        {
            ProductionOrderId = o.Order.Id,
            FaAttributeDefinitionId = def.Id,
            BooleanValue = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        await ctrl.SaveAttributeValue(o.Order.Id, def.Id, null, null);

        ctx.FaAttributeValues.Should().BeEmpty();
    }

    // ------------------------------------------------ AddWorkStep/RemoveWorkStep

    [Fact]
    public async Task AddWorkStep_ActivatesWorkStepForOrder()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ADDWS");
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);

        var result = await ctrl.AddWorkStep(o.Order.Id, vl.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");

        var row = ctx.FaWorkSteps.Single(f => f.ProductionOrderId == o.Order.Id && f.WorkStepId == vl.Id);
        row.IsRemoved.Should().BeFalse();
        row.Source.Should().Be(FaWorkStepSources.Manual);
    }

    [Fact]
    public async Task RemoveWorkStep_DeactivatesRow()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-REMWS");
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        SeedFaWorkStep(ctx, o.Order.Id, vl.Id);

        var result = await ctrl.RemoveWorkStep(o.Order.Id, vl.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        ctx.ChangeTracker.Clear();
        var row = ctx.FaWorkSteps.Single(f => f.ProductionOrderId == o.Order.Id && f.WorkStepId == vl.Id);
        row.IsRemoved.Should().BeTrue();
    }

    // ------------------------------------------------------------- Spec-CRUD

    [Fact]
    public async Task AddSpec_HappyPath_PersistsSpecWithAuditFields_AndRedirectsToCorrectTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ADD");
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        var vlRow = SeedFaWorkStep(ctx, o.Order.Id, vl.Id);

        var form = new FaWorkStepSpecFormModel
        {
            FaWorkStepId = vlRow.Id,
            Description = "Lueftermotor",
            Quantity = 2.000m,
            SortOrder = 10
        };

        var result = await ctrl.AddSpec(form);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VL");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        var spec = ctx.FaWorkStepSpecs.Single();
        spec.Description.Should().Be("Lueftermotor");
        spec.Quantity.Should().Be(2m);
        spec.FaWorkStepId.Should().Be(vlRow.Id);
        spec.CreatedBy.Should().Be("Max Mustermann");
        spec.CreatedByWindows.Should().Be("DOMAIN\\max");
    }

    [Fact]
    public async Task AddSpec_EmptyDescription_SetsWarningAndRedirects()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ADDEMPTY");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        var vkRow = SeedFaWorkStep(ctx, o.Order.Id, vk.Id);

        var form = new FaWorkStepSpecFormModel
        {
            FaWorkStepId = vkRow.Id,
            Description = "   ",
            SortOrder = 0
        };

        var result = await ctrl.AddSpec(form);

        result.Should().BeOfType<RedirectToActionResult>();
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.FaWorkStepSpecs.Should().BeEmpty();
    }

    [Fact]
    public async Task AddSpec_UnknownFaWorkStep_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var form = new FaWorkStepSpecFormModel
        {
            FaWorkStepId = 999_999,
            Description = "X",
            SortOrder = 0
        };

        var result = await ctrl.AddSpec(form);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EditSpec_PersistsChanges_AndSetsModifiedAuditFields_AndRedirectsWithTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-EDIT");
        var ve = SeedWorkStep(ctx, "VE", "Elektro", 3);
        var veRow = SeedFaWorkStep(ctx, o.Order.Id, ve.Id);
        var spec = SeedSpec(ctx, veRow.Id, "Old");

        var form = new FaWorkStepSpecFormModel
        {
            Id = spec.Id,
            FaWorkStepId = veRow.Id,
            Description = "Geaendert",
            Quantity = 4m,
            SortOrder = 12,
            Notes = "Hinweis"
        };

        var result = await ctrl.EditSpec(form);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VE");

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.FaWorkStepSpecs.Single();
        reloaded.Description.Should().Be("Geaendert");
        reloaded.Quantity.Should().Be(4m);
        reloaded.SortOrder.Should().Be(12);
        reloaded.Notes.Should().Be("Hinweis");
        reloaded.ModifiedAt.Should().NotBeNull();
        reloaded.ModifiedBy.Should().Be("Max Mustermann");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\max");
    }

    [Fact]
    public async Task EditSpec_UnknownId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var form = new FaWorkStepSpecFormModel
        {
            Id = 999_999,
            FaWorkStepId = 1,
            Description = "X",
            SortOrder = 0
        };

        var result = await ctrl.EditSpec(form);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSpec_RemovesAndRedirectsToTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-DEL");
        var vt = SeedWorkStep(ctx, "VT", "Tueren", 4);
        var vtRow = SeedFaWorkStep(ctx, o.Order.Id, vt.Id);
        var spec = SeedSpec(ctx, vtRow.Id, "Zum Loeschen");

        var result = await ctrl.DeleteSpec(spec.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VT");
        ctx.FaWorkStepSpecs.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSpec_UnknownId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.DeleteSpec(999_999);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ------------------------------------------------------ ToggleSpecComplete

    [Fact]
    public async Task ToggleSpecComplete_True_SetsSpecCompletedAtAndBy_AndRedirectsToTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-TOGGLE");
        var vk = SeedWorkStep(ctx, "VK", "Kuehlung", 1);
        var vkRow = SeedFaWorkStep(ctx, o.Order.Id, vk.Id);

        var result = await ctrl.ToggleSpecComplete(vkRow.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VK");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.FaWorkSteps.Find(vkRow.Id)!;
        reloaded.IsSpecComplete.Should().BeTrue();
        reloaded.SpecCompletedAt.Should().NotBeNull();
        reloaded.SpecCompletedBy.Should().Be("Max Mustermann");
        // Arbeit-erledigt (IsCompleted) bleibt unberuehrt
        reloaded.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleSpecComplete_FlipsValue_OnSecondCall()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-TOGGLE2");
        var vl = SeedWorkStep(ctx, "VL", "Lueftung", 2);
        var vlRow = SeedFaWorkStep(ctx, o.Order.Id, vl.Id);

        // First call -> true
        await ctrl.ToggleSpecComplete(vlRow.Id);
        // Second call -> false
        await ctrl.ToggleSpecComplete(vlRow.Id);

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.FaWorkSteps.Find(vlRow.Id)!;
        reloaded.IsSpecComplete.Should().BeFalse();
        reloaded.SpecCompletedAt.Should().BeNull();
        reloaded.SpecCompletedBy.Should().BeNull();
    }

    [Fact]
    public async Task ToggleSpecComplete_UnknownId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.ToggleSpecComplete(999_999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
