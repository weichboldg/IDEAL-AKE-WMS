using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionOrderRepositoryTests
{
    [Fact]
    public async Task GetReleasedForPickingAsync_ReturnsOnlyReleasedAndNotDone()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, PickingPriority = 2, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-3", IsReleasedForPicking = false, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-4", IsReleasedForPicking = true, IsDone = false, PickingPriority = 1, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingAsync();

        result.Should().HaveCount(2);
        result[0].OrderNumber.Should().Be("WA-4"); // Prio 1 zuerst
        result[1].OrderNumber.Should().Be("WA-1"); // Prio 2 danach
    }

    [Fact]
    public async Task GetReleasedForPickingAsync_NullPriorityComesLast()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-A", IsReleasedForPicking = true, IsDone = false, PickingPriority = null, ProductionDate = new DateTime(2026, 5, 1), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-B", IsReleasedForPicking = true, IsDone = false, PickingPriority = 3, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetReleasedForPickingAsync();

        result.Should().HaveCount(2);
        result[0].OrderNumber.Should().Be("WA-B"); // Prio 3
        result[1].OrderNumber.Should().Be("WA-A"); // Keine Prio → ans Ende
    }

    [Fact]
    public async Task GetReleasedForPickingCountAsync_CountsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        context.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "WA-1", IsReleasedForPicking = true, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsReleasedForPicking = true, IsDone = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-3", IsReleasedForPicking = false, IsDone = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var count = await repo.GetReleasedForPickingCountAsync();

        count.Should().Be(1);
    }
}
