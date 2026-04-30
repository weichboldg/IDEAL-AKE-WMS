using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OseonProductionOrderRepositoryReportingTests
{
    private static OseonProductionOrder NewOrder(long oseonId, int? workplaceId, DateTime? dueDate, int status,
        string custOrder = "K-100", string faNumber = "FA-100")
    {
        return new OseonProductionOrder
        {
            OseonId = oseonId,
            OseonOrderNumber = faNumber,
            CustomerOrderNumber = custOrder,
            OseonStatus = status,
            ProductionWorkplaceId = workplaceId,
            DueDate = dueDate,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
    }

    private static OseonWorkOperation NewWorkOp(string position, string name, int status,
        DateTime? lastReport = null)
    {
        return new OseonWorkOperation
        {
            PositionNumber = position,
            Name = name,
            OseonStatus = status,
            LastStatusReportInOseon = lastReport,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
    }

    private static OseonOperationConfig NewConfig(string name, int offset, bool relevant = true)
    {
        return new OseonOperationConfig
        {
            OperationName = name,
            DueDateOffsetDays = offset,
            IsOseonRelevant = relevant
        };
    }

    [Fact]
    public async Task ExcludesOrdersWithCancelledStatus()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 95); // 95 = Storniert
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesCancelledWorkOperations()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 95) }; // 95 = Storniert
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesOrdersWithoutDueDate()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, null, 60); // DueDate null
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ExcludesAgsWithoutConfig_AndCounts()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "UNKNOWN_AG", 60) };
        ctx.OseonProductionOrders.Add(order);
        // KEIN Config-Eintrag fuer UNKNOWN_AG
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
        result.OperationsWithoutConfigCount.Should().Be(1);
    }

    [Fact]
    public async Task ExcludesNonRelevantConfigs()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0, relevant: false));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task FiltersByWorkplaceId()
    {
        var ctx = TestDbContextFactory.Create();
        var wp1 = new ProductionWorkplace { Name = "WP1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp2 = new ProductionWorkplace { Name = "WP2", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        await ctx.SaveChangesAsync();

        var orderA = NewOrder(1, wp1.Id, DateTime.Today, 60);
        orderA.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        var orderB = NewOrder(2, wp2.Id, DateTime.Today, 60);
        orderB.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.AddRange(orderA, orderB);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(wp1.Id, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].Order.OseonId.Should().Be(1);
    }

    [Fact]
    public async Task FiltersByOperationNames()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.WorkOperations = new List<OseonWorkOperation>
        {
            NewWorkOp("10", "B", 60),
            NewWorkOp("20", "ST", 60)
        };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.AddRange(NewConfig("B", 0), NewConfig("ST", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, new[] { "B" }, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].WorkOperation.Name.Should().Be("B");
    }

    [Fact]
    public async Task FiltersByCustomerOrderNumber_PrefixMatch()
    {
        var ctx = TestDbContextFactory.Create();
        var orderA = NewOrder(1, null, DateTime.Today, 60, custOrder: "K-1234");
        orderA.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        var orderB = NewOrder(2, null, DateTime.Today, 60, custOrder: "K-9999");
        orderB.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.AddRange(orderA, orderB);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, "K-12", null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.Rows.Should().HaveCount(1);
        result.Rows[0].Order.CustomerOrderNumber.Should().Be("K-1234");
    }

    [Fact]
    public async Task DataAsOf_IsMaxLastChangedInOseon()
    {
        var ctx = TestDbContextFactory.Create();
        var order = NewOrder(1, null, DateTime.Today, 60);
        order.LastChangedInOseon = new DateTime(2026, 4, 30, 14, 32, 0);
        order.WorkOperations = new List<OseonWorkOperation> { NewWorkOp("10", "B", 60) };
        ctx.OseonProductionOrders.Add(order);
        ctx.OseonOperationConfigs.Add(NewConfig("B", 0));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.DataAsOf.Should().Be(new DateTime(2026, 4, 30, 14, 32, 0));
    }

    [Fact]
    public async Task DataAsOf_IsNullWhenEmpty()
    {
        var ctx = TestDbContextFactory.Create();
        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetRelevantOperationsForReportingAsync(null, null, null, null,
            DateTime.Today.AddDays(-30), DateTime.Today.AddDays(30));

        result.DataAsOf.Should().BeNull();
    }
}
