using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.78 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 5): Tageshistorie = Buchungen des Werkers heute
/// plus ALLE nicht stornierten Teilmeldungen des Tages (§69: teil + fertig; §67: FA- und Lagerbestell-Meldungen).</summary>
public class BdeTageshistorieTests
{
    private static BdeTeilMeldung Meldung(BdeBookingTestSeed.Ids ids, DateTime am, bool fertig, int subFa, int? lagerItem = null, bool storniert = false, int? bookingId = null)
        => new()
        {
            HauptFA = 100, SubFA = subFa, Position = 1, Artnr = "A-1", Matchcode = "Blech", Kuerzel = "KA",
            ProductionWorkplaceId = ids.WorkplaceId, BdeOperatorId = ids.OperatorId, BdeTerminalId = ids.TerminalId,
            GoodQuantity = 3, ScrapQuantity = 1, SollmengeBeiMeldung = 5, GemeldetAm = am, IstAbschliessend = fertig,
            WarehouseRequisitionItemId = lagerItem, IsCancelled = storniert, BdeBookingId = bookingId,
            CreatedAt = am, CreatedBy = "t", CreatedByWindows = "t"
        };

    [Fact]
    public async Task NurHeute_NurDieserWerker_OhneStornierte_NeuesteZuerst()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var anderer = await BdeBookingTestSeed.AddSecondOperatorAsync(ctx);
        var heute = DateTime.Today;
        var ruesten = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Setup, BdeBookingStatus.Finished, heute.AddHours(7), heute.AddHours(7.5));
        var produktion = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Running, heute.AddHours(8));
        var gestern = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, heute.AddHours(-3), heute.AddHours(-2));
        var storniert = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Activity, BdeBookingStatus.Finished, heute.AddHours(9), heute.AddHours(9.2), cancelled: true);
        var fremd = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, heute.AddHours(9), heute.AddHours(10));
        fremd.BdeOperatorId = anderer;
        ctx.BdeBookings.AddRange(ruesten, produktion, gestern, storniert, fremd);
        await ctx.SaveChangesAsync();
        ctx.BdeBookingQuantities.Add(new BdeBookingQuantity { BdeBookingId = ruesten.Id, BdeOperatorId = ids.OperatorId, GoodQuantity = 2, ScrapQuantity = 0, ReportedAt = heute.AddHours(7.5), CreatedAt = heute, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.BdeTeilMeldungen.AddRange(
            Meldung(ids, heute.AddHours(10), fertig: false, subFa: 11),
            Meldung(ids, heute.AddHours(11), fertig: true, subFa: 12),
            Meldung(ids, heute.AddHours(12), fertig: true, subFa: 13, storniert: true),
            Meldung(ids, heute.AddHours(-1), fertig: true, subFa: 14));
        await ctx.SaveChangesAsync();

        var z = await BdeTageshistorie.LadenAsync(ctx, ids.OperatorId, heute);

        z.Select(x => x.Typ).Should().Equal("Fertig", "Teilfertig", "Produktion", "Rüsten");
        z.Select(x => x.Art).Should().Equal("Teilmeldung", "Teilmeldung", "Buchung", "Buchung");
        z[3].Gut.Should().Be(2);
        z[3].DauerMinuten.Should().Be(30);
        z[3].Status.Should().Be("beendet");
        z[2].Ende.Should().BeNull();
        z[2].Status.Should().Be("läuft");
        z[0].Ziel.Should().Be("FA 100 / Sub-FA 12 · Pos. 1 · Blech");
        z[0].Gut.Should().Be(3);
        z[0].Werkbank.Should().Be("Werkbank");
    }

    [Fact]
    public async Task VerknuepfteBaugruppenBuchung_NurEineZeile_LagerbestellungGekennzeichnet()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var heute = DateTime.Today;
        var agFertig = BdeBookingTestSeed.NewBooking(ids, BdeBookingType.Production, BdeBookingStatus.Finished, heute.AddHours(8), heute.AddHours(8));
        ctx.BdeBookings.Add(agFertig);
        await ctx.SaveChangesAsync();
        ctx.BdeTeilMeldungen.AddRange(
            Meldung(ids, heute.AddHours(8), fertig: true, subFa: 11, bookingId: agFertig.Id),
            Meldung(ids, heute.AddHours(9), fertig: false, subFa: 12, lagerItem: 77));
        await ctx.SaveChangesAsync();

        var z = await BdeTageshistorie.LadenAsync(ctx, ids.OperatorId, heute);

        z.Should().HaveCount(2, "die AG-Fertigbuchung der Baugruppe haengt an der Teilmeldung und erscheint nicht doppelt");
        z.Should().OnlyContain(x => x.Art == "Teilmeldung");
        z[0].Ziel.Should().StartWith("Lagerbestellung · ");
    }

    // v1.99.79 F-34: 0 = „ohne“ – keine „FA 0“/„Sub-FA 0“ in der Ziel-Spalte
    [Fact]
    public async Task NullNummern_NichtAngezeigt()
    {
        var ctx = TestDbContextFactory.Create();
        var ids = await BdeBookingTestSeed.SeedAsync(ctx);
        var heute = DateTime.Today;
        var blech = Meldung(ids, heute.AddHours(9), fertig: true, subFa: 0);
        var lager = Meldung(ids, heute.AddHours(8), fertig: true, subFa: 0, lagerItem: 77);
        lager.HauptFA = 0; lager.Position = null;
        ctx.BdeTeilMeldungen.AddRange(blech, lager);
        await ctx.SaveChangesAsync();

        var z = await BdeTageshistorie.LadenAsync(ctx, ids.OperatorId, heute);

        z.Select(x => x.Ziel).Should().Equal("FA 100 · Pos. 1 · Blech", "Lagerbestellung · Blech");
    }

    [Fact]
    public void Spalten_RendernWieDieZelle()
    {
        var zeile = new BdeTageshistorieZeile("Buchung", "Rüsten", new DateTime(2026, 11, 25, 7, 5, 0), new DateTime(2026, 11, 25, 7, 35, 0),
            "FA1/10 — Kanten", "Kanten", 30, 2.5m, 0m, "beendet");
        var sp = BdeTageshistorie.Spalten;
        sp["zeit"](zeile).Should().Be("07:05–07:35");
        sp["dauer"](zeile).Should().Be("30");
        sp["gut"](zeile).Should().Be("2,5");
        sp["ausschuss"](zeile).Should().Be("0");
        sp.Keys.Should().Equal("zeit", "art", "typ", "ziel", "werkbank", "dauer", "gut", "ausschuss", "status");
    }
}
