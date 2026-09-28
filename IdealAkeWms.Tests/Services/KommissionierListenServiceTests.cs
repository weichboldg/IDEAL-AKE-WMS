using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer IDEAL Teil 3 (Kommissionierlisten) inkl. der UAT-Anpassungen vom 2026-08-13:
/// <list type="bullet">
///   <item><b>Aenderung 1:</b> ALLE Positionen mit gesetztem <c>Kommissionieren</c> werden gelistet,
///     unabhaengig von <c>SubFA</c> (<c>leafOnly: false</c>); kein Anomalie-Banner mehr; das
///     Ziel-Dropdown enthaelt auch Werte, die nur auf <c>SubFA != 0</c> vorkommen.</item>
///   <item><b>Aenderung 2:</b> neue Summiert-Aggregation je <c>(HauptFA, Artnr, Kommissionieren)</c>,
///     Summe nur <c>Sollmenge</c>, KW-Filter auf <c>KO_Termin</c> (ISO Mo–So); ohne KW alle HauptFA
///     (Antwort 4).</item>
/// </list>
/// </summary>
public class KommissionierListenServiceTests
{
    private static FaHierarchyListBuilder NewBuilder() =>
        new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance);

    private static FaHierarchyNode Node(int hauptFa, int subFa, string? komm,
        string? artnr = null, int? position = null, decimal sollmenge = 0m) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa,
            SubFA = subFa,
            Kommissionieren = komm,
            Artnr = artnr,
            Position = position,
            Sollmenge = sollmenge
        };

    private static FaHierarchyOrderInfo Info(int hauptFa, DateTime? koTermin = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, KO_Termin = koTermin };

    private static KommissionierListenService NewService(
        IEnumerable<FaHierarchyNode> nodes,
        IEnumerable<FaHierarchyOrderInfo>? infos = null)
    {
        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes.ToList());
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync((infos ?? Enumerable.Empty<FaHierarchyOrderInfo>()).ToList());
        return new KommissionierListenService(nodeRepo.Object, infoRepo.Object, NewBuilder());
    }

    private static List<FaHierarchyOrderInfo> EmptyInfos() => new();

    // ----- Aenderung 1: alle Ebenen, kein Banner, Dropdown ueber alle Ebenen -----

    [Fact]
    public async Task BuildAsync_ListsAllSubFaLevels_NoAnomalyBanner()
    {
        // N1a/N1b/N1e: SubFA != 0 mit gesetztem Kommissionieren wird JETZT gelistet; kein Banner.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 5, komm: "LAGER", artnr: "BAUGRUPPE", position: 0), // SubFA != 0 -> gelistet
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, komm: null,    artnr: "LEAF-C", position: 2),     // kein Flag -> raus
        };

        var result = await NewService(nodes).BuildAsync(null, null, 1, 100, paginate: true);

        var artnrs = result.Groups.Single().Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "BAUGRUPPE", "LEAF-A" });
        artnrs.Should().NotContain("LEAF-C");

        result.Anomalies.HasAnomalies.Should().BeFalse();
        result.Anomalies.PositionCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTargetValues_IncludesValuesOnlyOnSubFaNonZero()
    {
        // N1d: ein Ziel, das nur auf einer SubFA != 0-Zeile vorkommt, MUSS im Dropdown erscheinen.
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, komm: "LAGER", position: 1),
            Node(1, subFa: 7, komm: "NUR-BAUGRUPPE", position: 2), // Ziel nur auf SubFA != 0
        };

        var targets = await NewService(nodes).GetTargetValuesAsync();

        targets.Should().Contain("NUR-BAUGRUPPE");
        targets.Should().Equal("LAGER", "NUR-BAUGRUPPE"); // sortiert, distinct
    }

    // ----- Aenderung 2: Summiert-Aggregation + KW-Filter auf KO_Termin -----

    [Fact]
    public void Summiert_SumsSollmenge_PerHauptFaArtnrZiel()
    {
        // N2a: zwei Zeilen gleicher HauptFA/Artnr/Ziel -> eine Zeile mit summierter Sollmenge.
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 2m),
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 5m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) }; // KW 10

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, null, 1, 100, paginate: true);

        var row = result.Rows.Single();
        row.HauptFA.Should().Be(100);
        row.Artnr.Should().Be("A");
        row.Kommissionieren.Should().Be("LAGER");
        row.SummeSollmenge.Should().Be(7m);
        result.TotalRowCount.Should().Be(1);
    }

    [Fact]
    public void Summiert_DifferentZiel_ProducesSeparateRows()
    {
        // N2b: gleiche Artnr, unterschiedliches Ziel -> zwei getrennte Aggregatzeilen.
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 2m),
            Node(100, subFa: 0, komm: "WERK",  artnr: "A", sollmenge: 5m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, null, 1, 100, paginate: true);

        result.Rows.Should().HaveCount(2);
        result.Rows.Select(r => r.Kommissionieren).Should().BeEquivalentTo(new[] { "LAGER", "WERK" });
    }

    [Fact]
    public void Summiert_KwFilter_ExcludesHauptFaOutsideWeek_IncludesInside()
    {
        // N2c: HauptFA mit KO_Termin ausserhalb der KW fehlt komplett; innerhalb ist vollstaendig da.
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "IN",  sollmenge: 1m),
            Node(200, subFa: 0, komm: "LAGER", artnr: "OUT", sollmenge: 9m),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(100, new DateTime(2026, 3, 4)),   // KW 10 -> drin
            Info(200, new DateTime(2026, 3, 11)),  // KW 11 -> draussen
        };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, null, 1, 100, paginate: true);

        result.Rows.Select(r => r.HauptFA).Should().Equal(100);
        result.Rows.Single().Artnr.Should().Be("IN");
    }

    [Fact]
    public void Summiert_NoKw_IncludesAllHauptFa()
    {
        // Antwort 4 (Spec 2026-09-25-kommissionierliste-summierung-pdf): ohne KW ALLE HauptFA — ersetzt N2d.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 3m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, kwRange: null, columnFilters: null, page: 1, pageSize: 100, paginate: true);

        result.Rows.Should().ContainSingle();
        result.Rows[0].SummeSollmenge.Should().Be(3m);
        result.TotalRowCount.Should().Be(1);
        result.TotalGroupCount.Should().Be(1);
    }

    [Fact]
    public void Summiert_IsIndependentOfSubFa()
    {
        // Aenderung 1 in der Summiert-Aggregation: SubFA != 0 zaehlt mit (kein Blattfilter).
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 2m),
            Node(100, subFa: 7, komm: "LAGER", artnr: "A", sollmenge: 100m), // SubFA != 0 -> zaehlt mit
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(102m);
    }

    [Fact]
    public void Summiert_ColumnFilter_AppliesToAggregateProjection()
    {
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "ALPHA", sollmenge: 1m),
            Node(100, subFa: 0, komm: "LAGER", artnr: "BETA",  sollmenge: 1m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };
        var filters = new Dictionary<string, string> { ["artnr"] = "alpha" };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, filters, 1, 100, paginate: true);

        result.Rows.Single().Artnr.Should().Be("ALPHA");
        result.TotalRowCount.Should().Be(1);
    }

    [Fact]
    public void Summiert_CarriesMatchcodeAndHauptlagerplatz_WithoutChangingKey()
    {
        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var n1 = Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 2m);
        n1.Matchcode = "M1";
        n1.Hauptlagerplatz = "L1";
        var n2 = Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 5m);
        n2.Matchcode = "M1";
        n2.Hauptlagerplatz = "L1";
        var nodes = new List<FaHierarchyNode> { n1, n2 };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, range, null, 1, 100, paginate: true);

        var row = result.Rows.Single();
        row.SummeSollmenge.Should().Be(7m);
        row.Matchcode.Should().Be("M1");
        row.Hauptlagerplatz.Should().Be("L1");
    }

    [Fact]
    public void Summiert_GroupPaging_NeverSplitsHauptFa()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 1m),
            Node(100, subFa: 0, komm: "LAGER", artnr: "B", sollmenge: 1m),
            Node(100, subFa: 0, komm: "LAGER", artnr: "C", sollmenge: 1m),
            Node(200, subFa: 0, komm: "LAGER", artnr: "D", sollmenge: 1m),
        };

        var page1 = KommissionierListenService.AggregateSummiert(nodes, EmptyInfos(), null, kwRange: null, columnFilters: null, page: 1, pageSize: 1, paginate: true);

        page1.Rows.Should().HaveCount(3);
        page1.Rows.Select(r => r.HauptFA).Should().OnlyContain(h => h == 100);
        page1.TotalGroupCount.Should().Be(2);
        page1.TotalRowCount.Should().Be(4);

        var page2 = KommissionierListenService.AggregateSummiert(nodes, EmptyInfos(), null, kwRange: null, columnFilters: null, page: 2, pageSize: 1, paginate: true);

        page2.Rows.Should().ContainSingle();
        page2.Rows[0].HauptFA.Should().Be(200);
    }

    [Fact]
    public void Summiert_ColumnFilterOnMatchcode()
    {
        var n1 = Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 1m);
        n1.Matchcode = "M1";
        var n2 = Node(100, subFa: 0, komm: "LAGER", artnr: "B", sollmenge: 1m);
        n2.Matchcode = "M2";
        var nodes = new List<FaHierarchyNode> { n1, n2 };
        var filters = new Dictionary<string, string> { ["matchcode"] = "M1" };

        var result = KommissionierListenService.AggregateSummiert(nodes, EmptyInfos(), null, kwRange: null, columnFilters: filters, page: 1, pageSize: 100, paginate: true);

        result.Rows.Should().ContainSingle();
        result.Rows[0].Matchcode.Should().Be("M1");
    }

    [Fact]
    public void BuildToggleQuery_KeepsSharedKeys_DropsSollmengeAndListOnly()
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString(
            "?target=LAGER&kwVon=2026-W10&colf_artnr=A1&colf_sollmenge=5&colf_arbeitsbereich=X&page=3");

        var query = KommissionierListenService.BuildToggleQuery(context.Request);

        query.Should().Contain("target=LAGER");
        query.Should().Contain("kwVon=2026-W10");
        query.Should().Contain("colf_artnr=A1");
        query.Should().NotContain("colf_sollmenge");
        query.Should().NotContain("colf_arbeitsbereich");
        query.Should().NotContain("page=3");

        KommissionierListenService.BuildToggleQuery(null).Should().BeEmpty();

        var emptyContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        KommissionierListenService.BuildToggleQuery(emptyContext.Request).Should().BeEmpty();

        var specialContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        specialContext.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString(
            "?target=" + Uri.EscapeDataString("A B&C"));
        var specialQuery = KommissionierListenService.BuildToggleQuery(specialContext.Request);
        specialQuery.Should().NotContain("A B&C"); // muss kodiert sein, nicht roh
        var parsedBack = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(specialQuery);
        parsedBack["target"].ToString().Should().Be("A B&C");
    }
}
