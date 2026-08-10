using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer den gemeinsamen Listen-/Druck-Baustein <see cref="FaHierarchyListBuilder"/>
/// (IDEAL Teil 3 Referenzimplementierung). Pruefen die vier Baustein-Invarianten unabhaengig vom
/// Testdatenbestand: Blatt-nur (SubFA = 0), Anomalie-Erkennung/Banner, Fan-out-Freiheit,
/// Gruppen-Paging.
/// </summary>
public class FaHierarchyListBuilderTests
{
    private static FaHierarchyListBuilder NewBuilder() =>
        new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance);

    // Kommissionieren-Flag-Praedikat (Teil-3-Referenz).
    private static readonly Func<FaHierarchyNode, bool> KommFlag =
        n => !string.IsNullOrWhiteSpace(n.Kommissionieren);

    private static FaHierarchyNode Node(int hauptFa, int subFa, string? komm = null,
        string? artnr = null, int? position = null) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa,
            SubFA = subFa,
            Kommissionieren = komm,
            Artnr = artnr,
            Position = position
        };

    private static FaHierarchyOrderInfo Info(int hauptFa, string? montage, string? kunde = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, MontageAbteilung = montage, Kunde = kunde };

    private static FaHierarchyListResult Build(
        IEnumerable<FaHierarchyNode> nodes,
        IEnumerable<FaHierarchyOrderInfo> infos,
        int page = 1, int pageSize = 100, bool paginate = true) =>
        NewBuilder().Build(nodes, infos, KommFlag, "Kommissionieren",
            columnFilters: null, columnMap: null, page: page, pageSize: pageSize, paginate: paginate);

    [Fact]
    public void OnlyLeafPositions_SubFaZero_AreListed_ReferenceRowExcluded()
    {
        // HauptFA 100: eine Verweiszeile (SubFA != 0) mit Kommissionieren + ein Sub-FA mit eigenen
        // Blattzeilen (SubFA = 0). Erwartet: Verweiszeile NICHT gelistet, Blattzeilen je einmal.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 5, komm: "LAGER", artnr: "REF"),   // Verweis auf Baugruppe -> ausgeschlossen
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF-B", position: 2),
            Node(100, subFa: 0, komm: null,    artnr: "LEAF-C", position: 3), // ohne Flag -> ausgeschlossen
        };

        var result = Build(nodes, new List<FaHierarchyOrderInfo>());

        result.Groups.Should().HaveCount(1);
        var artnrs = result.Groups[0].Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "LEAF-A", "LEAF-B" });
        artnrs.Should().NotContain("REF");
        artnrs.Should().NotContain("LEAF-C");
    }

    [Fact]
    public void Anomaly_SubFaNonZero_WithFlag_IsExcluded_AndReportedInBanner()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF", position: 1),
            Node(100, subFa: 7, komm: "LAGER", artnr: "ANOM-1", position: 2), // Anomalie
            Node(200, subFa: 3, komm: "WERK",  artnr: "ANOM-2", position: 1), // Anomalie, anderer HauptFA
        };

        var result = Build(nodes, new List<FaHierarchyOrderInfo>());

        result.Anomalies.HasAnomalies.Should().BeTrue();
        result.Anomalies.PositionCount.Should().Be(2);
        result.Anomalies.AffectedHauptFas.Should().BeEquivalentTo(new[] { 100, 200 });
        // Anomalien tauchen NIE in der Liste selbst auf.
        result.Groups.SelectMany(g => g.Positions).Select(p => p.Artnr)
            .Should().NotContain(new[] { "ANOM-1", "ANOM-2" });
        // HauptFA 200 hat nur eine Anomalie -> keine gelistete Gruppe.
        result.Groups.Select(g => g.HauptFA).Should().BeEquivalentTo(new[] { 100 });
    }

    [Fact]
    public void NoAnomalies_BannerStaysInvisible()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF", position: 1),
        };

        var result = Build(nodes, new List<FaHierarchyOrderInfo>());

        result.Anomalies.HasAnomalies.Should().BeFalse();
        result.Anomalies.PositionCount.Should().Be(0);
        result.Anomalies.AffectedHauptFas.Should().BeEmpty();
    }

    [Fact]
    public void HeaderJoin_MultipleOrderInfosPerHauptFa_DoesNotMultiplyPositions()
    {
        // Kombigeraet: zwei OrderInfo-Zeilen je HauptFA. Erwartet: Kopf zeigt beide,
        // Positionsanzahl bleibt 1:1 zur Quellzeilenzahl (kein Fan-out).
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF-A", position: 1),
            Node(100, subFa: 0, komm: "LAGER", artnr: "LEAF-B", position: 2),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(100, "Montage-1", "Kunde X"),
            Info(100, "Montage-2", "Kunde X"),
        };

        var result = Build(nodes, infos);

        var group = result.Groups.Single();
        group.Positions.Should().HaveCount(2);          // NICHT 4 (kein kartesisches Produkt)
        group.OrderInfos.Should().HaveCount(2);
        group.IsAmbiguous.Should().BeTrue();
    }

    [Fact]
    public void GroupPaging_TotalCountCountsGroups_NotPositionRows()
    {
        // 5 HauptFA-Gruppen mit je 2 Blattpositionen (=10 Zeilen). Erwartet: TotalGroupCount = 5.
        var nodes = new List<FaHierarchyNode>();
        for (int hf = 1; hf <= 5; hf++)
        {
            nodes.Add(Node(hf, subFa: 0, komm: "LAGER", artnr: $"A{hf}", position: 1));
            nodes.Add(Node(hf, subFa: 0, komm: "LAGER", artnr: $"B{hf}", position: 2));
        }

        var page1 = Build(nodes, new List<FaHierarchyOrderInfo>(), page: 1, pageSize: 2);
        page1.TotalGroupCount.Should().Be(5);                 // Gruppen, nicht Zeilen
        page1.Groups.Select(g => g.HauptFA).Should().Equal(1, 2);

        var page2 = Build(nodes, new List<FaHierarchyOrderInfo>(), page: 2, pageSize: 2);
        page2.TotalGroupCount.Should().Be(5);
        page2.Groups.Select(g => g.HauptFA).Should().Equal(3, 4);
    }

    [Fact]
    public void GroupPaging_GroupIsNeverSplitAcrossPages()
    {
        // HauptFA 1 mit 3 Blattzeilen, HauptFA 2 mit 3 Blattzeilen; pageSize = 1 Gruppe.
        var nodes = new List<FaHierarchyNode>
        {
            Node(1, subFa: 0, komm: "LAGER", artnr: "A1", position: 1),
            Node(1, subFa: 0, komm: "LAGER", artnr: "A2", position: 2),
            Node(1, subFa: 0, komm: "LAGER", artnr: "A3", position: 3),
            Node(2, subFa: 0, komm: "LAGER", artnr: "B1", position: 1),
            Node(2, subFa: 0, komm: "LAGER", artnr: "B2", position: 2),
            Node(2, subFa: 0, komm: "LAGER", artnr: "B3", position: 3),
        };

        var page1 = Build(nodes, new List<FaHierarchyOrderInfo>(), page: 1, pageSize: 1);
        page1.Groups.Should().HaveCount(1);
        page1.Groups[0].HauptFA.Should().Be(1);
        page1.Groups[0].Positions.Should().HaveCount(3); // ganze Gruppe auf einer Seite
    }

    [Fact]
    public void Print_NoPagination_ReturnsAllGroups()
    {
        var nodes = new List<FaHierarchyNode>();
        for (int hf = 1; hf <= 5; hf++)
            nodes.Add(Node(hf, subFa: 0, komm: "LAGER", artnr: $"A{hf}", position: 1));

        var result = Build(nodes, new List<FaHierarchyOrderInfo>(), page: 1, pageSize: 2, paginate: false);

        result.Groups.Should().HaveCount(5);      // Druck ignoriert Paging
        result.TotalGroupCount.Should().Be(5);
    }

    [Fact]
    public void TargetPredicate_RestrictsToSelectedTarget_AndScopesAnomalies()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, komm: "LAGER", artnr: "L1", position: 1),
            Node(100, subFa: 0, komm: "WERK",  artnr: "W1", position: 2),
            Node(100, subFa: 4, komm: "LAGER", artnr: "ANOM-LAGER", position: 3), // Anomalie, Ziel LAGER
            Node(100, subFa: 4, komm: "WERK",  artnr: "ANOM-WERK",  position: 4), // Anomalie, Ziel WERK
        };

        var predicate = KommissionierListenService.BuildFlagPredicate("LAGER");
        var result = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(),
            predicate, "Kommissionieren", null, null, 1, 100, true);

        // Nur LAGER-Blaetter gelistet.
        result.Groups.Single().Positions.Select(p => p.Artnr).Should().BeEquivalentTo(new[] { "L1" });
        // Anomalie nur fuer das gewaehlte Ziel LAGER (WERK-Anomalie faellt nicht ins Banner).
        result.Anomalies.PositionCount.Should().Be(1);
    }
}
