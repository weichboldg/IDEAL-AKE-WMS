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
/// Controller-Tests fuer den neuen <see cref="FaCompletionController"/> (Phase 4 / v1.13.0).
/// Spec 12.1. Verwendet echte Repositories + InMemory-DbContext, weil die Edit-Action
/// Navigation-Properties (Article, AssemblyGroup) braucht — Moq waere unverhaeltnismaessig.
/// </summary>
public class FaCompletionControllerTests
{
    private static (ApplicationDbContext ctx, FaCompletionController ctrl, Mock<ICurrentUserService> userMock) Build()
    {
        var ctx = TestDbContextFactory.Create();
        var prodRepo = new ProductionOrderRepository(ctx);
        var grpRepo = new ProductionOrderAssemblyGroupRepository(ctx);
        var specRepo = new ProductionOrderAssemblyGroupSpecRepository(ctx);

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(x => x.GetDisplayName()).Returns("Max Mustermann");
        userMock.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\max");

        var ctrl = new FaCompletionController(prodRepo, grpRepo, specRepo, userMock.Object);

        ctrl.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (ctx, ctrl, userMock);
    }

    [Fact]
    public async Task Index_ReturnsListWithCompletionSummary()
    {
        var (ctx, ctrl, _) = Build();
        var o1 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-001");
        var o2 = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-002",
            applicableGroups: new Dictionary<string, bool> { { "VK", true }, { "VL", true } });

        // Mark VK of o2 as completed
        var o2Vk = o2.Groups.First(g => g.GroupKey == "VK");
        o2Vk.IsCompleted = true;
        o2Vk.CompletedAt = DateTime.UtcNow;
        o2Vk.CompletedBy = "max";
        // Add one spec for o2/VK
        ctx.ProductionOrderAssemblyGroupSpecs.Add(new ProductionOrderAssemblyGroupSpec
        {
            AssemblyGroupId = o2Vk.Id,
            Description = "Kuehlteil 1",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

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
    public async Task Edit_ReturnsViewModelWith5Tabs_AndDefaultActiveTabVk()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-100");

        var result = await ctrl.Edit(o.Order.Id, tab: null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var vm = view.Model.Should().BeOfType<FaCompletionEditViewModel>().Subject;
        vm.ActiveTab.Should().Be("VK");
        vm.Tabs.Should().HaveCount(5);
        vm.Tabs.Select(t => t.GroupKey).Should().BeEquivalentTo(new[] { "VK", "VL", "VE", "VT", "VA" });
    }

    [Fact]
    public async Task Edit_TabParam_SelectsCorrectActiveTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-101");

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
    public async Task Edit_InvalidTab_FallsBackToVK()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-102");

        var result = await ctrl.Edit(o.Order.Id, tab: "BLA");

        var vm = (FaCompletionEditViewModel)((ViewResult)result).Model!;
        vm.ActiveTab.Should().Be("VK");
    }

    [Fact]
    public async Task AddSpec_HappyPath_PersistsSpecWithAuditFields_AndRedirectsToCorrectTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ADD");
        var vlGroup = o.Groups.First(g => g.GroupKey == "VL");

        var form = new AssemblyGroupSpecFormModel
        {
            AssemblyGroupId = vlGroup.Id,
            Description = "Lueftermotor",
            Quantity = 2.000m,
            SortOrder = 10
        };

        var result = await ctrl.AddSpec(form);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VL");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        var spec = ctx.ProductionOrderAssemblyGroupSpecs.Single();
        spec.Description.Should().Be("Lueftermotor");
        spec.Quantity.Should().Be(2m);
        spec.AssemblyGroupId.Should().Be(vlGroup.Id);
        spec.CreatedBy.Should().Be("Max Mustermann");
        spec.CreatedByWindows.Should().Be("DOMAIN\\max");
    }

    [Fact]
    public async Task AddSpec_EmptyDescription_SetsWarningAndRedirects()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-ADDEMPTY");
        var vkGroup = o.Groups.First(g => g.GroupKey == "VK");

        var form = new AssemblyGroupSpecFormModel
        {
            AssemblyGroupId = vkGroup.Id,
            Description = "   ",
            SortOrder = 0
        };

        var result = await ctrl.AddSpec(form);

        result.Should().BeOfType<RedirectToActionResult>();
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.ProductionOrderAssemblyGroupSpecs.Should().BeEmpty();
    }

    [Fact]
    public async Task AddSpec_UnknownAssemblyGroup_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var form = new AssemblyGroupSpecFormModel
        {
            AssemblyGroupId = 999_999,
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
        var veGroup = o.Groups.First(g => g.GroupKey == "VE");

        var spec = new ProductionOrderAssemblyGroupSpec
        {
            AssemblyGroupId = veGroup.Id,
            Description = "Old",
            Quantity = 1m,
            SortOrder = 5,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "init",
            CreatedByWindows = "init"
        };
        ctx.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await ctx.SaveChangesAsync();

        var form = new AssemblyGroupSpecFormModel
        {
            Id = spec.Id,
            AssemblyGroupId = veGroup.Id,
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
        var reloaded = ctx.ProductionOrderAssemblyGroupSpecs.Single();
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

        var form = new AssemblyGroupSpecFormModel
        {
            Id = 999_999,
            AssemblyGroupId = 1,
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
        var vtGroup = o.Groups.First(g => g.GroupKey == "VT");

        var spec = new ProductionOrderAssemblyGroupSpec
        {
            AssemblyGroupId = vtGroup.Id,
            Description = "Zum Loeschen",
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await ctx.SaveChangesAsync();

        var result = await ctrl.DeleteSpec(spec.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VT");
        ctx.ProductionOrderAssemblyGroupSpecs.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSpec_UnknownId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.DeleteSpec(999_999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToggleIsCompleted_True_SetsCompletedAtAndBy_AndRedirectsToTab()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-TOGGLE");
        var vkGroup = o.Groups.First(g => g.GroupKey == "VK");

        var result = await ctrl.ToggleIsCompleted(vkGroup.Id);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.RouteValues!["tab"].Should().Be("VK");
        redirect.RouteValues!["id"].Should().Be(o.Order.Id);

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.ProductionOrderAssemblyGroups.Find(vkGroup.Id)!;
        reloaded.IsCompleted.Should().BeTrue();
        reloaded.CompletedAt.Should().NotBeNull();
        reloaded.CompletedBy.Should().Be("Max Mustermann");
    }

    [Fact]
    public async Task ToggleIsCompleted_FlipsValue_OnSecondCall()
    {
        var (ctx, ctrl, _) = Build();
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-TOGGLE2");
        var vlGroup = o.Groups.First(g => g.GroupKey == "VL");

        // First call -> true
        await ctrl.ToggleIsCompleted(vlGroup.Id);
        // Second call -> false
        await ctrl.ToggleIsCompleted(vlGroup.Id);

        ctx.ChangeTracker.Clear();
        var reloaded = ctx.ProductionOrderAssemblyGroups.Find(vlGroup.Id)!;
        reloaded.IsCompleted.Should().BeFalse();
        reloaded.CompletedAt.Should().BeNull();
        reloaded.CompletedBy.Should().BeNull();
    }

    [Fact]
    public async Task ToggleIsCompleted_UnknownId_ReturnsNotFound()
    {
        var (_, ctrl, _) = Build();

        var result = await ctrl.ToggleIsCompleted(999_999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
