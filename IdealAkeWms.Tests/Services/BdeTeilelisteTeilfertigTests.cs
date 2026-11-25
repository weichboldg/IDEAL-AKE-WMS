using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;
using static IdealAkeWms.Tests.Services.BdeTeilelisteMeldenTests;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.75 (Spec 2026-11-25-bde-masken-vereinheitlichung Etappe 3, ADR 0037): Teilfertigmeldung mit Restmenge – mehrere
/// Meldungen je Schluessel, nur die abschliessende ist eindeutig; Baugruppe als Teil-Buchung (IsFinal = false).</summary>
public class BdeTeilelisteTeilfertigTests
{
    private static BdeTeilelisteService.MeldeAuftrag Teil(TeilSchluessel s, decimal? gut, decimal? ausschuss = null) =>
        new(s, gut, ausschuss, Abschliessend: false);
    private static BdeTeilelisteService.MeldeAuftrag Fertig(TeilSchluessel s, decimal? gut = null, decimal? ausschuss = null) =>
        new(s, gut, ausschuss);

    private static Task<BdeTeilelisteService.MeldeErgebnis> Melden(Umgebung u, params BdeTeilelisteService.MeldeAuftrag[] a) =>
        u.Svc.MeldenAsync(u.Ids.OperatorId, u.Ids.WorkplaceId, u.Ids.TerminalId, a);

    private static async Task<TeilelisteZeile> ZeileAsync(Umgebung u, TeilSchluessel s) =>
        (await u.Svc.LadenAsync("KA", null, true, null)).Zeilen.Single(z => z.Schluessel == s);

    [Fact]
    public async Task Teilfertig_LaesstZeileOffen_SummeGutUndRest()
    {
        var u = await AufbauAsync(); // Blech 1: Soll 4

        (await Melden(u, Teil(Blech1, 1))).AllesGut.Should().BeTrue();
        (await Melden(u, Teil(Blech1, 2))).AllesGut.Should().BeTrue();

        u.Ctx.BdeTeilMeldungen.Should().HaveCount(2).And.OnlyContain(m => !m.IstAbschliessend);
        var z = await ZeileAsync(u, Blech1);
        (z.Meldbar, z.Gemeldet, z.Rest).Should().Be((true, 3m, 1m));
        z.Meldungen.Should().HaveCount(2);
        z.Abschluss.Should().BeNull();
    }

    [Fact]
    public async Task Fertig_OhneMenge_MeldetRestUndSchliesst_ZweitesFertigBereitsGemeldet()
    {
        var u = await AufbauAsync();
        await Melden(u, Teil(Blech1, 3));

        var r = await Melden(u, Fertig(Blech1));

        r.AllesGut.Should().BeTrue(r.Text());
        var abschluss = u.Ctx.BdeTeilMeldungen.Single(m => m.IstAbschliessend);
        abschluss.GoodQuantity.Should().Be(1, "Vorgabe = Rest (Soll 4 - 3)");
        (await ZeileAsync(u, Blech1)).Meldbar.Should().BeFalse();
        var zweit = await Melden(u, Fertig(Blech1, 1));
        zweit.BereitsGemeldet.Should().ContainSingle();
        (await Melden(u, Teil(Blech1, 1))).BereitsGemeldet.Should().ContainSingle("auch Teilfertig auf eine geschlossene Zeile wird abgewiesen");
        u.Ctx.BdeTeilMeldungen.Should().HaveCount(2);
    }

    [Fact]
    public async Task Fertig_MitWenigerAlsRest_SchliesstTrotzdem()
    {
        var u = await AufbauAsync();

        (await Melden(u, Fertig(Blech1, 1))).AllesGut.Should().BeTrue();

        (await ZeileAsync(u, Blech1)).Meldbar.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task Teilfertig_OhneGutMenge_Abgewiesen(int? gut)
    {
        var u = await AufbauAsync();

        var r = await Melden(u, Teil(Blech1, gut, 1));

        r.Abgewiesen.Should().ContainSingle().Which.Should().Contain("Teilfertig");
        u.Ctx.BdeTeilMeldungen.Should().BeEmpty();
    }

    [Fact]
    public async Task Uebermeldung_Teilfertig_GespeichertMitHinweis_FertigDanachMitNull()
    {
        var u = await AufbauAsync();
        await Melden(u, Teil(Blech1, 3));

        var r = await Melden(u, Teil(Blech1, 2)); // Summe 5 >= Soll 4

        r.Gemeldet.Should().Be(1);
        r.Hinweise.Should().ContainSingle().Which.Should().Contain("Sollmenge erreicht – bitte Fertig melden");
        var z = await ZeileAsync(u, Blech1);
        (z.Meldbar, z.Gemeldet, z.Rest).Should().Be((true, 5m, 0m));

        var fertig = await Melden(u, Fertig(Blech1));
        fertig.AllesGut.Should().BeTrue(fertig.Text());
        u.Ctx.BdeTeilMeldungen.Single(m => m.IstAbschliessend).GoodQuantity.Should().Be(0);
        (await ZeileAsync(u, Blech1)).Meldbar.Should().BeFalse();
    }

    [Fact]
    public async Task Fertig_OhneVorherigeMeldung_NullMengeAbgewiesen()
    {
        var u = await AufbauAsync();

        (await Melden(u, Fertig(Blech1, 0, 0))).Abgewiesen.Should().ContainSingle().Which.Should().Contain("Gut- oder Ausschussmenge");
    }

    [Fact]
    public async Task Ruecknahme_Abschluss_OeffnetZeile_RuecknahmeTeil_SenktGemeldet()
    {
        var u = await AufbauAsync(schichtende: DateTime.Now.AddHours(1));
        await Melden(u, Teil(Blech1, 1));
        await Melden(u, Fertig(Blech1));
        var abschluss = u.Ctx.BdeTeilMeldungen.Single(m => m.IstAbschliessend);

        (await u.Svc.ZuruecknehmenAsync(abschluss.Id, u.Ids.OperatorId, null)).Ok.Should().BeTrue();
        var z = await ZeileAsync(u, Blech1);
        (z.Meldbar, z.Gemeldet).Should().Be((true, 1m));

        var teil = u.Ctx.BdeTeilMeldungen.Single(m => !m.IstAbschliessend);
        (await u.Svc.ZuruecknehmenAsync(teil.Id, u.Ids.OperatorId, null)).Ok.Should().BeTrue();
        (await ZeileAsync(u, Blech1)).Gemeldet.Should().Be(0);
    }

    [Fact]
    public async Task Baugruppe_Teilfertig_TeilBuchung_Fertig_FinalBuchung()
    {
        var u = await AufbauAsync();
        var sub = new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", CreatedBy = "t", CreatedByWindows = "t" };
        u.Ctx.ProductionOrders.Add(sub);
        await u.Ctx.SaveChangesAsync();
        u.Ctx.WorkOperations.Add(new WorkOperation { ProductionOrderId = sub.Id, OperationNumber = "KA", Name = "Kanten", ProductionWorkplaceId = u.Ids.WorkplaceId, CreatedBy = "t", CreatedByWindows = "t" });
        await u.Ctx.SaveChangesAsync();

        var teil = await Melden(u, Teil(Korpus, 1));
        teil.Hinweise.Should().ContainSingle().Which.Should().Contain("Sollmenge erreicht", "Korpus Soll 1");
        var q = u.Ctx.BdeBookingQuantities.Single();
        (q.IsFinal, q.GoodQuantity).Should().Be((false, 1m));
        u.Ctx.BdeTeilMeldungen.Single().BdeBookingId.Should().Be(q.BdeBookingId);

        var fertig = await Melden(u, Fertig(Korpus, 1));
        fertig.AllesGut.Should().BeTrue(fertig.Text());
        u.Ctx.BdeBookingQuantities.Should().HaveCount(2).And.ContainSingle(x => x.IsFinal);
        u.Ctx.BdeTeilMeldungen.Single(m => m.IstAbschliessend).BdeBookingId.Should().NotBeNull();
    }

    [Fact]
    public async Task Block_OhneMenge_MeldetAbschliessendMitRest()
    {
        var u = await AufbauAsync();
        await Melden(u, Teil(Blech1, 3));

        (await Melden(u, new BdeTeilelisteService.MeldeAuftrag(Blech1, null, null), new BdeTeilelisteService.MeldeAuftrag(Korpus, null, null)))
            .Gemeldet.Should().Be(2);

        u.Ctx.BdeTeilMeldungen.Where(m => m.IstAbschliessend).Select(m => m.GoodQuantity).Should().BeEquivalentTo(new[] { 1m, 1m });
    }

    [Fact]
    public void Bauen_GemeldeteAus_BlendetNurAbgeschlosseneAus_WaisenJeSchluessel()
    {
        var nodes = new[]
        {
            new FaHierarchyNode { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 1, Artnr = "B1", Matchcode = "Teil", Arbeitsschritte = "KA", Sollmenge = 10 },
            new FaHierarchyNode { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 2, Artnr = "B2", Matchcode = "Fertig", Arbeitsschritte = "KA", Sollmenge = 10 },
        };
        BdeTeilMeldung M(int pos, string artnr, bool abschluss, decimal gut = 1) =>
            new() { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = pos, Artnr = artnr, Kuerzel = "KA", GoodQuantity = gut, IstAbschliessend = abschluss };
        var meldungen = new[] { M(1, "B1", false, 3), M(1, "B1", false, 4), M(2, "B2", true), M(9, "ALT", false), M(9, "ALT", true) };

        var d = BdeTeilelisteService.Bauen(nodes, Array.Empty<FaHierarchyOrderInfo>(), "KA", meldungen, new Dictionary<string, bool>(), null, false, null);

        d.Zeilen.Should().ContainSingle().Which.Node.Matchcode.Should().Be("Teil");
        d.Zeilen[0].Gemeldet.Should().Be(7);
        d.Waisen.Should().Be(1, "zwei Meldungen, ein Schluessel");
    }

    [Fact]
    public void TeilmeldungenAnwenden_NurTeilgemeldet_Laeuft_AbschlussErledigt()
    {
        var heute = new DateTime(2026, 11, 25);
        var n = new FaHierarchyNode { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 1, Artnr = "B1", Arbeitsschritte = "KA LA" };
        var t = new FaHierarchyTreeNode { Node = n };
        t.Arbeitsgaenge.Add(new FaArbeitsschrittSymbol("KA", null) { Status = FaAgStatus.KeinBdeArbeitsgang });
        t.Arbeitsgaenge.Add(new FaArbeitsschrittSymbol("LA", null) { Status = FaAgStatus.KeinBdeArbeitsgang });
        var s = new FaHierarchyStructure
        {
            HauptFA = 100, Roots = new List<FaHierarchyTreeNode> { t },
            OrderInfos = new List<FaHierarchyOrderInfo> { new() { HauptFA = 100, FE_Termin = heute.AddDays(-1) } },
        };
        var k = TeilSchluessel.Von(n);

        BdeTeilelisteService.TeilmeldungenAnwenden(new[] { s }, new HashSet<(TeilSchluessel, string)> { (k, "LA") }, new HashSet<string> { "KA" },
            new Dictionary<string, int>(), (d, _) => d, heute, teilgemeldet: new HashSet<(TeilSchluessel, string)> { (k, "KA") });

        t.Arbeitsgaenge[0].Status.Should().Be(FaAgStatus.LaeuftUeberfaellig, "teilgemeldet = laeuft, Soll ueberschritten");
        t.Arbeitsgaenge[1].Status.Should().Be(FaAgStatus.Erledigt);
    }

    [Fact]
    public void AdminSpalteArt()
    {
        IdealAkeWms.Controllers.BdeBookingsController.TeilmeldungSpalten["art"](new BdeTeilMeldung { IstAbschliessend = true }).Should().Be("Fertig");
        IdealAkeWms.Controllers.BdeBookingsController.TeilmeldungSpalten["art"](new BdeTeilMeldung()).Should().Be("Teil");
    }
}
