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

    // v1.99.13 (Spec 2026-11-12-befunde-ake-regression-dev, Punkt 4c): FA-Texte am Wagen einzeln, jede Nummer einmal, sortiert —
    // sonst waechst der Text bei jedem „Trotzdem buchen“ („A; A;B; A;A;B;C …“).
    [Fact]
    public async Task GetProductionOrdersAtLocation_ZerlegtTexteInEinzelneNummern()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, articleId: 1, locationId: 1);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 1m, MovementType.Einbuchung, "A"),
            NewMovementWithOrder(1, 1, 1m, MovementType.Einbuchung, "A;B"),
            NewMovementWithOrder(1, 1, 1m, MovementType.Einbuchung, "A; A ;B;;C"),
            NewMovementWithOrder(1, 1, 1m, MovementType.Einbuchung, "D,C"));
        await ctx.SaveChangesAsync();

        var result = await new StockMovementRepository(ctx).GetProductionOrdersAtLocationAsync(1);

        result.Should().Equal("A", "B", "C", "D");
    }

    // v1.99.47 (Teil 6, AK 1–3, 24): FA-Bezug je Artikel am Platz – Spalte, Zugangs-Nummern, Zugang ohne FA.
    [Fact]
    public async Task GetProductionOrdersByArticle_SpalteZugangUndOhneFa()
    {
        using var ctx = TestDbContextFactory.Create();
        foreach (var a in new[] { 1, 2, 3, 4, 5, 6 }) SeedArticleAndLocation(ctx, a, 1);
        SeedArticleAndLocation(ctx, 1, 2);
        ctx.StockMovements.AddRange(
            NewMovementWithOrder(1, 1, 5m, MovementType.Einbuchung, "1234567"),            // A: nur Zugang mit FA
            NewMovement(2, 1, 5m, MovementType.Einbuchung),                                 // B: ohne FA
            NewMovementWithOrder(3, 1, 1m, MovementType.Einbuchung, "222"),                 // C: 111, 222 (+ Liste)
            NewMovementWithOrder(3, 1, 1m, MovementType.SageEinbuchung, "111;222"),
            NewMovementWithOrder(4, 1, 2m, MovementType.Einbuchung, "333"),                 // D: kein Restbestand
            NewMovement(4, 1, 2m, MovementType.Ausbuchung),
            NewMovementWithOrder(5, 1, 100m, MovementType.Einbuchung, ""),                  // Y: WE ohne FA (leerer Text)
            NewMovementWithOrder(5, 1, 5m, MovementType.Ausbuchung, "111"),                 //    dann Ausbuchung mit FA
            NewMovementWithOrder(6, 1, 3m, MovementType.Umbuchung, "777"));                 // Umbuchung-Ziel = Zugang
        var quelle = NewMovementWithOrder(1, 2, 1m, MovementType.Umbuchung, "888");        // Ziel Platz 2, Quelle Platz 1
        quelle.SourceStorageLocationId = 1;
        ctx.StockMovements.Add(quelle);
        await ctx.SaveChangesAsync();

        var r = await new StockMovementRepository(ctx).GetProductionOrdersByArticleAtLocationAsync(1);

        r.Keys.Should().BeEquivalentTo([1, 2, 3, 5, 6]);
        r[1].Nummern.Should().Equal("1234567");                // Umbuchungs-Quelle 888 nicht gelesen
        (r[1].ZugangOhneFa, r[1].ZugangNummern.Contains("1234567")).Should().Be((false, true));
        r[2].Nummern.Should().BeEmpty();
        r[2].ZugangOhneFa.Should().BeTrue();
        r[3].Nummern.Should().Equal("111", "222");
        r[5].Nummern.Should().Equal("111");
        (r[5].ZugangOhneFa, r[5].ZugangNummern.Count).Should().Be((true, 0));
        r[6].ZugangNummern.Should().Contain("777");
        (await new StockMovementRepository(ctx).GetProductionOrdersAtLocationAsync(1)).Should().Equal("111", "1234567", "222", "777");
    }

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
