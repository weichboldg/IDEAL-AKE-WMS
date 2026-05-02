using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
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

        var result = await ctrl.Close(r.Id, new[] { item.Id }, new[] { 4m }, r.RowVersion) as RedirectToActionResult;

        result.Should().NotBeNull();
        var updated = ctx.WarehouseRequisitions.Include(x => x.Items).First(x => x.Id == r.Id);
        updated.Status.Should().Be(WarehouseRequisitionStatus.Closed);
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

        var result = await ctrl.Close(id: 99, itemIds: new[] { 1 }, quantitiesPicked: new[] { 4m }, rowVersion: staleRowVersion) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Details");
        ctrl.TempData["WarningMessage"].Should().NotBeNull();
    }
}
