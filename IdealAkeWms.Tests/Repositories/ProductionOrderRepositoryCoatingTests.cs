using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionOrderRepositoryCoatingTests
{
    [Fact]
    public async Task GetOpenOrdersInWindowAsync_FiltersAndLimits()
    {
        using var context = TestDbContextFactory.Create();
        var now = DateTime.Now;

        context.ProductionOrders.AddRange(
            // Should be included: open, date within window (4 weeks)
            new ProductionOrder { OrderNumber = "WA-1", IsDone = false, ProductionDate = now.AddDays(7), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new ProductionOrder { OrderNumber = "WA-2", IsDone = false, ProductionDate = now.AddDays(14), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            // Should be excluded: IsDone = true
            new ProductionOrder { OrderNumber = "WA-DONE", IsDone = true, ProductionDate = now.AddDays(5), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            // Should be excluded: no ProductionDate
            new ProductionOrder { OrderNumber = "WA-NODATE", IsDone = false, ProductionDate = null, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            // Should be excluded: date beyond window (4 weeks = 28 days)
            new ProductionOrder { OrderNumber = "WA-FAR", IsDone = false, ProductionDate = now.AddDays(60), CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        );
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetOpenOrdersInWindowAsync(weeksAhead: 4, maxCount: 100);

        result.Should().HaveCount(2);
        result.Select(r => r.OrderNumber).Should().Contain("WA-1").And.Contain("WA-2");
        result[0].ProductionDate.Should().BeBefore(result[1].ProductionDate!.Value); // ordered ASC
    }

    [Fact]
    public async Task GetOpenOrdersInWindowAsync_RespectsMaxCount()
    {
        using var context = TestDbContextFactory.Create();
        var now = DateTime.Now;

        for (int i = 1; i <= 10; i++)
        {
            context.ProductionOrders.Add(new ProductionOrder
            {
                OrderNumber = $"WA-{i:D2}",
                IsDone = false,
                ProductionDate = now.AddDays(i),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test",
                CreatedByWindows = "test"
            });
        }
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        var result = await repo.GetOpenOrdersInWindowAsync(weeksAhead: 4, maxCount: 3);

        result.Should().HaveCount(3);
        // Should be the 3 earliest
        result[0].OrderNumber.Should().Be("WA-01");
        result[1].OrderNumber.Should().Be("WA-02");
        result[2].OrderNumber.Should().Be("WA-03");
    }

    [Fact]
    public async Task SetCoatingFlagsAsync_SetsFlag()
    {
        using var context = TestDbContextFactory.Create();
        var order = new ProductionOrder
        {
            OrderNumber = "WA-COAT",
            IsDone = false,
            HasCoatingParts = false,
            IsCoatingDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        await repo.SetCoatingFlagsAsync(new Dictionary<int, bool> { { order.Id, true } });

        var updated = context.ProductionOrders.First(po => po.Id == order.Id);
        updated.HasCoatingParts.Should().BeTrue();
        updated.IsCoatingDone.Should().BeFalse(); // not touched when setting to true
        updated.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetCoatingFlagsAsync_ResetsIsCoatingDone_WhenFlagFlipsToFalse()
    {
        using var context = TestDbContextFactory.Create();
        var order = new ProductionOrder
        {
            OrderNumber = "WA-RESET",
            IsDone = false,
            HasCoatingParts = true,
            IsCoatingDone = true, // was marked as coating done
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        var repo = new ProductionOrderRepository(context);
        await repo.SetCoatingFlagsAsync(new Dictionary<int, bool> { { order.Id, false } });

        var updated = context.ProductionOrders.First(po => po.Id == order.Id);
        updated.HasCoatingParts.Should().BeFalse();
        updated.IsCoatingDone.Should().BeFalse(); // cascade reset
        updated.ModifiedAt.Should().NotBeNull();
    }
}
