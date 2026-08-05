using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer den onlyActualStock-Parameter von GetStockByProductionOrderAsync (Teil-1-Bugfix,
/// [[2026-08-05-wms-bugs-improvements-teil-1-spec]]).
/// true-Pfad (Default, Einbuchungs-Hinweis + Tracking-Modal): Ist-Bestand ueber ALLE Bewegungen,
/// nur > 0. false-Pfad (StockOverview-FA-Filter): historische FA-getaggte Netto-Summe, bit-identisch.
/// </summary>
public class StockMovementRepositoryProductionOrderTests
{
    private const string Fa = "1234567";

    // --- true-Pfad (Default) ---------------------------------------------------------------

    [Fact] // AK1: getaggt eingebucht, ungetaggt komplett ausgebucht -> keine Zeile mehr
    public async Task GetStockByProductionOrder_TruePath_TaggedInThenUntaggedOut_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa),
            NewMovement(1, 1, 5m, MovementType.Ausbuchung) // ungetaggt
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa); // Default true

        result.Should().BeEmpty();
    }

    [Fact] // AK2: getaggt eingebucht, unveraendert liegen geblieben -> Menge 5
    public async Task GetStockByProductionOrder_TruePath_TaggedInOnly_ReturnsFullQuantity()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.Add(NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa));
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(5m);
    }

    [Fact] // AK3: getaggt eingebucht 5, ungetaggt 2 ausgebucht -> realer Restbestand 3
    public async Task GetStockByProductionOrder_TruePath_PartialUntaggedOut_ReturnsRealRemainder()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa),
            NewMovement(1, 1, 2m, MovementType.Ausbuchung) // ungetaggt
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(3m);
    }

    [Fact] // AK7: gemischter Platz -> angezeigte Menge = realer Platz-Bestand, nicht FA-Anteil
    public async Task GetStockByProductionOrder_TruePath_MixedLocation_ShowsPlaceStockNotFaShare()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa),      // FA-getaggt
            NewMovement(1, 1, 3m, MovementType.Einbuchung)                    // ungetaggt, gleicher Platz
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(8m); // realer Platz-Bestand, nicht der FA-Anteil (5)
    }

    // --- false-Pfad (historisch, Regressionsschutz) ----------------------------------------

    [Fact] // AK6: false-Pfad zeigt Phantom-Zeile weiterhin (FA-getaggte Netto-Summe)
    public async Task GetStockByProductionOrder_FalsePath_TaggedInThenUntaggedOut_StillShowsRow()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa),
            NewMovement(1, 1, 5m, MovementType.Ausbuchung) // ungetaggt -> zaehlt im false-Pfad NICHT
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa, onlyActualStock: false);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(5m); // Phantom-Menge, bewusst unveraendert
    }

    [Fact] // false-Pfad: gemischter Platz zeigt NUR den FA-Anteil (im Gegensatz zu AK7)
    public async Task GetStockByProductionOrder_FalsePath_MixedLocation_ShowsOnlyFaShare()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, Fa),
            NewMovement(1, 1, 3m, MovementType.Einbuchung) // ungetaggt -> im false-Pfad ignoriert
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByProductionOrderAsync(Fa, onlyActualStock: false);

        result.Should().ContainSingle();
        result[0].CurrentQuantity.Should().Be(5m); // nur FA-getaggte Summe
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
