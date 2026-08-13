using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Tests fuer IDEAL Teil 5 (Vormontage-Listen) inkl. der UAT-Anpassungen vom 2026-08-13:
/// <list type="bullet">
///   <item><b>Aenderung 1:</b> ALLE Positionen mit gesetztem <c>VMBedarf</c> werden gelistet/gezaehlt,
///     unabhaengig von <c>SubFA</c> (<c>leafOnly: false</c>); kein Anomalie-Banner mehr.</item>
///   <item><b>Aenderung 2:</b> KW-Zeitraum-Filter auf <c>FE_Termin</c> (ISO Mo–So) wirkt in Sicht 2
///     VOR der Matchcode-Aggregation.</item>
///   <item><b>Sicht 2 (Summiert)</b> aggregiert je VMBedarf-Reiter nach <b>Matchcode</b> mit zwei
///     getrennten Summen; gleicher Artnr mit verschiedenem Matchcode ergibt mehrere Zeilen.</item>
/// </list>
/// </summary>
public class VormontageServiceTests
{
    private static FaHierarchyListBuilder NewBuilder() =>
        new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance);

    private static FaHierarchyNode Node(int hauptFa, int subFa, string? vmBedarf,
        string? artnr = null, string? matchcode = null, int? position = null,
        decimal sollmenge = 0m, decimal fertigungmenge = 0m) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa,
            SubFA = subFa,
            VMBedarf = vmBedarf,
            Artnr = artnr,
            Matchcode = matchcode,
            Position = position,
            Sollmenge = sollmenge,
            Fertigungmenge = fertigungmenge
        };

    private static FaHierarchyOrderInfo Info(int hauptFa, string? montage = null,
        string? kunde = null, DateTime? feTermin = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, MontageAbteilung = montage, Kunde = kunde, FE_Termin = feTermin };

    private static VormontageService NewService(
        IEnumerable<FaHierarchyNode> nodes,
        IEnumerable<FaHierarchyOrderInfo>? infos = null)
    {
        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes.ToList());
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync((infos ?? Enumerable.Empty<FaHierarchyOrderInfo>()).ToList());
        return new VormontageService(nodeRepo.Object, infoRepo.Object, NewBuilder(),
            NullLogger<VormontageService>.Instance);
    }

    // ----- Sicht 1 (Einzelne Teile) — via gemeinsamer Baustein -----

    [Fact]
    public void Sicht1_FlagPredicate_FiltersVmBedarfSet_NvarcharNotBit()
    {
        // VMBedarf ist nvarchar: gesetzt = IS NOT NULL AND <> '' (kein -1/Bit-Vergleich).
        var predAll = VormontageService.BuildFlagPredicate(null);
        predAll(Node(1, 0, "VM-A")).Should().BeTrue();
        predAll(Node(1, 0, "")).Should().BeFalse();
        predAll(Node(1, 0, "   ")).Should().BeFalse();
        predAll(Node(1, 0, null)).Should().BeFalse();

        // Reiter-Einschraenkung auf einen konkreten Bereich.
        var predA = VormontageService.BuildFlagPredicate("VM-A");
        predA(Node(1, 0, "VM-A")).Should().BeTrue();
        predA(Node(1, 0, "VM-B")).Should().BeFalse();
    }

    [Fact]
    public async Task Sicht1_ListsAllSubFaLevels_NoAnomalyBanner()
    {
        // Nachtrag 2026-08-13, Aenderung 1 (N9): SubFA != 0 mit gesetztem VMBedarf wird JETZT
        // gelistet (frueher Anomalie/ausgeschlossen); kein Banner.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 5, vmBedarf: "VM-A", artnr: "BAUGRUPPE", position: 0), // SubFA != 0 -> jetzt gelistet
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-B", position: 2),
            Node(100, subFa: 0, vmBedarf: null,   artnr: "LEAF-C", position: 3),     // kein Flag -> raus
        };

        var result = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, null, 1, 100, paginate: true);

        var artnrs = result.Groups.Single().Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "BAUGRUPPE", "LEAF-A", "LEAF-B" });
        artnrs.Should().NotContain("LEAF-C");

        result.Anomalies.HasAnomalies.Should().BeFalse();
        result.Anomalies.PositionCount.Should().Be(0);
    }

    [Fact]
    public async Task Sicht1_HeaderJoin_MultipleOrderInfos_DoesNotMultiplyPositions()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-B", position: 2),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(100, "Montage-1", "Kunde X"),
            Info(100, "Montage-2", "Kunde X"),
        };

        var group = (await NewService(nodes, infos).BuildEinzelteileAsync("VM-A", null, null, 1, 100, true))
            .Groups.Single();

        group.Positions.Should().HaveCount(2);   // NICHT 4 (kein Fan-out)
        group.OrderInfos.Should().HaveCount(2);
        group.IsAmbiguous.Should().BeTrue();
    }

    [Fact]
    public async Task Sicht1_ReiterRestriction_ScopesToSelectedBereich()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "A1", position: 1),
            Node(100, subFa: 0, vmBedarf: "VM-B", artnr: "B1", position: 2),
        };

        var resultA = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, null, 1, 100, true);
        resultA.Groups.Single().Positions.Select(p => p.Artnr).Should().BeEquivalentTo(new[] { "A1" });
    }

    [Fact]
    public async Task Sicht1_GroupPaging_TotalCountCountsGroups()
    {
        var nodes = new List<FaHierarchyNode>();
        for (int hf = 1; hf <= 5; hf++)
            nodes.Add(Node(hf, subFa: 0, vmBedarf: "VM-A", artnr: $"A{hf}", position: 1));

        var page1 = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, null, 1, 2, true);
        page1.TotalGroupCount.Should().Be(5);
        page1.Groups.Select(g => g.HauptFA).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetBereiche_DistinctNonEmpty_Sorted_IndependentOfSubFa()
    {
        // Nachtrag 2026-08-13, Aenderung 1: VMBedarf-Werte auf SubFA != 0 erzeugen JETZT ebenfalls
        // einen Reiter (frueher nur SubFA = 0).
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-B", position: 1),
            Node(1, subFa: 0, vmBedarf: "VM-A", position: 2),
            Node(1, subFa: 0, vmBedarf: "VM-A", position: 3),   // Duplikat
            Node(1, subFa: 0, vmBedarf: "",     position: 4),   // leer -> kein Reiter
            Node(1, subFa: 0, vmBedarf: null,   position: 5),   // null -> kein Reiter
            Node(1, subFa: 7, vmBedarf: "VM-Z", position: 6),   // SubFA != 0 -> JETZT ein Reiter
        };

        var bereiche = await NewService(nodes).GetBereicheAsync();

        bereiche.Should().Equal("VM-A", "VM-B", "VM-Z");   // sortiert, distinct, ohne leer/null, alle Ebenen
    }

    // ----- Sicht 2 (Summiert) — eigene Aggregat-Logik -----

    [Fact]
    public void Sicht2_AggregatesByMatchcode_SumsBothQuantities()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "X", matchcode: "MC-1", sollmenge: 2m, fertigungmenge: 3m),
            Node(2, subFa: 0, vmBedarf: "VM-A", artnr: "Y", matchcode: "MC-1", sollmenge: 5m, fertigungmenge: 7m),
        };

        var result = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 1, 100, paginate: true);

        var row = result.Rows.Single();
        row.Matchcode.Should().Be("MC-1");
        row.SummeSollmenge.Should().Be(7m);       // 2 + 5
        row.SummeFertigungmenge.Should().Be(10m); // 3 + 7
        result.TotalRowCount.Should().Be(1);
    }

    [Fact]
    public void Sicht2_SameArtnrDifferentMatchcode_ProducesMultipleRows()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "SAME", matchcode: "MC-1", sollmenge: 1m, fertigungmenge: 1m),
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "SAME", matchcode: "MC-2", sollmenge: 4m, fertigungmenge: 4m),
        };

        var result = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 1, 100, paginate: true);

        result.Rows.Select(r => r.Matchcode).Should().BeEquivalentTo(new[] { "MC-1", "MC-2" });
        result.TotalRowCount.Should().Be(2);
    }

    [Fact]
    public void Sicht2_AggregatesPerReiter_ExcludesOtherBereich()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 3m),
            Node(1, subFa: 0, vmBedarf: "VM-B", matchcode: "MC-1", sollmenge: 9m), // anderer Reiter -> raus
        };

        var result = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(3m); // nur VM-A, NICHT 12
    }

    [Fact]
    public void Sicht2_IncludesAllSubFaLevels_InAggregationBase()
    {
        // Nachtrag 2026-08-13, Aenderung 1: SubFA != 0 zaehlt JETZT in die Aggregation mit hinein
        // (frueher ausgeschlossen). Beide Zeilen desselben Matchcodes -> eine Summe.
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 2m),
            Node(1, subFa: 7, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 100m), // SubFA != 0 -> zaehlt mit
        };

        var result = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(102m); // 2 + 100
    }

    [Fact]
    public void Sicht2_AggregatePaging_TotalCountCountsAggregateRows()
    {
        var nodes = new List<FaHierarchyNode>();
        for (int i = 1; i <= 5; i++)
            nodes.Add(Node(i, subFa: 0, vmBedarf: "VM-A", matchcode: $"MC-{i}", sollmenge: 1m));

        var page1 = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 1, 2, paginate: true);
        page1.TotalRowCount.Should().Be(5);          // Aggregatzeilen, nicht Positionen
        page1.Rows.Should().HaveCount(2);
        page1.Rows.Select(r => r.Matchcode).Should().Equal("MC-1", "MC-2");

        var page2 = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, null, 2, 2, paginate: true);
        page2.Rows.Select(r => r.Matchcode).Should().Equal("MC-3", "MC-4");
    }

    [Fact]
    public void Sicht2_ColumnFilter_AppliesToAggregateProjection()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "ALPHA", sollmenge: 1m),
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "BETA",  sollmenge: 1m),
        };
        var filters = new Dictionary<string, string> { ["matchcode"] = "alpha" };

        var result = VormontageService.AggregateSummiert(nodes, EmptyInfos(), "VM-A", null, filters, 1, 100, paginate: true);

        result.Rows.Single().Matchcode.Should().Be("ALPHA");
        result.TotalRowCount.Should().Be(1); // TotalCount zaehlt NACH Spaltenfilter
    }

    [Fact]
    public void Sicht2_KwFilter_OnFeTermin_AppliesBeforeAggregation()
    {
        // Nachtrag 2026-08-13, Aenderung 2 (AK10): zwei HauptFA mit FE_Termin in unterschiedlichen KW,
        // gleicher Matchcode. Filter auf KW von HauptFA 1 -> nur dessen Menge fliesst in die Summe.
        var kwOne = new DateTime(2026, 3, 4);  // Mittwoch, ISO-KW 10/2026
        var kwTwo = new DateTime(2026, 3, 11); // Mittwoch, ISO-KW 11/2026
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 4m),
            Node(2, subFa: 0, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 40m),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(1, feTermin: kwOne),
            Info(2, feTermin: kwTwo),
        };

        var range = IsoWeekRange.Resolve("2026-W10", "2026-W10");
        var result = VormontageService.AggregateSummiert(nodes, infos, "VM-A", range, null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(4m); // nur HauptFA 1 (KW 10), NICHT 44
    }

    private static List<FaHierarchyOrderInfo> EmptyInfos() => new();
}
