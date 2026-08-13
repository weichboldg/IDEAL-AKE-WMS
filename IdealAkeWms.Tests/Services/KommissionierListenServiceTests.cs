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
///     Summe nur <c>Sollmenge</c>, KW-Filter auf <c>KO_Termin</c> (ISO Mo–So); ohne KW kein Ergebnis.</item>
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
    public void Summiert_NoKw_ReturnsEmpty_NoImplicitTotal()
    {
        // N2d: ohne KW-Bereich KEINE implizite Gesamtsumme.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "A", sollmenge: 3m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, new DateTime(2026, 3, 4)) };

        var result = KommissionierListenService.AggregateSummiert(nodes, infos, null, kwRange: null, columnFilters: null, page: 1, pageSize: 100, paginate: true);

        result.Rows.Should().BeEmpty();
        result.TotalRowCount.Should().Be(0);
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
}
