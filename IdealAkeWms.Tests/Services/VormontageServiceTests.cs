using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer IDEAL Teil 5 (Vormontage-Listen). Belegen:
/// <list type="bullet">
///   <item><b>Sicht 1 (Einzelne Teile)</b> nutzt den gemeinsamen <see cref="FaHierarchyListBuilder"/>
///     mit <c>leafOnly: true</c> + <c>anomalyOnNonLeaf: true</c> (wie Teil 3): Blattfilter, Anomalie
///     (SubFA != 0 mit VMBedarf) ausgeschlossen + Banner, Fan-out-freier Kopf-Join, Gruppen-Paging.</item>
///   <item><b>Sicht 2 (Summiert)</b> aggregiert je VMBedarf-Reiter nach <b>Matchcode</b> (nicht Artnr)
///     mit zwei getrennten Summen (Sollmenge UND Fertigungmenge); gleicher Artnr mit verschiedenem
///     Matchcode ergibt bewusst mehrere Zeilen; Seiteneinheit = Aggregatzeile.</item>
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

    private static FaHierarchyOrderInfo Info(int hauptFa, string? montage = null, string? kunde = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, MontageAbteilung = montage, Kunde = kunde };

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
    public async Task Sicht1_LeafFilter_ExcludesSubFaNonZero_AndReportsAnomalyBanner()
    {
        // HauptFA 100: Verweiszeile (SubFA != 0) mit VMBedarf -> Anomalie; Blattzeilen (SubFA = 0)
        // gelistet; Blatt ohne VMBedarf ausgeschlossen. Analog Teil 3 AK1/AK3.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 5, vmBedarf: "VM-A", artnr: "REF", position: 0),      // Anomalie
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, vmBedarf: "VM-A", artnr: "LEAF-B", position: 2),
            Node(100, subFa: 0, vmBedarf: null,   artnr: "LEAF-C", position: 3),   // kein Flag -> raus
        };

        var result = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, 1, 100, paginate: true);

        var artnrs = result.Groups.Single().Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "LEAF-A", "LEAF-B" });
        artnrs.Should().NotContain("REF");
        artnrs.Should().NotContain("LEAF-C");

        // Anomalie ausgewiesen (Banner) und NICHT gelistet.
        result.Anomalies.HasAnomalies.Should().BeTrue();
        result.Anomalies.PositionCount.Should().Be(1);
        result.Anomalies.AffectedHauptFas.Should().BeEquivalentTo(new[] { 100 });
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

        var group = (await NewService(nodes, infos).BuildEinzelteileAsync("VM-A", null, 1, 100, true))
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

        var resultA = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, 1, 100, true);
        resultA.Groups.Single().Positions.Select(p => p.Artnr).Should().BeEquivalentTo(new[] { "A1" });
    }

    [Fact]
    public async Task Sicht1_GroupPaging_TotalCountCountsGroups()
    {
        var nodes = new List<FaHierarchyNode>();
        for (int hf = 1; hf <= 5; hf++)
            nodes.Add(Node(hf, subFa: 0, vmBedarf: "VM-A", artnr: $"A{hf}", position: 1));

        var page1 = await NewService(nodes).BuildEinzelteileAsync("VM-A", null, 1, 2, true);
        page1.TotalGroupCount.Should().Be(5);
        page1.Groups.Select(g => g.HauptFA).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetBereiche_DistinctNonEmpty_SortedAndLeafOnly()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-B", position: 1),
            Node(1, subFa: 0, vmBedarf: "VM-A", position: 2),
            Node(1, subFa: 0, vmBedarf: "VM-A", position: 3),   // Duplikat
            Node(1, subFa: 0, vmBedarf: "",     position: 4),   // leer -> kein Reiter
            Node(1, subFa: 0, vmBedarf: null,   position: 5),   // null -> kein Reiter
            Node(1, subFa: 7, vmBedarf: "VM-Z", position: 6),   // SubFA != 0 -> nicht in Reiter-Enumeration
        };

        var bereiche = await NewService(nodes).GetBereicheAsync();

        bereiche.Should().Equal("VM-A", "VM-B");   // sortiert, distinct, ohne leer/null, nur Blatt
    }

    // ----- Sicht 2 (Summiert) — eigene Aggregat-Logik -----

    [Fact]
    public void Sicht2_AggregatesByMatchcode_SumsBothQuantities()
    {
        // Zwei Positionen mit gleichem Matchcode -> eine Zeile mit summierter Sollmenge/Fertigungmenge.
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "X", matchcode: "MC-1", sollmenge: 2m, fertigungmenge: 3m),
            Node(2, subFa: 0, vmBedarf: "VM-A", artnr: "Y", matchcode: "MC-1", sollmenge: 5m, fertigungmenge: 7m),
        };

        var result = VormontageService.AggregateSummiert(nodes, "VM-A", null, 1, 100, paginate: true);

        var row = result.Rows.Single();
        row.Matchcode.Should().Be("MC-1");
        row.SummeSollmenge.Should().Be(7m);       // 2 + 5
        row.SummeFertigungmenge.Should().Be(10m); // 3 + 7
        result.TotalRowCount.Should().Be(1);
    }

    [Fact]
    public void Sicht2_SameArtnrDifferentMatchcode_ProducesMultipleRows()
    {
        // Bewusst akzeptiert (Spec F.A. 7): gleicher Artnr, verschiedene Matchcodes -> zwei Zeilen.
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "SAME", matchcode: "MC-1", sollmenge: 1m, fertigungmenge: 1m),
            Node(1, subFa: 0, vmBedarf: "VM-A", artnr: "SAME", matchcode: "MC-2", sollmenge: 4m, fertigungmenge: 4m),
        };

        var result = VormontageService.AggregateSummiert(nodes, "VM-A", null, 1, 100, paginate: true);

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

        var result = VormontageService.AggregateSummiert(nodes, "VM-A", null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(3m); // nur VM-A, NICHT 12
    }

    [Fact]
    public void Sicht2_ExcludesSubFaNonZero_FromAggregationBase()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 2m),
            Node(1, subFa: 7, vmBedarf: "VM-A", matchcode: "MC-1", sollmenge: 100m), // SubFA != 0 -> raus
        };

        var result = VormontageService.AggregateSummiert(nodes, "VM-A", null, 1, 100, paginate: true);

        result.Rows.Single().SummeSollmenge.Should().Be(2m); // Baugruppen-Zeile zaehlt nicht mit
    }

    [Fact]
    public void Sicht2_AggregatePaging_TotalCountCountsAggregateRows()
    {
        // 5 verschiedene Matchcodes -> 5 Aggregatzeilen; Seiteneinheit = Aggregatzeile.
        var nodes = new List<FaHierarchyNode>();
        for (int i = 1; i <= 5; i++)
            nodes.Add(Node(i, subFa: 0, vmBedarf: "VM-A", matchcode: $"MC-{i}", sollmenge: 1m));

        var page1 = VormontageService.AggregateSummiert(nodes, "VM-A", null, 1, 2, paginate: true);
        page1.TotalRowCount.Should().Be(5);          // Aggregatzeilen, nicht Positionen
        page1.Rows.Should().HaveCount(2);
        page1.Rows.Select(r => r.Matchcode).Should().Equal("MC-1", "MC-2");

        var page2 = VormontageService.AggregateSummiert(nodes, "VM-A", null, 2, 2, paginate: true);
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

        var result = VormontageService.AggregateSummiert(nodes, "VM-A", filters, 1, 100, paginate: true);

        result.Rows.Single().Matchcode.Should().Be("ALPHA");
        result.TotalRowCount.Should().Be(1); // TotalCount zaehlt NACH Spaltenfilter
    }
}
