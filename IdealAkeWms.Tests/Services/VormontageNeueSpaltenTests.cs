using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// v1.72.0 (Spec 2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder, Teil 2): neue Spalten Arbeitsbereich, HauptFA (Sicht 2),
/// Kunde, FE-Termin, Bemerkung PN, Fertigungsinfo PN in beiden Vormontage-Sichten. DB-frei; die Optik ist Manual-UAT.
/// </summary>
public class VormontageNeueSpaltenTests
{
    private static FaHierarchyNode Node(int hauptFa, int position, string? arbeitsbereich = null, string? matchcode = "MC-1",
        decimal soll = 0m, string? bemerkung = null, string? fertigungsInfo = null) =>
        new()
        {
            HauptFA = hauptFa, SubFA = 0, VMBedarf = "VM-A", Position = position, Artnr = $"A{hauptFa}-{position}",
            Matchcode = matchcode, Sollmenge = soll, Arbeitsbereich = arbeitsbereich, BemerkungPN = bemerkung, FertigungsInfoPN = fertigungsInfo,
        };

    private static FaHierarchyOrderInfo Info(int hauptFa, string? kunde = null, DateTime? fe = null) =>
        new() { HauptFA = hauptFa, Kunde = kunde, FE_Termin = fe };

    private static Task<FaHierarchyListResult> Sicht1(IEnumerable<FaHierarchyNode> nodes, IEnumerable<FaHierarchyOrderInfo> infos,
        Dictionary<string, string>? filter)
    {
        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes.ToList());
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(infos.ToList());
        var svc = new VormontageService(nodeRepo.Object, infoRepo.Object,
            new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance), NullLogger<VormontageService>.Instance);
        return svc.BuildEinzelteileAsync("VM-A", null, filter, 1, 100, paginate: true);
    }

    private static Dictionary<string, string> F(string key, string value) => new() { [key] = value };

    // ----- Formatierer -----

    [Fact]
    public void Formatierer_VerwirftLeer_EntferntDoppelte_Sortiert()
    {
        VormontageService.FormatTexte(new[] { "B", null, " ", "", "a", "B", " A " }).Should().Be("a, A, B");
        VormontageService.FormatTexte(new string?[] { null, "  " }).Should().BeNull("nichts => leere Zelle");
        VormontageService.FormatDaten(new DateTime?[] { new(2026, 11, 19, 8, 0, 0), null, new(2026, 11, 12), new(2026, 11, 19) })
            .Should().Be("12.11.2026, 19.11.2026");
        VormontageService.FormatDaten(new DateTime?[] { null }).Should().BeNull();
    }

    // ----- Sicht 1 -----

    [Fact]
    public async Task Sicht1_Arbeitsbereich_JePosition_FilterUndNegation()
    {
        var nodes = new[] { Node(100, 1, "H1-03"), Node(100, 2, "H2-02"), Node(100, 3, null) };

        var ohne = (await Sicht1(nodes, Array.Empty<FaHierarchyOrderInfo>(), null)).Groups.Single();
        ohne.Positions.Select(p => p.Arbeitsbereich).Should().Equal("H1-03", "H2-02", null);

        var h1 = await Sicht1(nodes, Array.Empty<FaHierarchyOrderInfo>(), F("arbeitsbereich", "H1"));
        h1.Groups.Single().Positions.Select(p => p.Position).Should().Equal(1);
        h1.TotalGroupCount.Should().Be(1);

        var nichtH1 = await Sicht1(nodes, Array.Empty<FaHierarchyOrderInfo>(), F("arbeitsbereich", "!H1"));
        nichtH1.Groups.Single().Positions.Select(p => p.Position).Should().Equal(2, 3);
    }

    [Fact]
    public async Task Sicht1_Kopfwerte_Kombigeraet_ListeSortiert_DatumsfilterAufFruehestem_KeinFanOut()
    {
        var nodes = new[] { Node(100, 1), Node(100, 2), Node(200, 1) };
        var infos = new[] { Info(100, "B", new DateTime(2026, 11, 19)), Info(100, "A", new DateTime(2026, 11, 12)) };

        var kopf = VormontageService.BuildKopfLookup(infos);
        kopf[100].Should().Be(new VormontageService.KopfWerte("A, B", "12.11.2026, 19.11.2026"));
        kopf.ContainsKey(200).Should().BeFalse("HauptFA ohne Kopfzeile => leere Zelle");
        var map = VormontageService.BuildEinzelnColumnMap(kopf);
        map["kunde"](nodes[2]).Should().BeNull();
        map["kunde"](nodes[0]).Should().Be(VormontageService.BuildKopfWerte(infos).Kunde, "Getter und Zelle nutzen dieselbe Methode");

        var alle = await Sicht1(nodes, infos, null);
        alle.Groups.Single(g => g.HauptFA == 100).Positions.Should().HaveCount(2, "kein Fan-out durch zwei Kopfzeilen");

        var frueh = await Sicht1(nodes, infos, F("fe-termin", "<=15.11.2026"));
        frueh.Groups.Select(g => g.HauptFA).Should().Equal(100);
        (await Sicht1(nodes, infos, F("fe-termin", ">=15.11.2026"))).Groups.Should().BeEmpty("verglichen wird der frueheste FE-Termin");
        (await Sicht1(nodes, infos, F("kunde", "A"))).Groups.Select(g => g.HauptFA).Should().Equal(100);
    }

    [Fact]
    public async Task Sicht1_PnFelder_Filterbar()
    {
        var nodes = new[] { Node(100, 1, bemerkung: "eilt"), Node(100, 2, fertigungsInfo: "lasern") };
        (await Sicht1(nodes, Array.Empty<FaHierarchyOrderInfo>(), F("bemerkungpn", "eilt"))).Groups.Single().Positions.Select(p => p.Position).Should().Equal(1);
        (await Sicht1(nodes, Array.Empty<FaHierarchyOrderInfo>(), F("fertigungsinfopn", "lasern"))).Groups.Single().Positions.Select(p => p.Position).Should().Equal(2);
    }

    // ----- Sicht 2 -----

    [Fact]
    public void Sicht2_Anzeige_WerteDerBeitragendenPositionen_JeMatchcodeEineZeile()
    {
        var nodes = new[]
        {
            Node(200, 1, "H2-02", soll: 3m, bemerkung: "x"),
            Node(100, 1, "H1-03", soll: 4m),
            Node(100, 2, "H1-03", soll: 1m),
        };
        var infos = new[] { Info(100, "Kunde B", new DateTime(2026, 11, 19)), Info(200, "Kunde A", new DateTime(2026, 11, 12)) };

        var row = VormontageService.AggregateSummiert(nodes, infos, "VM-A", null, null, 1, 100, true).Rows.Single();

        row.SummeSollmenge.Should().Be(8m);
        (row.Arbeitsbereiche, row.HauptFas, row.Kunden, row.FeTermine, row.BemerkungenPN, row.FertigungsInfosPN)
            .Should().Be(("H1-03, H2-02", "100, 200", "Kunde A, Kunde B", "12.11.2026, 19.11.2026", "x", (string?)null));
    }

    [Theory]
    [InlineData("arbeitsbereich", "H1", 5)]
    [InlineData("hauptfa", "200", 3)]
    [InlineData("kunde", "Kunde B", 5)]
    [InlineData("fe-termin", "12.11.2026", 3)]
    public void Sicht2_FilterNeueSpalte_WirktJePositionVorDemZusammenzaehlen(string key, string wert, decimal summe)
    {
        var nodes = new[] { Node(200, 1, "H2-02", soll: 3m), Node(100, 1, "H1-03", soll: 4m), Node(100, 2, "H1-03", soll: 1m) };
        var infos = new[] { Info(100, "Kunde B", new DateTime(2026, 11, 19)), Info(200, "Kunde A", new DateTime(2026, 11, 12)) };

        var result = VormontageService.AggregateSummiert(nodes, infos, "VM-A", null, F(key, wert), 1, 100, true);

        result.Rows.Single().SummeSollmenge.Should().Be(summe);
        result.TotalRowCount.Should().Be(1);
    }

    [Fact]
    public void Sicht2_MatchcodeOhnePassendePosition_Verschwindet_SummenfilterWirktWeiterAufDerSumme()
    {
        var nodes = new[] { Node(100, 1, "H1-03", matchcode: "MC-1", soll: 4m), Node(200, 1, "H2-02", matchcode: "MC-2", soll: 1m),
                            Node(300, 1, "H1-03", matchcode: "MC-2", soll: 1m) };

        var h2 = VormontageService.AggregateSummiert(nodes, Array.Empty<FaHierarchyOrderInfo>(), "VM-A", null, F("arbeitsbereich", "H2"), 1, 100, true);
        h2.Rows.Select(r => (r.Matchcode, r.SummeSollmenge)).Should().Equal(("MC-2", 1m));

        // Summenfilter auf der Aggregat-Projektion: MC-2 hat ohne Vorfilter 2,000 (1+1).
        var summe = VormontageService.AggregateSummiert(nodes, Array.Empty<FaHierarchyOrderInfo>(), "VM-A", null,
            F("sollmenge", "2"), 1, 100, true);
        summe.Rows.Select(r => r.Matchcode).Should().Equal("MC-2");
    }

    // ----- Drift-Waechter (A/B/C ohne Druck-Datei, Muster KommissionierSpaltenKonsistenzTests) -----

    public static IEnumerable<object[]> Views()
    {
        yield return new object[] { "Index.cshtml", ColumnDefinitions.FaHierarchyVormontageEinzeln };
        yield return new object[] { "Summiert.cshtml", ColumnDefinitions.FaHierarchyVormontageSummiert };
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void DreiQuellen_GleicheSchluessel_GleicheReihenfolge_ArbeitsbereichStattAbteilung(string file, ViewConfig config)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        var html = File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "FaHierarchyVormontage", file));

        var defKeys = config.Columns.Select(c => c.Key).ToList();
        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().BeEquivalentTo(defKeys);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(defKeys);
        defKeys.Should().Contain("arbeitsbereich").And.NotContain("abteilung").And.NotContain("montage-abteilung");
        config.Columns.Should().OnlyContain(c => !c.DefaultHidden, "neue Spalten sind per Default sichtbar");
        html.Should().MatchRegex(@"data-col-key=""fe-termin"" data-date-filter", "Typ an <th> UND im Register (fallstricke §26)");
    }

    [Fact]
    public void Sicht1Filtermap_GleicheSchluesselWieColumnDefinitions()
    {
        VormontageService.BuildEinzelnColumnMap(new Dictionary<int, VormontageService.KopfWerte>()).Keys
            .Should().BeEquivalentTo(ColumnDefinitions.FaHierarchyVormontageEinzeln.Columns.Select(c => c.Key));
        ColumnFilterTypes.For("FaHierarchyVormontage/Index")["fe-termin"].Should().Be(ColumnFilterType.Date);
        ColumnFilterTypes.For("FaHierarchyVormontage/Summiert")["fe-termin"].Should().Be(ColumnFilterType.Date);
    }
}
