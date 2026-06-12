using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das FaWorkStepRepository (FA-Vorbau v1.22.0, Plan Task 3).
/// </summary>
public class FaWorkStepRepositoryTests
{
    [Fact]
    public async Task SetActive_ReactivatesRemovedRow_InsteadOfDuplicate()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaWorkStepRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "Lueftung" });
        ctx.FaWorkSteps.Add(new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsRemoved = true, Source = "Sync" });
        await ctx.SaveChangesAsync();

        await repo.SetActiveAsync(1, 10, active: true, "tester", "win\\tester");

        var rows = ctx.FaWorkSteps.Where(f => f.ProductionOrderId == 1 && f.WorkStepId == 10).ToList();
        rows.Should().ContainSingle();
        rows[0].IsRemoved.Should().BeFalse();
        rows[0].Source.Should().Be("Manual");
    }

    [Fact]
    public async Task GetWorkStepPivot_ReturnsCodeFlags_OnlyForActiveRows()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaWorkStepRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VL", Name = "L" }, new WorkStep { Id = 11, Code = "VK", Name = "K" });
        ctx.FaWorkSteps.AddRange(
            new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10, IsRemoved = false },
            new FaWorkStep { ProductionOrderId = 1, WorkStepId = 11, IsRemoved = true });
        await ctx.SaveChangesAsync();

        var pivot = await repo.GetWorkStepPivotAsync(new List<int> { 1 });

        pivot[1].GetValueOrDefault("VL").Should().BeTrue();
        pivot[1].GetValueOrDefault("VK").Should().BeFalse();
    }

    [Fact]
    public async Task SetIsCompleted_SetsAuditAndCompletedFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new FaWorkStepRepository(ctx);
        ctx.ProductionOrders.Add(new ProductionOrder { Id = 1, OrderNumber = "FA1" });
        ctx.WorkSteps.Add(new WorkStep { Id = 10, Code = "VL", Name = "L" });
        var row = new FaWorkStep { ProductionOrderId = 1, WorkStepId = 10 };
        ctx.FaWorkSteps.Add(row);
        await ctx.SaveChangesAsync();

        await repo.SetIsCompletedAsync(row.Id, true, "tester", "win\\tester");

        var reloaded = await ctx.FaWorkSteps.FindAsync(row.Id);
        reloaded!.IsCompleted.Should().BeTrue();
        reloaded.CompletedAt.Should().NotBeNull();
        reloaded.CompletedBy.Should().Be("tester");
    }
}
