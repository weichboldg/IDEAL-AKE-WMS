using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class OseonProductionOrderRepositoryArticleFilterTests
{
    private static OseonProductionOrder NewOrder(long oseonId, string? articleNumber,
        string custOrder = "K-100", string faNumber = "FA-100")
        => new()
        {
            OseonId = oseonId,
            OseonOrderNumber = faNumber,
            CustomerOrderNumber = custOrder,
            OseonStatus = 30,
            ArticleNumber = articleNumber,
            DueDate = DateTime.Today,
            LastChangedInOseon = DateTime.Now,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };

    [Fact]
    public async Task Filter_ByArticleNumber_ReturnsExactMatch()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-999", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "ART-100");

        result.Items.Should().HaveCount(1);
        result.Items[0].ArticleNumber.Should().Be("ART-100");
    }

    [Fact]
    public async Task Filter_ByArticleNumber_ReturnsContainsMatch()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-1001", "K-2", "FA-2"));
        ctx.OseonProductionOrders.Add(NewOrder(3, "OTHER", "K-3", "FA-3"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "100");

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.ArticleNumber).Should().BeEquivalentTo(new[] { "ART-100", "ART-1001" });
    }

    [Fact]
    public async Task Filter_ByArticleNumber_IgnoresOrdersWithNullArticleNumber()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, null, "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-100", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "ART");

        result.Items.Should().HaveCount(1);
        result.Items[0].ArticleNumber.Should().Be("ART-100");
    }

    [Fact]
    public async Task Filter_ByArticleNumber_WhitespaceOnly_TreatedAsNoFilter()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-A", "K-1", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-B", "K-2", "FA-2"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync(null, null, false, 1, 25, null, "   ");

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Filter_ByArticleNumber_CombinedWithSearchTerm_AllConjunctive()
    {
        // Repo lädt nach Gruppen-Match alle Sub-Orders der Gruppe (CustomerOrderNumber).
        // Konjunktivität wird über TotalGroupCount geprüft: nur Gruppe K-MATCH passt zu beiden Filtern.
        var ctx = TestDbContextFactory.Create();
        ctx.OseonProductionOrders.Add(NewOrder(1, "ART-100", "K-MATCH", "FA-1"));
        ctx.OseonProductionOrders.Add(NewOrder(2, "ART-100", "K-OTHER", "FA-2"));
        ctx.OseonProductionOrders.Add(NewOrder(3, "ART-999", "K-MATCH", "FA-3"));
        await ctx.SaveChangesAsync();

        var repo = new OseonProductionOrderRepository(ctx);
        var result = await repo.GetPagedAsync("K-MATCH", null, false, 1, 25, null, "ART-100");

        result.TotalGroupCount.Should().Be(1, "only K-MATCH has an order matching both filters");
        result.Items.Should().HaveCount(2, "Group-Pagination: alle Sub-Orders der matchenden Gruppe K-MATCH werden geladen");
        result.Items.Should().Contain(i => i.OseonOrderNumber == "FA-1");
        result.Items.Should().Contain(i => i.OseonOrderNumber == "FA-3");
        result.Items.Should().NotContain(i => i.CustomerOrderNumber == "K-OTHER");
    }
}
