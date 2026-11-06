using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Integrationstests des Enqueue-Decorators auf der InMemory-DB: prueft das Gating
/// (globaler Toggle UND Lagerplatz-Flag), den Bewegungsart-Filter (Feedback-Loop-Schutz)
/// und die Pass-through-Zusicherung (S7: bei Toggle aus entsteht kein Queue-Eintrag).
/// </summary>
public class SageBookingEnqueueingStockMovementRepositoryTests
{
    private static (SageBookingEnqueueingStockMovementRepository repo, IdealAkeWms.Data.ApplicationDbContext ctx, int articleId, int locationId)
        Build(bool toggleOn, bool locationAllows)
    {
        var ctx = TestDbContextFactory.Create();

        var loc = new StorageLocation
        {
            Code = "LL;1;4;0",
            Source = StorageLocationSource.Sage,
            SageBuchungErlaubt = locationAllows,
            SageLagerkennung = "LL;1;4;0",
            SageLagerplatzId = 42,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var art = new Article { ArticleNumber = "ART-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.StorageLocations.Add(loc);
        ctx.Articles.Add(art);
        ctx.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungAktiv", Value = toggleOn ? "true" : "false" });
        ctx.SaveChanges();

        var repo = new SageBookingEnqueueingStockMovementRepository(
            ctx,
            new ServiceSettingRepository(ctx),
            new SageBookingQueueRepository(ctx),
            NullLogger<SageBookingEnqueueingStockMovementRepository>.Instance);

        return (repo, ctx, art.Id, loc.Id);
    }

    private static StockMovement Movement(int articleId, int locationId, MovementType type) => new()
    {
        ArticleId = articleId,
        StorageLocationId = locationId,
        Quantity = 3m,
        MovementType = type,
        Timestamp = DateTime.Now,
        WindowsUser = "t",
        CreatedAt = DateTime.Now, CreatedBy = "tester", CreatedByWindows = "t"
    };

    [Fact]
    public async Task AddAsync_BothOn_Einbuchung_CreatesOpenQueueItem()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);

        var saved = await repo.AddAsync(Movement(artId, locId, MovementType.Einbuchung));

        var items = await ctx.SageBookingQueueItems.ToListAsync();
        items.Should().ContainSingle();
        items[0].StockMovementId.Should().Be(saved.Id);
        items[0].Status.Should().Be(SageBookingQueueStatus.Offen);
    }

    [Fact]
    public async Task AddAsync_ToggleOff_CreatesNoQueueItem_ButStillSavesMovement()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: false, locationAllows: true);

        var saved = await repo.AddAsync(Movement(artId, locId, MovementType.Ausbuchung));

        (await ctx.StockMovements.CountAsync()).Should().Be(1);   // Buchung bleibt (Pass-through)
        saved.Id.Should().BeGreaterThan(0);
        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_LocationFlagOff_CreatesNoQueueItem()
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: false);

        await repo.AddAsync(Movement(artId, locId, MovementType.Einbuchung));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(MovementType.SageEinbuchung)]
    [InlineData(MovementType.SageAusbuchung)]
    [InlineData(MovementType.Umbuchung)]
    public async Task AddAsync_NonBookableType_CreatesNoQueueItem_EvenWithBothOn(MovementType type)
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);

        await repo.AddAsync(Movement(artId, locId, type));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task EnqueueAsync_SameMovementTwice_CreatesOnlyOneItem()
    {
        // App-Layer-Guard gegen Doppel-Enqueue (Web-Decorator vs. Reconciliation-Sweep) —
        // InMemory erzwingt den UNIQUE-Index nicht, daher der explizite Guard.
        var (_, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);
        var queue = new SageBookingQueueRepository(ctx);
        var mv = Movement(artId, locId, MovementType.Einbuchung);
        ctx.StockMovements.Add(mv);
        await ctx.SaveChangesAsync();

        var first = await queue.EnqueueAsync(mv);
        var second = await queue.EnqueueAsync(mv);

        second.Id.Should().Be(first.Id);
        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(1);
    }

    // ----- Umbuchung (v1.73.0 E7, Spec F9): Decorator + Nacherfassungs-Lauf nach derselben Regel -----

    private static int Platz(IdealAkeWms.Data.ApplicationDbContext ctx, string code, bool sage)
    {
        var loc = new StorageLocation
        {
            Code = code, Source = StorageLocationSource.Sage, SageBuchungErlaubt = sage,
            SageLagerkennung = sage ? "LL" : null, SageLagerplatzId = sage ? 100 + ctx.StorageLocations.Count() : null,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        ctx.StorageLocations.Add(loc);
        ctx.SaveChanges();
        return loc.Id;
    }

    private static StockMovement Umbuchung(int articleId, int? herkunft, int ziel)
    {
        var m = Movement(articleId, ziel, MovementType.Umbuchung);
        m.SourceStorageLocationId = herkunft;
        return m;
    }

    [Theory]
    [InlineData(true, true, 1)]   // Sage -> Sage
    [InlineData(true, false, 1)]  // Sage -> WMS-only (Entnahme)
    [InlineData(false, true, 1)]  // WMS-only -> Sage (Zugang)
    [InlineData(false, false, 0)] // WMS-only -> WMS-only
    public async Task AddAsync_Umbuchung_SchalterAn_NachPlatzFlags(bool herkunftSage, bool zielSage, int erwartet)
    {
        var (repo, ctx, artId, _) = Build(toggleOn: true, locationAllows: true);
        ctx.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungUmbuchungAktiv", Value = "true" });
        ctx.SaveChanges();
        var von = Platz(ctx, "VON", herkunftSage);
        var nach = Platz(ctx, "NACH", zielSage);

        await repo.AddAsync(Umbuchung(artId, von, nach));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(erwartet);
    }

    [Theory]
    [InlineData(null)]     // Key fehlt (Stand nach Deploy vor dem Seeding)
    [InlineData("false")]  // Default
    public async Task AddAsync_Umbuchung_SchalterAus_KeinEintrag(string? wert)
    {
        var (repo, ctx, artId, locId) = Build(toggleOn: true, locationAllows: true);
        if (wert is not null) ctx.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungUmbuchungAktiv", Value = wert });
        ctx.SaveChanges();

        await repo.AddAsync(Umbuchung(artId, Platz(ctx, "VON", true), locId));

        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task EnqueueMissing_UmbuchungNurMitSchalter_NachPlatzFlags_NieSageKorrektur()
    {
        var (_, ctx, artId, sageId) = Build(toggleOn: true, locationAllows: true);
        var wms = Platz(ctx, "WMS", false);
        var sage2 = Platz(ctx, "SAGE2", true);
        var ein = Movement(artId, sageId, MovementType.Einbuchung);
        var sageSage = Umbuchung(artId, sageId, sage2);
        var sageWms = Umbuchung(artId, sageId, wms);
        var wmsSage = Umbuchung(artId, wms, sageId);
        var wmsWms = Umbuchung(artId, wms, wms);
        var korrektur = Movement(artId, sageId, MovementType.SageEinbuchung);
        ctx.StockMovements.AddRange(ein, sageSage, sageWms, wmsSage, wmsWms, korrektur);
        await ctx.SaveChangesAsync();
        var queue = new SageBookingQueueRepository(ctx);

        (await queue.EnqueueMissingAsync(DateTime.Now.AddMinutes(-15), 50, umbuchungAktiv: false)).Should().Be(1, "Schalter aus = nur Ein-/Ausbuchung wie bisher");
        (await queue.EnqueueMissingAsync(DateTime.Now.AddMinutes(-15), 50, umbuchungAktiv: true)).Should().Be(3);

        (await ctx.SageBookingQueueItems.Select(q => q.StockMovementId).ToListAsync())
            .Should().BeEquivalentTo(new[] { ein.Id, sageSage.Id, sageWms.Id, wmsSage.Id });
    }
}
