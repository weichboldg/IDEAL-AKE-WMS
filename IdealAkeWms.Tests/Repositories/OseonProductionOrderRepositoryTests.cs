using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OseonProductionOrderRepositoryTests
{
    [Fact]
    public async Task GetAllWithOperationsAsync_ReturnsOrdersWithOperations()
    {
        using var context = TestDbContextFactory.Create();
        var order = new OseonProductionOrder
        {
            OseonId = 1,
            OseonOrderNumber = "1515516",
            CustomerOrderNumber = "2610097",
            OseonStatus = 60,
            ArticleNumber = "US253721",
            Description1 = "UKW 4-1-E USA",
            QuantityTarget = 3,
            QuantityActual = 0,
            DueDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();

        var operation = new OseonWorkOperation
        {
            OseonProductionOrderId = order.Id,
            PositionNumber = "0010",
            Name = "A-BT",
            OseonStatus = 90,
            IsFirstOperation = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };
        context.OseonWorkOperations.Add(operation);
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var result = await repo.GetAllWithOperationsAsync();

        result.Should().HaveCount(1);
        result[0].OseonOrderNumber.Should().Be("1515516");
        result[0].WorkOperations.Should().HaveCount(1);
        result[0].WorkOperations.First().Name.Should().Be("A-BT");
    }

    [Fact]
    public async Task GetByOseonIdAsync_FindsCorrectOrder()
    {
        using var context = TestDbContextFactory.Create();
        context.OseonProductionOrders.Add(new OseonProductionOrder
        {
            OseonId = 999,
            OseonOrderNumber = "TEST-001",
            OseonStatus = 30,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var result = await repo.GetByOseonIdAsync(999);

        result.Should().NotBeNull();
        result!.OseonOrderNumber.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetByOseonIdAsync_NotFound_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new OseonProductionOrderRepository(context);

        var result = await repo.GetByOseonIdAsync(9999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCustomerOrderNumberAsync_GroupsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        context.OseonProductionOrders.AddRange(
            new OseonProductionOrder
            {
                OseonId = 1, OseonOrderNumber = "100", CustomerOrderNumber = "WA-001",
                OseonStatus = 60, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
            },
            new OseonProductionOrder
            {
                OseonId = 2, OseonOrderNumber = "100-01", CustomerOrderNumber = "WA-001",
                OseonStatus = 90, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
            },
            new OseonProductionOrder
            {
                OseonId = 3, OseonOrderNumber = "200", CustomerOrderNumber = "WA-002",
                OseonStatus = 30, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
            }
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var result = await repo.GetByCustomerOrderNumberAsync("WA-001");

        result.Should().HaveCount(2);
        result.All(o => o.CustomerOrderNumber == "WA-001").Should().BeTrue();
    }
}
