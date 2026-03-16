using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class WorkOperationRepositoryTests
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

    private static WorkOperation CreateOperation(int productionOrderId, string opNumber, string name, int sequence)
    {
        return new WorkOperation
        {
            ProductionOrderId = productionOrderId,
            OperationNumber = opNumber,
            Name = name,
            Sequence = sequence,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task GetByProductionOrderIdAsync_ReturnsSortedBySequence()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var po = CreateProductionOrder("WA-001");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.AddRange(
            CreateOperation(po.Id, "30", "Lackieren", 3),
            CreateOperation(po.Id, "10", "Zuschneiden", 1),
            CreateOperation(po.Id, "20", "Biegen", 2));
        await ctx.SaveChangesAsync();

        var result = await repo.GetByProductionOrderIdAsync(po.Id);

        result.Should().HaveCount(3);
        result.Select(op => op.Sequence).Should().ContainInOrder(1, 2, 3);
        result.Select(op => op.Name).Should().ContainInOrder("Zuschneiden", "Biegen", "Lackieren");
    }

    [Fact]
    public async Task GetByProductionOrderIdAsync_NoOperations_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var po = CreateProductionOrder("WA-002");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        var result = await repo.GetByProductionOrderIdAsync(po.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByProductionOrderIdAsync_OnlyReturnsMatchingOrder()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var po1 = CreateProductionOrder("WA-003");
        var po2 = CreateProductionOrder("WA-004");
        ctx.ProductionOrders.AddRange(po1, po2);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.AddRange(
            CreateOperation(po1.Id, "10", "Op A", 1),
            CreateOperation(po2.Id, "10", "Op B", 1));
        await ctx.SaveChangesAsync();

        var result = await repo.GetByProductionOrderIdAsync(po1.Id);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Op A");
    }

    [Fact]
    public async Task GetByProductionOrderIdWithWorkplaceAsync_IncludesWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var workplace = new ProductionWorkplace
        {
            Name = "Werkbank Test",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.ProductionWorkplaces.Add(workplace);
        await ctx.SaveChangesAsync();

        var po = CreateProductionOrder("WA-005");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        var op = CreateOperation(po.Id, "10", "Op mit Werkbank", 1);
        op.ProductionWorkplaceId = workplace.Id;
        ctx.WorkOperations.Add(op);
        await ctx.SaveChangesAsync();

        var result = await repo.GetByProductionOrderIdWithWorkplaceAsync(po.Id);

        result.Should().HaveCount(1);
        result.First().ProductionWorkplace.Should().NotBeNull();
        result.First().ProductionWorkplace!.Name.Should().Be("Werkbank Test");
    }

    [Fact]
    public async Task GetByProductionOrderIdWithWorkplaceAsync_NullWorkplace_ReturnsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WorkOperationRepository(ctx);

        var po = CreateProductionOrder("WA-006");
        ctx.ProductionOrders.Add(po);
        await ctx.SaveChangesAsync();

        ctx.WorkOperations.Add(CreateOperation(po.Id, "10", "Op ohne Werkbank", 1));
        await ctx.SaveChangesAsync();

        var result = await repo.GetByProductionOrderIdWithWorkplaceAsync(po.Id);

        result.Should().HaveCount(1);
        result.First().ProductionWorkplace.Should().BeNull();
    }
}
