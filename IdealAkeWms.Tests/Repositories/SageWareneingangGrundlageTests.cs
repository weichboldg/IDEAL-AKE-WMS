using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe, E1, AK 1-3): Ausschluss von Sage-WE-Wareneingaengen aus der
/// Lagerbuchungs-Queue an beiden Stellen (Decorator und Nacherfassung) und die Rest-Regel (Uebergebenes nicht doppelt abziehen).
/// </summary>
public class SageWareneingangGrundlageTests
{
    private static (SageBookingEnqueueingStockMovementRepository repo, IdealAkeWms.Data.ApplicationDbContext ctx, int artId, int locId) Build()
    {
        var ctx = TestDbContextFactory.Create();
        var loc = new StorageLocation
        {
            Code = "LL;1;4;0", Source = StorageLocationSource.Sage, SageBuchungErlaubt = true, SageLagerkennung = "LL;1;4;0", SageLagerplatzId = 42,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        };
        var art = new Article { ArticleNumber = "ART-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.StorageLocations.Add(loc);
        ctx.Articles.Add(art);
        ctx.ServiceSettings.Add(new ServiceSetting { Key = "SageLagerbuchungAktiv", Value = "true" });
        ctx.SaveChanges();
        var repo = new SageBookingEnqueueingStockMovementRepository(ctx, new ServiceSettingRepository(ctx), new SageBookingQueueRepository(ctx),
            NullLogger<SageBookingEnqueueingStockMovementRepository>.Instance);
        return (repo, ctx, art.Id, loc.Id);
    }

    private static StockMovement Wareneingang(int artId, int locId, SageWareneingangStatus status) => new()
    {
        ArticleId = artId, StorageLocationId = locId, Quantity = 2m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
        WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        PurchaseOrderReceipt = new PurchaseOrderReceipt
        {
            BelId = 1, BelPosId = 11, Belegnummer = "B-1", Quantity = 2m, Lieferart = PurchaseOrderReceiptLieferart.Teillieferung,
            SageWeStatus = status, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
        },
    };

    [Theory]
    [InlineData(SageWareneingangStatus.NichtNoetig, 1)]
    [InlineData(SageWareneingangStatus.Offen, 0)]
    [InlineData(SageWareneingangStatus.NurInSage, 0)]
    public async Task Decorator_SageWe_KeinQueueEintrag_NichtNoetig_WieTeil1(SageWareneingangStatus status, int erwartet)
    {
        var (repo, ctx, artId, locId) = Build();
        await repo.AddAsync(Wareneingang(artId, locId, status));
        (await ctx.SageBookingQueueItems.CountAsync()).Should().Be(erwartet);
    }

    [Fact]
    public async Task Nacherfassung_SageWe_NieAlsZugang_NichtNoetig_Schon()
    {
        var (_, ctx, artId, locId) = Build();
        var teil1 = Wareneingang(artId, locId, SageWareneingangStatus.NichtNoetig);
        var sageWe = Wareneingang(artId, locId, SageWareneingangStatus.Uebergeben);
        sageWe.PurchaseOrderReceipt!.BelPosId = 12;
        ctx.StockMovements.AddRange(teil1, sageWe);
        await ctx.SaveChangesAsync();

        (await new SageBookingQueueRepository(ctx).EnqueueMissingAsync(DateTime.Now.AddMinutes(-15), 50, umbuchungAktiv: false)).Should().Be(1);
        (await ctx.SageBookingQueueItems.Select(q => q.StockMovementId).ToListAsync()).Should().Equal(teil1.Id);
    }

    [Fact]
    public void Decision_HatSageWareneingang_SchliesstAus()
    {
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Einbuchung, true, false, true, false, hatSageWareneingang: true).Should().BeFalse();
        SageBookingEnqueueDecision.ShouldEnqueue(MovementType.Einbuchung, true, false, true, false, hatSageWareneingang: false).Should().BeTrue();
    }

    [Fact]
    public void RestRegel_UebergebenBisSyncedAt_NichtAbgezogen_SonstAbgezogen()
    {
        var sync = new DateTime(2026, 11, 11, 12, 0, 0);
        var stand = new ErfasstJeBelPos(10m, new List<(DateTime, decimal)> { (sync.AddMinutes(-1), 3m), (sync, 2m), (sync.AddMinutes(1), 4m) });
        // 3 und 2 sind im Sage-Stand (<= SyncedAt), 4 nach dem Abgleich uebergeben, 1 nicht uebergeben (anderer Status) -> 5 abziehen
        stand.Abzuziehen(sync).Should().Be(5m);
        ErfasstJeBelPos.Leer.Abzuziehen(sync).Should().Be(0m);
        // v1.99.10: EINE Rest-Funktion fuer Maske und Lieferverzug-Mail, nie negativ
        stand.Rest(8m, sync).Should().Be(3m);
        stand.Rest(4m, sync).Should().Be(0m);
        ErfasstJeBelPos.RestAus(4m, 5m).Should().Be(0m);
    }

    [Fact]
    public async Task Repository_LiefertGesamtUndNurUebergebeneMitZeitpunkt()
    {
        var (_, ctx, artId, locId) = Build();
        var am = new DateTime(2026, 11, 11, 10, 0, 0);
        var a = Wareneingang(artId, locId, SageWareneingangStatus.Uebergeben);
        a.PurchaseOrderReceipt!.SageWeAm = am;
        var b = Wareneingang(artId, locId, SageWareneingangStatus.Fehler);
        b.PurchaseOrderReceipt!.SageWeAm = am;   // Fehler zaehlt nie als enthalten
        ctx.StockMovements.AddRange(a, b);
        await ctx.SaveChangesAsync();

        var e = (await new PurchaseOrderReceiptRepository(ctx).GetErfasstJeBelPosAsync(new[] { 11 }))[11];
        e.Gesamt.Should().Be(4m);
        e.Uebergeben.Should().Equal((am, 2m));
        e.Abzuziehen(am).Should().Be(2m);
        e.Abzuziehen(am.AddSeconds(-1)).Should().Be(4m);
    }
}
