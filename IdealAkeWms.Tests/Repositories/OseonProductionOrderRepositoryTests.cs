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

    // === GetPagedAsync with relevantOperationNames ===

    private static OseonProductionOrder CreateOrder(long oseonId, string orderNr, string custNr, int status)
        => new()
        {
            OseonId = oseonId, OseonOrderNumber = orderNr, CustomerOrderNumber = custNr,
            OseonStatus = status, DueDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
        };

    private static OseonWorkOperation CreateOp(int orderId, string pos, string name, int status)
        => new()
        {
            OseonProductionOrderId = orderId, PositionNumber = pos, Name = name, OseonStatus = status,
            CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetPagedAsync_WithoutRelevance_FiltersOnOseonStatus()
    {
        using var context = TestDbContextFactory.Create();
        context.OseonProductionOrders.AddRange(
            CreateOrder(1, "O-001", "C-001", 60),  // open
            CreateOrder(2, "O-002", "C-002", 90)   // finished
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);

        // showFinished=false, no relevance
        var result = await repo.GetPagedAsync(null, null, false, 1, 25);
        result.TotalGroupCount.Should().Be(1);
        result.Items.Should().AllSatisfy(o => o.CustomerOrderNumber.Should().Be("C-001"));

        // showFinished=true
        var resultAll = await repo.GetPagedAsync(null, null, true, 1, 25);
        resultAll.TotalGroupCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithRelevance_AllRelevantOpsFinished_OrderIsFiltered()
    {
        // Auftrag mit ST (relevant, fertig) + ZB (nicht relevant, offen)
        // → Auftrag gilt als "fertig" weil alle relevanten AGs fertig sind
        using var context = TestDbContextFactory.Create();
        var order = CreateOrder(1, "O-001", "C-001", 60); // OSEON status still 60 (In Arbeit)
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            CreateOp(order.Id, "10", "ST", 90),  // relevant, fertig
            CreateOp(order.Id, "20", "ZB", 30)   // NOT relevant (not in set), offen
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var relevantNames = new HashSet<string> { "ST", "BG", "EG" }; // ZB not in set

        // showFinished=false with relevance → should filter out this order
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, relevantNames);
        result.TotalGroupCount.Should().Be(0, "all relevant ops (ST) are finished, so order is filtered");
    }

    [Fact]
    public async Task GetPagedAsync_WithRelevance_NoRelevantOps_OrderIsFiltered()
    {
        // Auftrag mit nur A-BT + ZB (beide NICHT relevant)
        // → Auftrag gilt als "fertig" (keine relevanten AGs = fertig)
        using var context = TestDbContextFactory.Create();
        var order = CreateOrder(1, "O-001", "C-001", 60);
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            CreateOp(order.Id, "10", "A-BT", 90),
            CreateOp(order.Id, "20", "ZB", 30)    // offen aber nicht relevant
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var relevantNames = new HashSet<string> { "ST", "BG", "EG" }; // weder A-BT noch ZB

        var result = await repo.GetPagedAsync(null, null, false, 1, 25, relevantNames);
        result.TotalGroupCount.Should().Be(0, "no relevant ops exist, so order counts as finished");
    }

    [Fact]
    public async Task GetPagedAsync_WithRelevance_RelevantOpStillOpen_OrderShown()
    {
        // Auftrag mit BG (relevant, offen) + ZB (nicht relevant, offen)
        // → Auftrag gilt als "offen" weil BG noch offen ist
        using var context = TestDbContextFactory.Create();
        var order = CreateOrder(1, "O-001", "C-001", 60);
        context.OseonProductionOrders.Add(order);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            CreateOp(order.Id, "10", "BG", 60),  // relevant, offen
            CreateOp(order.Id, "20", "ZB", 30)   // nicht relevant
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var relevantNames = new HashSet<string> { "ST", "BG", "EG" };

        var result = await repo.GetPagedAsync(null, null, false, 1, 25, relevantNames);
        result.TotalGroupCount.Should().Be(1, "BG is relevant and still open");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithRelevance_ShowFinishedTrue_ShowsAll()
    {
        using var context = TestDbContextFactory.Create();
        var order1 = CreateOrder(1, "O-001", "C-001", 60);
        var order2 = CreateOrder(2, "O-002", "C-002", 90);
        context.OseonProductionOrders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            CreateOp(order1.Id, "10", "ST", 90), // all relevant finished
            CreateOp(order2.Id, "10", "ST", 90)
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var relevantNames = new HashSet<string> { "ST" };

        // showFinished=true ignores relevance filter
        var result = await repo.GetPagedAsync(null, null, true, 1, 25, relevantNames);
        result.TotalGroupCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithRelevance_PaginationCountIsCorrect()
    {
        using var context = TestDbContextFactory.Create();

        // 3 Auftraege: 2 effektiv offen, 1 effektiv fertig
        var o1 = CreateOrder(1, "O-001", "C-001", 60);
        var o2 = CreateOrder(2, "O-002", "C-002", 60);
        var o3 = CreateOrder(3, "O-003", "C-003", 60);
        context.OseonProductionOrders.AddRange(o1, o2, o3);
        await context.SaveChangesAsync();

        context.OseonWorkOperations.AddRange(
            CreateOp(o1.Id, "10", "ST", 60),  // relevant, offen
            CreateOp(o2.Id, "10", "ST", 30),  // relevant, offen
            CreateOp(o3.Id, "10", "ST", 90)   // relevant, fertig → wird gefiltert
        );
        await context.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(context);
        var relevantNames = new HashSet<string> { "ST" };

        var result = await repo.GetPagedAsync(null, null, false, 1, 25, relevantNames);
        result.TotalGroupCount.Should().Be(2, "only 2 orders have open relevant ops");
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(1);
    }
}
