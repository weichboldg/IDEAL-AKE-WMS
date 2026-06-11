using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das ProductionOrderRepository (Sage-Master-Daten).
/// Released-/Picker-/Priority-Logik liegt seit Phase 1 (v1.11.0) im
/// <see cref="ProductionOrderPickingStatusRepository"/> — siehe dort die migrierten Tests.
/// </summary>
public class ProductionOrderRepositoryTests
{
    [Fact]
    public async Task GetAllOrderedAsync_OrdersByOrderNumber()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-3");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-1");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-2");

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetAllOrderedAsync();

        result.Should().HaveCount(3);
        result.Select(o => o.OrderNumber).Should().ContainInOrder("WA-1", "WA-2", "WA-3");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ExcludesDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-OPEN", isDone: false);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-DONE", isDone: true);

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetOpenOrdersAsync();

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("WA-OPEN");
    }

    [Fact]
    public async Task GetByOrderNumberAsync_ReturnsCorrectOrder()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-100");
        TestDataHelper.CreateOrderWithStatuses(context, "WA-200");

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetByOrderNumberAsync("WA-200");

        result.Should().NotBeNull();
        result!.OrderNumber.Should().Be("WA-200");
    }

    [Fact]
    public async Task GetForLeitstand_ExcludesKommDoneOrders_WhenShowDoneFalse()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 3, OrderNumber = "FA-SAGEDONE", IsDone = true });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 1, IsDonePicking = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: false, page: 1, pageSize: 100);

        page.Rows.Should().ContainSingle(r => r.OrderNumber == "FA-OPEN");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetForLeitstand_IncludesKommDoneWithFlag_WhenShowDoneTrue()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionOrderRepository(ctx);

        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA-OPEN", IsDone = false });
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 2, OrderNumber = "FA-KOMMDONE", IsDone = false });
        ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus { ProductionOrderId = 2, IsDonePicking = true });
        await ctx.SaveChangesAsync();

        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1, pageSize: 100);

        page.Rows.Should().HaveCount(2);
        page.Rows.Single(r => r.OrderNumber == "FA-KOMMDONE").IsDonePicking.Should().BeTrue();
        page.Rows.Single(r => r.OrderNumber == "FA-OPEN").IsDonePicking.Should().BeFalse();
    }
}
