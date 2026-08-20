using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// IDEAL FA-Liste-Hierarchie (Etappe A): gruppierte, gruppen-paginierte Abfrage
/// <see cref="ProductionOrderRepository.GetForLeitstandGroupedAsync"/>.
/// Ohne Like-Filter (InMemory kann EF.Functions.Like nicht) — getestet werden Gruppierung,
/// Gruppen-Paginierung, Z2-Sortierung, Zaehlungen und die showDone-Zeilensemantik (Z3b).
/// </summary>
public class ProductionOrderRepositoryGroupedTests
{
    private static void Seed(ApplicationDbContext ctx, int id, string order, string sub,
        bool isDone = false, bool isCancelled = false, string? parent = null)
    {
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = id,
            OrderNumber = order,
            SubOrderNumber = sub,
            ParentSubOrderNumber = parent,
            Quantity = 1,
            IsDone = isDone,
            IsCancelled = isCancelled,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        });
    }

    [Fact]
    public async Task Grouped_PaginatesOverGroups_AndCountsBoth()
    {
        using var ctx = TestDbContextFactory.Create();
        // 3 Gruppen (A: 2 Sub-FAs, B: 1, C: 1) -> 3 Gruppen, 4 Zeilen.
        Seed(ctx, 1, "GRP-A", "A-2");
        Seed(ctx, 2, "GRP-A", "A-1");
        Seed(ctx, 3, "GRP-B", "B-1");
        Seed(ctx, 4, "GRP-C", "C-1");
        await ctx.SaveChangesAsync();
        var repo = new ProductionOrderRepository(ctx);

        var page1 = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 2);

        page1.TotalGroupCount.Should().Be(3);   // 3 HauptFA-Gruppen
        page1.TotalRowCount.Should().Be(4);      // 4 Sub-FAs (Z4 „N Auftraege · M Sub-FAs")
        page1.Groups.Should().HaveCount(2);      // Seite 1 = 2 Gruppen
        page1.Groups.Select(g => g.OrderNumber).Should().ContainInOrder("GRP-A", "GRP-B");
        // Z2: innerhalb der Gruppe SubOrderNumber aufsteigend
        page1.Groups[0].Rows.Select(r => r.SubOrderNumber).Should().ContainInOrder("A-1", "A-2");

        var page2 = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 2, pageSize: 2);
        page2.Groups.Should().ContainSingle().Which.OrderNumber.Should().Be("GRP-C");
    }

    [Fact]
    public async Task Grouped_ShowDoneFalse_DropsDoneRows_AndEmptyGroups()
    {
        using var ctx = TestDbContextFactory.Create();
        Seed(ctx, 1, "GRP-A", "A-1", isDone: false);
        Seed(ctx, 2, "GRP-A", "A-2", isDone: true);   // erledigt -> faellt bei showDone=false raus
        Seed(ctx, 3, "GRP-B", "B-1", isDone: true);   // ganze Gruppe erledigt -> verschwindet
        await ctx.SaveChangesAsync();
        var repo = new ProductionOrderRepository(ctx);

        var page = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: false, page: 1, pageSize: 25);

        // Nur GRP-A bleibt (mit ihrer einen offenen Zeile); GRP-B faellt auf 0 Zeilen -> weg (Z3b).
        page.TotalGroupCount.Should().Be(1);
        page.TotalRowCount.Should().Be(1);
        page.Groups.Should().ContainSingle().Which.OrderNumber.Should().Be("GRP-A");
        page.Groups[0].Rows.Should().ContainSingle().Which.SubOrderNumber.Should().Be("A-1");
    }

    [Fact]
    public async Task Grouped_ProjectsHierarchyFields()
    {
        using var ctx = TestDbContextFactory.Create();
        Seed(ctx, 1, "GRP-A", "A-1", parent: "ROOT-A");
        await ctx.SaveChangesAsync();
        var repo = new ProductionOrderRepository(ctx);

        var page = await repo.GetForLeitstandGroupedAsync(null, null, null, showDone: true, page: 1, pageSize: 25);

        var row = page.Groups[0].Rows[0];
        row.SubOrderNumber.Should().Be("A-1");
        row.ParentSubOrderNumber.Should().Be("ROOT-A");
    }
}
