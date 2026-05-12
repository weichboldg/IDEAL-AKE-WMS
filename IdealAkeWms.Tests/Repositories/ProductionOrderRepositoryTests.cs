using FluentAssertions;
using IdealAkeWms.Data.Repositories;
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
}
