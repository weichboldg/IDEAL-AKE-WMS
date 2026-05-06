using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Aggregations-Audit-Tests: Stellen sicher, dass alle Bestands-Aggregationen
/// die neuen MovementType-Werte SageEinbuchung (3) und SageAusbuchung (4)
/// korrekt verrechnen.
/// </summary>
public class StockMovementRepositoryAggregationTests
{
    [Fact]
    public async Task GetCurrentStockAsync_AppliesSageEinbuchungAsPlus()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetCurrentStockAsync();

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(13m);
    }

    [Fact]
    public async Task GetCurrentStockAsync_AppliesSageAusbuchungAsMinus()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 4m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetCurrentStockAsync();

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(6m);
    }

    [Fact]
    public async Task GetStockByProductionOrderAsync_HandlesSageMovements()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung, orderNumber: "FA-100"),
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung, orderNumber: "FA-100"),
            NewMovementWithOrder(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung, orderNumber: "FA-100")
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync("FA-100");

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(11m);
    }

    [Fact]
    public async Task GetCurrentStockAtLocationAsync_HandlesSageMovements()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetCurrentStockAtLocationAsync(articleId: 1, storageLocationId: 1);

        result.Should().Be(11m);
    }

    [Fact]
    public async Task GetStockByArticleNumbersAsync_HandlesSageMovements()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 3m, MovementType.SageEinbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByArticleNumbersAsync(new List<string> { "A-001" });

        result.Should().ContainKey("A-001");
        result["A-001"].Should().ContainSingle();
        result["A-001"][0].Quantity.Should().Be(11m);
    }

    [Fact]
    public async Task GetMovementHistoryAsync_MovementTypeName_MapsSageValues()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 5m, MovementType.SageEinbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 2m, MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, _) = await repo.GetMovementHistoryAsync();

        items.Should().HaveCount(2);
        items.Should().Contain(i => i.MovementTypeName == "Sage-Einbuchung");
        items.Should().Contain(i => i.MovementTypeName == "Sage-Ausbuchung");
    }

    [Fact]
    public async Task GetMovementHistoryAsync_ReturnsNote()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = 1,
            StorageLocationId = 1,
            Quantity = 5m,
            MovementType = MovementType.SageEinbuchung,
            Note = "Sage-Korrektur: WMS=0, Sage=5, Diff=+5",
            Timestamp = DateTime.Now,
            WindowsUser = "system:sync",
            CreatedAt = DateTime.Now,
            CreatedBy = "system:sync",
            CreatedByWindows = "MACHINE"
        });
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, _) = await repo.GetMovementHistoryAsync();

        items.Should().ContainSingle();
        items[0].Note.Should().Be("Sage-Korrektur: WMS=0, Sage=5, Diff=+5");
    }

    [Fact]
    public async Task GetCurrentStockByArticleAndLocationAsync_AggregatesAllMovementTypes()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        SeedArticleAndLocation(ctx, articleId: 2, locationId: 1);
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 2);
        ctx.StockMovements.AddRange(
            NewMovement(articleId: 1, locationId: 1, qty: 10m, MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 3m,  MovementType.SageEinbuchung),
            NewMovement(articleId: 1, locationId: 1, qty: 4m,  MovementType.Ausbuchung),
            NewMovement(articleId: 2, locationId: 1, qty: 5m,  MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 2, qty: 7m,  MovementType.Einbuchung),
            NewMovement(articleId: 1, locationId: 2, qty: 2m,  MovementType.SageAusbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var stock = await repo.GetCurrentStockByArticleAndLocationAsync();

        stock.Should().HaveCount(3);
        stock[(1, 1)].Should().Be(9m);   // 10 + 3 - 4
        stock[(2, 1)].Should().Be(5m);
        stock[(1, 2)].Should().Be(5m);   // 7 - 2
    }

    private static void SeedArticleAndLocation(IdealAkeWms.Data.ApplicationDbContext ctx, int articleId, int locationId)
    {
        if (ctx.Articles.Find(articleId) == null)
        {
            ctx.Articles.Add(new Article
            {
                Id = articleId,
                ArticleNumber = $"A-{articleId:000}",
                Description = "Test",
                Unit = "Stk",
                CreatedBy = "tester",
                CreatedByWindows = "tester"
            });
        }
        if (ctx.StorageLocations.Find(locationId) == null)
        {
            ctx.StorageLocations.Add(new StorageLocation
            {
                Id = locationId,
                Code = $"L-{locationId:000}",
                BarcodeValue = $"L-{locationId:000}",
                IsActive = true,
                IsPickingTransport = false,
                Source = StorageLocationSource.Sage,
                CreatedBy = "tester",
                CreatedByWindows = "tester"
            });
        }
    }

    private static StockMovement NewMovement(int articleId, int locationId, decimal qty, MovementType type) => new()
    {
        ArticleId = articleId,
        StorageLocationId = locationId,
        Quantity = qty,
        MovementType = type,
        Timestamp = DateTime.Now,
        WindowsUser = "tester",
        CreatedAt = DateTime.Now,
        CreatedBy = "tester",
        CreatedByWindows = "tester"
    };

    private static StockMovement NewMovementWithOrder(int articleId, int locationId, decimal qty, MovementType type, string orderNumber)
    {
        var m = NewMovement(articleId, locationId, qty, type);
        m.ProductionOrder = orderNumber;
        return m;
    }
}
