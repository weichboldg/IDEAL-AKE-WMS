using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class WorkOperationRepositoryExtendedTests
{
    private static ProductionOrder CreateProductionOrder(string number)
    {
        return new ProductionOrder
        {
            OrderNumber = number,
            ArticleNumber = "ART-001",
            Description1 = "Testartikel",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    private static ProductionWorkplace CreateWorkplace(string name)
    {
        return new ProductionWorkplace
        {
            Name = name,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    private static WorkOperation CreateOperation(int poId, string opNumber, string name, int sequence,
        int? workplaceId = null, bool isReported = false)
    {
        return new WorkOperation
        {
            ProductionOrderId = poId,
            OperationNumber = opNumber,
            Name = name,
            Sequence = sequence,
            ProductionWorkplaceId = workplaceId,
            IsReportable = true,
            IsReported = isReported,
            ReportedAt = isReported ? DateTime.Now : null,
            ReportedBy = isReported ? "Test" : null,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task GetAllWithOrderAndWorkplaceAsync_IncludesRelatedData()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var workplace = CreateWorkplace("Werkbank A");
        ctx.ProductionWorkplaces.Add(workplace);
        var po = CreateProductionOrder("WA-100");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.Add(CreateOperation(po.Id, "10", "Zuschneiden", 1, workplace.Id));
        await ctx.SaveChangesAsync();

        var result = await repo.GetAllWithOrderAndWorkplaceAsync();

        result.Should().HaveCount(1);
        result.First().ProductionOrder.Should().NotBeNull();
        result.First().ProductionOrder.OrderNumber.Should().Be("WA-100");
        result.First().ProductionWorkplace.Should().NotBeNull();
        result.First().ProductionWorkplace!.Name.Should().Be("Werkbank A");
    }

    [Fact]
    public async Task GetAllWithOrderAndWorkplaceAsync_SortsByOrderNumberThenSequence()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var po1 = CreateProductionOrder("WA-200");
        var po2 = CreateProductionOrder("WA-100");
        ctx.ProductionOrders.AddRange(po1, po2);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.AddRange(
            CreateOperation(po1.Id, "20", "Op B", 2),
            CreateOperation(po1.Id, "10", "Op A", 1),
            CreateOperation(po2.Id, "10", "Op C", 1));
        await ctx.SaveChangesAsync();

        var result = await repo.GetAllWithOrderAndWorkplaceAsync();

        result.Should().HaveCount(3);
        result[0].ProductionOrder.OrderNumber.Should().Be("WA-100");
        result[1].ProductionOrder.OrderNumber.Should().Be("WA-200");
        result[1].Sequence.Should().Be(1);
        result[2].ProductionOrder.OrderNumber.Should().Be("WA-200");
        result[2].Sequence.Should().Be(2);
    }

    [Fact]
    public async Task GetByWorkplaceIdAsync_ReturnsOnlyMatchingWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp1 = CreateWorkplace("Werkbank 1");
        var wp2 = CreateWorkplace("Werkbank 2");
        ctx.ProductionWorkplaces.AddRange(wp1, wp2);
        var po = CreateProductionOrder("WA-300");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.AddRange(
            CreateOperation(po.Id, "10", "Op A", 1, wp1.Id),
            CreateOperation(po.Id, "20", "Op B", 2, wp2.Id),
            CreateOperation(po.Id, "30", "Op C", 3, wp1.Id));
        await ctx.SaveChangesAsync();

        var result = await repo.GetByWorkplaceIdAsync(wp1.Id);

        result.Should().HaveCount(2);
        result.All(o => o.ProductionWorkplaceId == wp1.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetByWorkplaceIdAsync_IncludesProductionOrder()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp = CreateWorkplace("Werkbank X");
        ctx.ProductionWorkplaces.Add(wp);
        var po = CreateProductionOrder("WA-400");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.Add(CreateOperation(po.Id, "10", "Op A", 1, wp.Id));
        await ctx.SaveChangesAsync();

        var result = await repo.GetByWorkplaceIdAsync(wp.Id);

        result.Should().HaveCount(1);
        result.First().ProductionOrder.Should().NotBeNull();
        result.First().ProductionOrder.OrderNumber.Should().Be("WA-400");
    }

    [Fact]
    public async Task GetOpenByWorkplaceIdAsync_ExcludesReportedOperations()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp = CreateWorkplace("Werkbank Y");
        ctx.ProductionWorkplaces.Add(wp);
        var po = CreateProductionOrder("WA-500");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.AddRange(
            CreateOperation(po.Id, "10", "Op offen", 1, wp.Id, isReported: false),
            CreateOperation(po.Id, "20", "Op gemeldet", 2, wp.Id, isReported: true),
            CreateOperation(po.Id, "30", "Op offen 2", 3, wp.Id, isReported: false));
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenByWorkplaceIdAsync(wp.Id);

        result.Should().HaveCount(2);
        result.All(o => !o.IsReported).Should().BeTrue();
    }

    [Fact]
    public async Task GetOpenByWorkplaceIdAsync_EmptyWhenAllReported()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var wp = CreateWorkplace("Werkbank Z");
        ctx.ProductionWorkplaces.Add(wp);
        var po = CreateProductionOrder("WA-600");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.Add(CreateOperation(po.Id, "10", "Op fertig", 1, wp.Id, isReported: true));
        await ctx.SaveChangesAsync();

        var result = await repo.GetOpenByWorkplaceIdAsync(wp.Id);

        result.Should().BeEmpty();
    }
}
