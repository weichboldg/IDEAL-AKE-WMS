using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WarehouseRequisitionsControllerTests
{
    private static (WarehouseRequisitionsController ctrl, ApplicationDbContext ctx, int userId) Setup(int? defaultRecipientGroupId = null)
    {
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "tester", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u);
        ctx.SaveChanges();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
        currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetIntValueAsync("DefaultLagerbestellempfaengerId", 0))
                .ReturnsAsync(defaultRecipientGroupId ?? 0);

        var workplaces = new ProductionWorkplaceRepository(ctx);
        var requisitions = new WarehouseRequisitionRepository(ctx);
        var groups = new OrderRecipientRepository(ctx);

        var ctrl = new WarehouseRequisitionsController(requisitions, workplaces, groups, currentUser.Object, settings.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx, u.Id);
    }

    [Fact]
    public async Task CreateDraft_NoWorkplaceAssigned_ReturnsErrorAndRedirects()
    {
        var (ctrl, _, _) = Setup();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result.Should().NotBeNull();
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDraft_OneWorkplace_AutoSelectsAndCreates()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Edit");
        ctx.WarehouseRequisitions.Should().ContainSingle();
        var created = ctx.WarehouseRequisitions.First();
        created.ProductionWorkplaceId.Should().Be(wp.Id);
        created.CreatedByUserId.Should().Be(userId, "stabiler User-Filter via CreatedByUserId");
    }

    [Fact]
    public async Task CreateDraft_TwoWorkplaces_NoSelection_RedirectsToIndexWithChoice()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp1 = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp2 = new ProductionWorkplace { Name = "WB-B", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        ctx.ProductionWorkplaceUsers.AddRange(
            new ProductionWorkplaceUser { UserId = userId, ProductionWorkplaceId = wp1.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionWorkplaceUser { UserId = userId, ProductionWorkplaceId = wp2.Id, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var result = await ctrl.CreateDraft(workplaceId: null) as RedirectToActionResult;

        result!.ActionName.Should().Be("Index");
        ctx.WarehouseRequisitions.Should().BeEmpty("ohne Auswahl wird kein Draft angelegt");
    }

    [Fact]
    public async Task Submit_NoItems_RejectsWithWarning()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        };
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Submit(r.Id) as RedirectToActionResult;

        result!.ActionName.Should().Be("Edit");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.WarehouseRequisitions.First().Status.Should().Be(WarehouseRequisitionStatus.Draft, "kein Submit ohne Items");
    }

    [Fact]
    public async Task Submit_NoDefaultRecipientSetting_RejectsWithWarning()
    {
        var (ctrl, ctx, userId) = Setup(defaultRecipientGroupId: 0);
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var r = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester" };
        r.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 1, Position = 1,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Submit(r.Id) as RedirectToActionResult;

        result!.ActionName.Should().Be("Edit");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        ctx.WarehouseRequisitions.First().Status.Should().Be(WarehouseRequisitionStatus.Draft);
    }

    [Fact]
    public async Task Index_ShowsMissingPartsCard_WhenUserHasFinalShortages()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });

        // Zwei abgeschlossene Bestellungen mit insgesamt drei Final-Shortage-Items.
        var r1 = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Closed,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester",
            CreatedByUserId = userId,
        };
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 1, Position = 1,
            ShortageStatus = ShortageStatus.NoRestock,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-2", ArticleDescription = "y", QuantityRequested = 1, Position = 2,
            ShortageStatus = ShortageStatus.NoRestock,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        var r2 = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Closed,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester",
            CreatedByUserId = userId,
        };
        r2.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-3", ArticleDescription = "z", QuantityRequested = 1, Position = 1,
            ShortageStatus = ShortageStatus.NoRestock,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        ctx.WarehouseRequisitions.AddRange(r1, r2);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index() as ViewResult;

        result.Should().NotBeNull();
        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm.Should().NotBeNull();
        vm!.MissingPartsNoRestockItemCount.Should().Be(3);
        vm.MissingPartsNoRestockRequisitionCount.Should().Be(2);
    }

    [Fact]
    public async Task Index_HidesMissingPartsCard_WhenNoShortages()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index() as ViewResult;

        result.Should().NotBeNull();
        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm.Should().NotBeNull();
        vm!.MissingPartsWaitingItemCount.Should().Be(0);
        vm.MissingPartsWaitingRequisitionCount.Should().Be(0);
        vm.MissingPartsNoRestockItemCount.Should().Be(0);
        vm.MissingPartsNoRestockRequisitionCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_ShowsWaitingCounts_WhenUserHasWillBeRestockedItems()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });

        // PartiallyDelivered Bestellung mit 2 WillBeRestocked-Items.
        var r1 = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.PartiallyDelivered,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester",
            CreatedByUserId = userId,
        };
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 1, Position = 1,
            ShortageStatus = ShortageStatus.WillBeRestocked,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-2", ArticleDescription = "y", QuantityRequested = 1, Position = 2,
            ShortageStatus = ShortageStatus.WillBeRestocked,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        ctx.WarehouseRequisitions.Add(r1);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index() as ViewResult;

        result.Should().NotBeNull();
        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm.Should().NotBeNull();
        vm!.MissingPartsWaitingItemCount.Should().Be(2);
        vm.MissingPartsWaitingRequisitionCount.Should().Be(1);
        vm.MissingPartsNoRestockItemCount.Should().Be(0);
        vm.MissingPartsNoRestockRequisitionCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_ShowsBothCounts_WhenMixedShortageStatuses()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp);
        ctx.SaveChanges();
        ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = userId, ProductionWorkplaceId = wp.Id,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });

        // Eine Closed-Bestellung mit gemischten Shortage-Statuses.
        var r1 = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Closed,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester",
            CreatedByUserId = userId,
        };
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 1, Position = 1,
            ShortageStatus = ShortageStatus.WillBeRestocked,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        r1.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-2", ArticleDescription = "y", QuantityRequested = 1, Position = 2,
            ShortageStatus = ShortageStatus.NoRestock,
            CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "DOMAIN\\tester"
        });
        ctx.WarehouseRequisitions.Add(r1);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index() as ViewResult;

        result.Should().NotBeNull();
        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm.Should().NotBeNull();
        vm!.MissingPartsWaitingItemCount.Should().Be(1);
        vm.MissingPartsWaitingRequisitionCount.Should().Be(1, "selbe Bestellung zaehlt fuer Waiting");
        vm.MissingPartsNoRestockItemCount.Should().Be(1);
        vm.MissingPartsNoRestockRequisitionCount.Should().Be(1, "selbe Bestellung zaehlt auch fuer NoRestock");
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var (ctrl, ctx, userId) = Setup();
        var wpA = new ProductionWorkplace { Name = "WB-A1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wpB = new ProductionWorkplace { Name = "WB-B2", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wpC = new ProductionWorkplace { Name = "WB-C3", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.AddRange(wpA, wpB, wpC); ctx.SaveChanges();

        ctx.WarehouseRequisitions.AddRange(
            new WarehouseRequisition { ProductionWorkplaceId = wpA.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now, CreatedByUserId = userId, CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "t" },
            new WarehouseRequisition { ProductionWorkplaceId = wpB.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now, CreatedByUserId = userId, CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "t" },
            new WarehouseRequisition { ProductionWorkplaceId = wpC.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now, CreatedByUserId = userId, CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();

        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpCtx.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_workplace=A1");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].WorkplaceName.Should().Be("WB-A1");
        vm.Pagination.TotalCount.Should().Be(1);
    }
}
