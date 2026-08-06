using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer die funktionsfaehigen Spaltenfilter „Bewegungsart" (movement-type) und „Datum/Zeit"
/// (datetime) der Bewegungshistorie (Teil-4, korrigiert: Filter wieder funktionsfaehig statt entfernt).
/// </summary>
public class StockMovementRepositoryMovementFilterTests
{
    // --- Bewegungsart ----------------------------------------------------------------------

    [Fact]
    public async Task MovementTypeFilter_Ausbuchung_MatchesAusbuchungAndSageAusbuchung()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            Mov(1, 1, 1m, MovementType.Einbuchung),
            Mov(1, 1, 1m, MovementType.Ausbuchung),
            Mov(1, 1, 1m, MovementType.SageAusbuchung),
            Mov(1, 1, 1m, MovementType.Umbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["movement-type"] = "ausbuchung" });

        total.Should().Be(2);
        items.Select(i => i.MovementType).Should().BeEquivalentTo(new[]
        {
            MovementType.Ausbuchung, MovementType.SageAusbuchung
        });
    }

    [Fact]
    public async Task MovementTypeFilter_Einbuchung_DoesNotReturnAusbuchung()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            Mov(1, 1, 1m, MovementType.Einbuchung),
            Mov(1, 1, 1m, MovementType.SageEinbuchung),
            Mov(1, 1, 1m, MovementType.Ausbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["movement-type"] = "einbuchung" });

        total.Should().Be(2);
        items.Should().OnlyContain(i =>
            i.MovementType == MovementType.Einbuchung || i.MovementType == MovementType.SageEinbuchung);
    }

    [Fact]
    public async Task MovementTypeFilter_Negated_ExcludesMatches()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            Mov(1, 1, 1m, MovementType.Einbuchung),
            Mov(1, 1, 1m, MovementType.Ausbuchung),
            Mov(1, 1, 1m, MovementType.Umbuchung)
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["movement-type"] = "!einbuchung" });

        total.Should().Be(2);
        items.Should().NotContain(i => i.MovementType == MovementType.Einbuchung);
    }

    [Fact]
    public async Task MovementTypeFilter_NoMatchingType_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.Add(Mov(1, 1, 1m, MovementType.Einbuchung));
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (_, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["movement-type"] = "xyz" });

        total.Should().Be(0);
    }

    // --- Datum/Zeit ------------------------------------------------------------------------

    [Fact]
    public async Task DateFilter_FullDay_MatchesOnlyThatDay()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            MovAt(1, 1, new DateTime(2026, 8, 6, 10, 0, 0)),
            MovAt(1, 1, new DateTime(2026, 8, 6, 23, 59, 0)),
            MovAt(1, 1, new DateTime(2026, 8, 7, 0, 1, 0))
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["datetime"] = "06.08.2026" });

        total.Should().Be(2);
        items.Should().OnlyContain(i => i.Timestamp.Date == new DateTime(2026, 8, 6));
    }

    [Fact]
    public async Task DateFilter_Year_MatchesOnlyThatYear()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            MovAt(1, 1, new DateTime(2025, 12, 31, 10, 0, 0)),
            MovAt(1, 1, new DateTime(2026, 1, 1, 0, 0, 0)),
            MovAt(1, 1, new DateTime(2026, 8, 6, 10, 0, 0))
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["datetime"] = "2026" });

        total.Should().Be(2);
        items.Should().OnlyContain(i => i.Timestamp.Year == 2026);
    }

    [Fact]
    public async Task DateFilter_Month_MatchesOnlyThatMonth()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedArticleAndLocation(ctx, 1, 1);
        ctx.StockMovements.AddRange(
            MovAt(1, 1, new DateTime(2026, 7, 31, 10, 0, 0)),
            MovAt(1, 1, new DateTime(2026, 8, 1, 0, 0, 0)),
            MovAt(1, 1, new DateTime(2026, 8, 31, 23, 0, 0))
        );
        await ctx.SaveChangesAsync();
        var repo = new StockMovementRepository(ctx);

        var (items, total) = await repo.GetMovementHistoryAsync(
            columnFilters: new Dictionary<string, string> { ["datetime"] = "08.2026" });

        total.Should().Be(2);
        items.Should().OnlyContain(i => i.Timestamp.Month == 8 && i.Timestamp.Year == 2026);
    }

    private static void SeedArticleAndLocation(IdealAkeWms.Data.ApplicationDbContext ctx, int articleId, int locationId)
    {
        if (ctx.Articles.Find(articleId) == null)
            ctx.Articles.Add(new Article
            {
                Id = articleId,
                ArticleNumber = $"A-{articleId:000}",
                Description = "Test",
                Unit = "Stk",
                CreatedBy = "tester",
                CreatedByWindows = "tester"
            });
        if (ctx.StorageLocations.Find(locationId) == null)
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

    private static StockMovement Mov(int articleId, int locationId, decimal qty, MovementType type)
        => MovAt(articleId, locationId, DateTime.Now, qty, type);

    private static StockMovement MovAt(int articleId, int locationId, DateTime timestamp,
        decimal qty = 1m, MovementType type = MovementType.Einbuchung) => new()
    {
        ArticleId = articleId,
        StorageLocationId = locationId,
        Quantity = qty,
        MovementType = type,
        Timestamp = timestamp,
        WindowsUser = "tester",
        CreatedAt = DateTime.Now,
        CreatedBy = "tester",
        CreatedByWindows = "tester"
    };
}
