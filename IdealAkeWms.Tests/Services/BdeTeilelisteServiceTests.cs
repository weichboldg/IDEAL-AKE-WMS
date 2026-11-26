using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Zeilenbildung der BDE-Teileliste (Etappe 2).</summary>
public class BdeTeilelisteServiceTests
{
    private static FaHierarchyNode Node(int haupt, int? vater, int sub, int? pos, string? artnr, string matchcode, string? schritte, decimal soll = 1) =>
        new() { HauptFA = haupt, VaterFA = vater, SubFA = sub, Position = pos, Artnr = artnr, Matchcode = matchcode, Arbeitsschritte = schritte, Sollmenge = soll };

    private static readonly Dictionary<string, bool> KeineAuftraege = new();

    private static TeilelisteDaten Bauen(IReadOnlyCollection<FaHierarchyNode> nodes, IReadOnlyCollection<BdeTeilMeldung>? meldungen = null,
        IReadOnlyDictionary<string, bool>? auftraege = null, IEnumerable<FaHierarchyOrderInfo>? infos = null,
        (DateTime, DateTime)? kw = null, bool gemeldete = false, IReadOnlyDictionary<string, string>? colf = null) =>
        BdeTeilelisteService.Bauen(nodes, infos ?? Array.Empty<FaHierarchyOrderInfo>(), "KA", meldungen ?? Array.Empty<BdeTeilMeldung>(),
            auftraege ?? KeineAuftraege, kw, gemeldete, colf);

    [Fact]
    public void Zeile_NurMitKuerzelImToken_BlattUndBaugruppe()
    {
        var nodes = new[]
        {
            Node(100, 100, 200, 1, "BG", "Baugruppe", "ST KA"),   // Baugruppe mit KA
            Node(100, 200, 0, 1, "B1", "Blech 1", "ka la"),      // Blatt, Gross/Klein egal
            Node(100, 200, 0, 2, "B2", "Blech 2", "KAX LA"),      // Teilstring zaehlt nicht
            Node(100, 200, 0, 3, "B3", "Blech 3", null),
        };

        var d = Bauen(nodes);

        d.Zeilen.Select(z => z.Node.Matchcode).Should().BeEquivalentTo("Baugruppe", "Blech 1");
        d.Zeilen.Single(z => z.Node.Matchcode == "Baugruppe").IstBaugruppe.Should().BeTrue();
        d.Zeilen.Single(z => z.Node.Matchcode == "Baugruppe").Kinder.Should().HaveCount(3, "alle direkten Kinder werden aufklappbar angezeigt");
        d.Zeilen.Single(z => z.Node.Matchcode == "Blech 1").IstBaugruppe.Should().BeFalse();
    }

    [Fact]
    public void Meldung_TrifftZeileUeberFachlichenSchluessel_AuchNachNeuerNodeId()
    {
        var node = Node(100, 200, 0, 5, "B1", "Blech 1", "KA");
        node.Id = 999; // Sync hat neu angelegt
        var meldung = new BdeTeilMeldung { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 5, Artnr = "B1", Kuerzel = "KA", GoodQuantity = 1, IstAbschliessend = true };

        Bauen(new[] { node }, new[] { meldung }).Zeilen.Should().BeEmpty("gemeldete sind ausgeblendet");
        var z = Bauen(new[] { node }, new[] { meldung }, gemeldete: true).Zeilen.Single();
        z.Abschluss.Should().BeSameAs(meldung);
        z.Meldbar.Should().BeFalse();
    }

    [Fact]
    public void LeereArtnr_IstGleichNull()
    {
        var node = Node(100, 200, 0, 5, "", "Blech", "KA");
        var meldung = new BdeTeilMeldung { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 5, Artnr = null, Kuerzel = "KA", IstAbschliessend = true };

        Bauen(new[] { node }, new[] { meldung }, gemeldete: true).Zeilen.Single().Abschluss.Should().NotBeNull();
    }

    [Fact]
    public void ErledigterAuftrag_BlendetZeileAus_FehlenderAuftragIstMeldbar()
    {
        var nodes = new[]
        {
            Node(100, 200, 0, 1, "B1", "Blech erledigt", "KA"),   // Besitzer 200 erledigt
            Node(100, 300, 0, 1, "B2", "Blech offen", "KA"),      // Besitzer 300 offen
            Node(100, 400, 0, 1, "B3", "Blech ohne", "KA"),       // Besitzer 400 kein WMS-Auftrag
            Node(100, 100, 500, 2, "BG", "Baugruppe erledigt", "KA"), // Baugruppe: Besitzer eigene Sub-FA 500
        };
        var auftraege = new Dictionary<string, bool> { ["200"] = false, ["300"] = true, ["500"] = false };

        var d = Bauen(nodes, auftraege: auftraege);

        d.Zeilen.Select(z => z.Node.Matchcode).Should().BeEquivalentTo("Blech offen", "Blech ohne");
        d.Zeilen.Single(z => z.Node.Matchcode == "Blech ohne").KeinWmsAuftrag.Should().BeTrue();
        d.Zeilen.Single(z => z.Node.Matchcode == "Blech ohne").Meldbar.Should().BeTrue();
        d.Zeilen.Single(z => z.Node.Matchcode == "Blech offen").KeinWmsAuftrag.Should().BeFalse();
    }

    [Fact]
    public void DoppelterSchluessel_NichtMeldbar()
    {
        var nodes = new[] { Node(100, 200, 0, 1, "B1", "Blech", "KA"), Node(100, 200, 0, 1, "B1", "Blech", "KA") };

        Bauen(nodes).Zeilen.Should().OnlyContain(z => !z.Eindeutig && !z.Meldbar);
    }

    [Fact]
    public void Waisen_ZaehlenMeldungenOhneZeile_NurFuerHauptFaInStruktur()
    {
        var nodes = new[] { Node(100, 200, 0, 1, "B1", "Blech", "KA") };
        var meldungen = new[]
        {
            new BdeTeilMeldung { HauptFA = 100, VaterFA = 200, SubFA = 0, Position = 9, Artnr = "ALT", Kuerzel = "KA" }, // Waise
            new BdeTeilMeldung { HauptFA = 777, VaterFA = 1, SubFA = 0, Position = 1, Artnr = "X", Kuerzel = "KA" },     // HauptFA weg → kein Hinweis
        };

        Bauen(nodes, meldungen).Waisen.Should().Be(1);
    }

    [Fact]
    public void KwFilter_AufFeTerminDesHauptauftrags()
    {
        var nodes = new[] { Node(100, 200, 0, 1, "B1", "In KW", "KA"), Node(101, 201, 0, 1, "B2", "Ausserhalb", "KA") };
        var infos = new[]
        {
            new FaHierarchyOrderInfo { HauptFA = 100, FE_Termin = new DateTime(2026, 11, 18) },
            new FaHierarchyOrderInfo { HauptFA = 101, FE_Termin = new DateTime(2026, 12, 30) },
        };
        var kw = IsoWeekRange.Resolve("2026-W47", "2026-W48");

        Bauen(nodes, infos: infos, kw: kw).Zeilen.Select(z => z.Node.Matchcode).Should().Equal("In KW");
    }

    [Fact]
    public void Summiert_JeMatchcode_MitOffenenSchluesseln()
    {
        var nodes = new[]
        {
            Node(100, 200, 0, 1, "B1", "Blech", "KA", 2),
            Node(101, 201, 0, 1, "B1", "Blech", "KA", 3),
            Node(101, 201, 0, 2, "B2", "Anderes", "KA", 5),
        };
        var gemeldet = new BdeTeilMeldung { HauptFA = 101, VaterFA = 201, SubFA = 0, Position = 1, Artnr = "B1", Kuerzel = "KA", IstAbschliessend = true };
        var d = Bauen(nodes, new[] { gemeldet }, gemeldete: true);

        var a = BdeTeilelisteService.Summieren(d.Zeilen, Array.Empty<FaHierarchyOrderInfo>(), null).Single(x => x.Matchcode == "Blech");

        a.SummeSollmenge.Should().Be(5);
        a.Anzahl.Should().Be(2);
        a.HauptFas.Should().Be("100, 101");
        a.OffeneSchluessel.Should().ContainSingle().Which.Should().Be(new TeilSchluessel(100, 200, 0, 1, "B1").Text);
        a.SummeSollmengeOffen.Should().Be(2);
    }

    [Theory]
    [InlineData(100, 200, 0, 1, "B1")]
    [InlineData(100, null, 0, null, null)]
    [InlineData(100, 200, 300, 4, "A|B")]
    public void Schluessel_TextUndParse_RoundTrip(int h, int? v, int s, int? p, string? a)
    {
        var k = new TeilSchluessel(h, v, s, p, a);
        TeilSchluessel.Parse(k.Text).Should().Be(k);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x|1|0|1|A")]
    [InlineData("1|y|0|1|A")]
    [InlineData("1|2|0")]
    public void Schluessel_Parse_Ungueltig(string? text) => TeilSchluessel.Parse(text).Should().BeNull();

    [Fact]
    public async Task AuftragOffen_HauptUndSubAuftrag()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "100", SubOrderNumber = "", CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { OrderNumber = "100", SubOrderNumber = "200", IsDone = true, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { OrderNumber = "100", SubOrderNumber = "300", IsCancelled = true, CreatedBy = "t", CreatedByWindows = "t" },
            new ProductionOrder { OrderNumber = "400", SubOrderNumber = "400", CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var svc = new BdeTeilelisteService(Mock.Of<IFaHierarchyNodeRepository>(), Mock.Of<IFaHierarchyOrderInfoRepository>(), ctx,
            Mock.Of<IBdeBookingService>(), Mock.Of<ICurrentUserService>(), Mock.Of<IAppSettingRepository>(), Mock.Of<IBdeShiftCalendarService>(), Mock.Of<IArticleRepository>(), Mock.Of<IBusinessDayService>(), Mock.Of<IHolidayRepository>());

        var r = await svc.AuftragOffenAsync(new[] { "100", "200", "300", "400", "999" });

        r.Should().BeEquivalentTo(new Dictionary<string, bool> { ["100"] = true, ["200"] = false, ["300"] = false, ["400"] = true });
    }

    // v1.99.78 (Etappe 5): Terminal_LeitetNurBeiStandardsichtTeileUm entfaellt – BdeTerminal/Index leitet jetzt immer weiter
    // (BdeTerminalControllerTests.Index_LeitetImmerAufBdeSeiteWeiter).

    // ------------------------------------------------------------ Etappe 5: Filter speichern

    [Fact]
    public void Filter_WocheRelativ_RoundtripUeberJson()
    {
        var montag = new DateTime(2026, 11, 16); // KW 47
        var f = new BdeTeilelisteController.GespeicherterFilter("summiert",
            BdeTeilelisteController.WochenOffset("2026-W47", montag), BdeTeilelisteController.WochenOffset("2026-W48", montag), true, 50,
            new Dictionary<string, string> { ["matchcode"] = "Blech*" });
        (f.KwVonOffset, f.KwBisOffset).Should().Be((0, 1));

        var gelesen = BdeTeilelisteController.TryLesen(System.Text.Json.JsonSerializer.Serialize(f))!;
        var q = BdeTeilelisteController.AlsQuery(3, 7, gelesen, new DateTime(2026, 11, 25)); // eine Woche spaeter, KW 48

        q["kwVon"].Should().Be("2026-W48");
        q["kwBis"].Should().Be("2026-W49");
        q["sicht"].Should().Be("summiert");
        q["gemeldete"].Should().Be(true);
        q["pageSize"].Should().Be(50);
        q["colf_matchcode"].Should().Be("Blech*");
        BdeTeilelisteController.FilterSchluessel(3, 7).Should().Be("BdeTL:W3:O7");
    }

    [Fact]
    public void Filter_LeereWoche_BleibtLeer_JahreswechselStimmt_KaputtesJsonIstNull()
    {
        var f = new BdeTeilelisteController.GespeicherterFilter(null, null, null, false, null, null);
        var q = BdeTeilelisteController.AlsQuery(1, 2, f, DateTime.Today);
        (q["kwVon"], q["kwBis"]).Should().Be(("", ""));
        q.ContainsKey("gemeldete").Should().BeFalse();

        BdeTeilelisteController.WochenOffset("2027-W01", new DateTime(2026, 12, 23)).Should().Be(2); // 2026 hat 53 KW
        BdeTeilelisteController.WochenOffset("unsinn", DateTime.Today).Should().BeNull();
        BdeTeilelisteController.TryLesen("{kaputt").Should().BeNull();
    }

    [Fact]
    public void Beschichtet_Zelltext_UndSpaltenfilter()
    {
        (BdeTeilelisteController.BeschichtetText(true), BdeTeilelisteController.BeschichtetText(false), BdeTeilelisteController.BeschichtetText(null))
            .Should().Be(("ja", "nein", "–"));
        var spalten = BdeTeilelisteService.SpaltenEinzeln(new Dictionary<int, VormontageService.KopfWerte>(), n => n.Beschichtet ? "ja" : "nein");
        spalten["beschichtet"](new FaHierarchyNode { Beschichtet = true }).Should().Be("ja");
    }

    // v1.99.81 (Task 4b, TS-202): Abteilung filtert in Einzeln (Baugruppe + Kinder) und Summiert – Diagnose des Nachtrags Gerald 2026-11-26
    [Fact]
    public void Abteilung_Filter_BaugruppeUndSummiert()
    {
        var nodes = new[]
        {
            Node(100, 100, 200, 1, "BG", "Baugruppe A", "KA"),
            Node(100, 200, 0, 1, "B1", "Blech A", "KA"),
            Node(101, 101, 300, 1, "BG2", "Baugruppe B", "KA"),
            Node(101, 300, 0, 1, "B2", "Blech B", "KA"),
        };
        var infos = new[]
        {
            new FaHierarchyOrderInfo { HauptFA = 100, MontageAbteilung = "Montage Nord" },
            new FaHierarchyOrderInfo { HauptFA = 101, MontageAbteilung = "Montage Sued" },
        };
        var colf = new Dictionary<string, string> { ["montage-abteilung"] = "Nord" };

        var d = Bauen(nodes, infos: infos, colf: colf);
        d.Zeilen.Select(z => z.Node.Matchcode).Should().BeEquivalentTo("Baugruppe A", "Blech A");

        BdeTeilelisteService.Summieren(d.Zeilen, infos, colf).Select(a => a.Matchcode).Should().BeEquivalentTo("Baugruppe A", "Blech A");
    }
}
