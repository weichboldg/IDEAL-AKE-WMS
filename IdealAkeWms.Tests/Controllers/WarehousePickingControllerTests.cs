using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WarehousePickingControllerTests
{
    private static (WarehousePickingController ctrl, ApplicationDbContext ctx, int userId) Setup()
    {
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "stocker", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.SaveChanges();

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        current.Setup(s => s.GetDisplayName()).Returns("stocker");
        current.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\stocker");

        var repo = new WarehouseRequisitionRepository(ctx);
        var workplaces = new ProductionWorkplaceRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null))
             .ReturnsAsync(new List<StockOverviewItem>());

        var ctrl = new WarehousePickingController(repo, workplaces, stock.Object, current.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx, u.Id);
    }

    [Fact]
    public async Task Index_ShowsOnlyNonDraft()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();

        var draft = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Draft, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" };
        var submitted = new WarehouseRequisition { ProductionWorkplaceId = wp.Id, Status = WarehouseRequisitionStatus.Submitted, SubmittedAt = DateTime.Now, CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" };
        ctx.WarehouseRequisitions.AddRange(draft, submitted);
        await ctx.SaveChangesAsync();

        var result = await ctrl.Index(statusFilter: null, workplaceId: null, page: 1) as ViewResult;

        var vm = result!.Model as WarehouseRequisitionListViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].Status.Should().Be(WarehouseRequisitionStatus.Submitted);
    }

    [Fact]
    public async Task Close_WritesItemQuantitiesPickedAndSetsStatus()
    {
        var (ctrl, ctx, userId) = Setup();
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.Add(wp); ctx.SaveChanges();
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = wp.Id,
            Status = WarehouseRequisitionStatus.Submitted,
            SubmittedAt = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        };
        r.Items.Add(new WarehouseRequisitionItem
        {
            ArticleNumber = "ART-1", ArticleDescription = "x", QuantityRequested = 5, Position = 1,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x"
        });
        ctx.WarehouseRequisitions.Add(r);
        await ctx.SaveChangesAsync();
        var item = ctx.WarehouseRequisitionItems.First();

        var result = await ctrl.Close(r.Id, new[] { item.Id }, new[] { 4 }, notes: null, isFinalShortages: null, r.RowVersion) as RedirectToActionResult;

        result.Should().NotBeNull();
        var updated = ctx.WarehouseRequisitions.Include(x => x.Items).First(x => x.Id == r.Id);
        // Mit DeriveStatus-Refactor (Task 2): picked 4 < requested 5 und IsFinalShortage=false
        // => PartiallyDelivered. Vorher war Status hartkodiert Closed.
        // Der Controller liefert (noch) keinen IsFinalShortage-Flag durch (Task 7).
        updated.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
        updated.Items.First().QuantityPicked.Should().Be(4m);
    }

    [Fact]
    public async Task Close_RowVersionConflict_ShowsWarningAndStays()
    {
        // InMemory DB unterstuetzt kein RowVersion → Mock<IWarehouseRequisitionRepository> der DbUpdateConcurrencyException wirft
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "stocker", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.SaveChanges();

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        current.Setup(s => s.GetDisplayName()).Returns("stocker");
        current.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\stocker");

        var repo = new Mock<IWarehouseRequisitionRepository>();
        repo.Setup(r => r.CloseAsync(It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<int, decimal>>(),
                It.IsAny<IReadOnlyDictionary<int, string?>>(),
                It.IsAny<IReadOnlyDictionary<int, bool>>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var workplaces = new ProductionWorkplaceRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null))
             .ReturnsAsync(new List<StockOverviewItem>());

        var ctrl = new WarehousePickingController(repo.Object, workplaces, stock.Object, current.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        var staleRowVersion = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        var result = await ctrl.Close(id: 99, itemIds: new[] { 1 }, quantitiesPicked: new[] { 4 }, notes: null, isFinalShortages: null, rowVersion: staleRowVersion) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Details");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }

    /// <summary>
    /// Baut einen Controller mit Mock-Repository + minimalen Mocks fuer alle Dependencies.
    /// Wird in Tests gebraucht, die das Verhalten des Controllers gegen ein Mock-Repo verifizieren.
    /// </summary>
    private static (WarehousePickingController ctrl, Mock<IWarehouseRequisitionRepository> repoMock)
        SetupWithMockRepo()
    {
        var ctx = TestDbContextFactory.Create();
        var u = new User { Name = "stocker", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.SaveChanges();

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns(u.Id);
        current.Setup(s => s.GetDisplayName()).Returns("stocker");
        current.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\stocker");
        current.Setup(s => s.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        var repo = new Mock<IWarehouseRequisitionRepository>();
        var workplaces = new ProductionWorkplaceRepository(ctx);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null))
             .ReturnsAsync(new List<StockOverviewItem>());

        var ctrl = new WarehousePickingController(repo.Object, workplaces, stock.Object, current.Object);
        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        ctrl.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = httpCtx
        };
        // Url.Action braucht ein IUrlHelper — Mock liefert die Route-URL als String zurueck.
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(ctx =>
                $"/WarehousePicking/{ctx.Action}/{ctx.Values?.GetType().GetProperty("id")?.GetValue(ctx.Values)}");
        ctrl.Url = urlHelper.Object;
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpCtx,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, repo);
    }

    [Fact]
    public async Task Close_AcceptsIsFinalShortageArray_PassesToRepo()
    {
        var (ctrl, repo) = SetupWithMockRepo();
        IReadOnlyDictionary<int, bool>? capturedFlags = null;
        repo.Setup(r => r.CloseAsync(It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<int, decimal>>(),
                It.IsAny<IReadOnlyDictionary<int, string?>>(),
                It.IsAny<IReadOnlyDictionary<int, bool>>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<int, IReadOnlyDictionary<int, decimal>, IReadOnlyDictionary<int, string?>,
                IReadOnlyDictionary<int, bool>, int, string, string, byte[]>(
                (_, _, _, flags, _, _, _, _) => capturedFlags = flags)
            .Returns(Task.CompletedTask);

        await ctrl.Close(id: 42, itemIds: new[] { 1, 2 }, quantitiesPicked: new[] { 5, 3 },
            notes: null, isFinalShortages: new[] { true, false }, rowVersion: new byte[0]);

        capturedFlags.Should().NotBeNull();
        capturedFlags![1].Should().BeTrue();
        capturedFlags![2].Should().BeFalse();
        repo.Verify(r => r.CloseAsync(42,
            It.IsAny<IReadOnlyDictionary<int, decimal>>(),
            It.IsAny<IReadOnlyDictionary<int, string?>>(),
            It.Is<IReadOnlyDictionary<int, bool>>(d => d[1] == true && d[2] == false),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Once);
    }

    [Fact]
    public async Task Close_QuantitiesNegative_ReturnsWarning_NoChange()
    {
        var (ctrl, repo) = SetupWithMockRepo();

        var result = await ctrl.Close(id: 42, itemIds: new[] { 1, 2 },
            quantitiesPicked: new[] { -1, 5 }, notes: null, isFinalShortages: null,
            rowVersion: new byte[0]) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Details");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
        repo.Verify(r => r.CloseAsync(It.IsAny<int>(),
            It.IsAny<IReadOnlyDictionary<int, decimal>>(),
            It.IsAny<IReadOnlyDictionary<int, string?>>(),
            It.IsAny<IReadOnlyDictionary<int, bool>>(),
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_Default_ShowsSubmittedAndPartiallyDelivered()
    {
        var (ctrl, repo) = SetupWithMockRepo();
        WarehouseRequisitionStatus[]? capturedStatuses = null;
        repo.Setup(r => r.GetForWarehouseAsync(
                It.IsAny<WarehouseRequisitionStatus[]>(),
                It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<WarehouseRequisitionStatus[], int?, int, int>(
                (statuses, _, _, _) => capturedStatuses ??= statuses)
            .ReturnsAsync((new List<WarehouseRequisition>(), 0));

        await ctrl.Index(statusFilter: null, workplaceId: null, page: 1);

        capturedStatuses.Should().NotBeNull();
        capturedStatuses.Should().Contain(WarehouseRequisitionStatus.Submitted);
        capturedStatuses.Should().Contain(WarehouseRequisitionStatus.PartiallyDelivered);
        capturedStatuses!.Length.Should().Be(2);
    }

    [Fact]
    public async Task SaveProgress_PersistsAllFields_ReturnsOk()
    {
        var (ctrl, repo) = SetupWithMockRepo();
        IReadOnlyDictionary<int, decimal?>? capturedQty = null;
        IReadOnlyDictionary<int, string?>? capturedNotes = null;
        IReadOnlyDictionary<int, bool>? capturedFlags = null;
        repo.Setup(r => r.SaveProgressAsync(It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<int, decimal?>>(),
                It.IsAny<IReadOnlyDictionary<int, string?>>(),
                It.IsAny<IReadOnlyDictionary<int, bool>>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, IReadOnlyDictionary<int, decimal?>, IReadOnlyDictionary<int, string?>,
                IReadOnlyDictionary<int, bool>, string, string>(
                (_, q, n, f, _, _) => { capturedQty = q; capturedNotes = n; capturedFlags = f; })
            .Returns(Task.CompletedTask);

        var result = await ctrl.SaveProgress(id: 1, itemIds: new[] { 10 },
            quantitiesPicked: new int?[] { 5 },
            notes: new[] { "x" },
            isFinalShortages: new[] { true });

        result.Should().BeOfType<OkResult>();
        capturedQty.Should().NotBeNull();
        capturedQty![10].Should().Be(5m);
        capturedNotes!.Should().NotBeNull();
        capturedNotes![10].Should().Be("x");
        capturedFlags!.Should().NotBeNull();
        capturedFlags![10].Should().BeTrue();
    }

    [Fact]
    public async Task PrintAndClose_OnSuccess_ReturnsJsonWithRedirectUrl()
    {
        var (ctrl, repo) = SetupWithMockRepo();
        repo.Setup(r => r.CloseAsync(It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<int, decimal>>(),
                It.IsAny<IReadOnlyDictionary<int, string?>>(),
                It.IsAny<IReadOnlyDictionary<int, bool>>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);

        var result = await ctrl.PrintAndClose(id: 42, itemIds: new[] { 1 },
            quantitiesPicked: new[] { 3 }, notes: null, isFinalShortages: null,
            rowVersion: new byte[0]) as OkObjectResult;

        result.Should().NotBeNull();
        var redirectUrl = result!.Value!.GetType().GetProperty("redirectUrl")!.GetValue(result.Value) as string;
        redirectUrl.Should().NotBeNullOrEmpty();
        redirectUrl.Should().Contain("Print");
        redirectUrl.Should().Contain("42");
    }

    [Fact]
    public async Task PrintAndClose_OnConcurrencyConflict_Returns409Conflict()
    {
        var (ctrl, repo) = SetupWithMockRepo();
        repo.Setup(r => r.CloseAsync(It.IsAny<int>(),
                It.IsAny<IReadOnlyDictionary<int, decimal>>(),
                It.IsAny<IReadOnlyDictionary<int, string?>>(),
                It.IsAny<IReadOnlyDictionary<int, bool>>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await ctrl.PrintAndClose(id: 42, itemIds: new[] { 1 },
            quantitiesPicked: new[] { 3 }, notes: null, isFinalShortages: null,
            rowVersion: new byte[0]);

        result.Should().BeOfType<ConflictObjectResult>();
    }
}
