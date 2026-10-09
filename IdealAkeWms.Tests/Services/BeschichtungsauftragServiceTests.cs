using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer IDEAL Teil 4 (Beschichtungsauftrag). Belegen die Kern-Feinheit der Baustein-Naht:
/// Teil 4 erweitert <see cref="FaHierarchyListBuilder"/> mit <c>leafOnly: false</c> +
/// <c>anomalyOnNonLeaf: false</c> — Beschichtet-Filter ueber ALLE Ebenen (SubFA != 0 legitim,
/// KEINE Anomalie), Fan-out-freier Kopf-Join, Gruppen-Paging, ILogger-Warnung bei Mehrdeutigkeit.
/// Der letzte Test bestaetigt zusaetzlich, dass das Teil-3-Default-Verhalten unveraendert bleibt.
/// </summary>
public class BeschichtungsauftragServiceTests
{
    private static FaHierarchyListBuilder NewBuilder() =>
        new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance);

    private static FaHierarchyNode Node(int hauptFa, int subFa, bool beschichtet,
        string? artnr = null, int? position = null) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa,
            SubFA = subFa,
            Beschichtet = beschichtet,
            Artnr = artnr,
            Position = position
        };

    private static FaHierarchyOrderInfo Info(int hauptFa, string? dienstleister = null,
        string? montage = null) =>
        new FaHierarchyOrderInfo { HauptFA = hauptFa, Dienstleister = dienstleister, MontageAbteilung = montage };

    /// <summary>Baut ueber den Baustein exakt mit der Teil-4-Naht (leafOnly:false, anomaly:false).</summary>
    private static FaHierarchyListResult BuildTeil4(
        IEnumerable<FaHierarchyNode> nodes,
        IEnumerable<FaHierarchyOrderInfo> infos,
        int page = 1, int pageSize = 100, bool paginate = true) =>
        NewBuilder().Build(nodes, infos, BeschichtungsauftragService.FlagPredicate,
            BeschichtungsauftragService.FlagName, columnFilters: null,
            columnMap: BeschichtungsauftragService.ColumnMap,
            page: page, pageSize: pageSize, paginate: paginate,
            leafOnly: false, anomalyOnNonLeaf: false);

    [Fact]
    public void Filter_OnlyBeschichtetTrue_IsListed()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "B-1", position: 1),
            Node(100, subFa: 0, beschichtet: false, artnr: "N-1", position: 2), // nicht beschichtet -> raus
            Node(100, subFa: 0, beschichtet: true, artnr: "B-2", position: 3),
        };

        var result = BuildTeil4(nodes, new List<FaHierarchyOrderInfo>());

        var artnrs = result.Groups.Single().Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "B-1", "B-2" });
        artnrs.Should().NotContain("N-1");
    }

    [Fact]
    public void AllLevels_SubFaNonZero_WithBeschichtet_IsListed_AndNoAnomaly()
    {
        // Kern-Feinheit Teil 4: eine beschichtete Baugruppe (SubFA != 0) gehoert MIT auf die Liste
        // und ist KEINE Anomalie (anders als Teil 3).
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "LEAF", position: 1),
            Node(100, subFa: 7, beschichtet: true, artnr: "BAUGRUPPE", position: 2), // SubFA != 0
        };

        var result = BuildTeil4(nodes, new List<FaHierarchyOrderInfo>());

        var artnrs = result.Groups.Single().Positions.Select(p => p.Artnr).ToList();
        artnrs.Should().BeEquivalentTo(new[] { "LEAF", "BAUGRUPPE" });
        // KEIN Anomalie-Banner in Teil 4.
        result.Anomalies.HasAnomalies.Should().BeFalse();
        result.Anomalies.PositionCount.Should().Be(0);
        result.Anomalies.AffectedHauptFas.Should().BeEmpty();
    }

    [Fact]
    public void HeaderJoin_MultipleOrderInfosPerHauptFa_DoesNotMultiplyPositions()
    {
        // Dienstleister-Kopf 1:n -> kein Fan-out: Positionsanzahl bleibt 1:1.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "B-1", position: 1),
            Node(100, subFa: 0, beschichtet: true, artnr: "B-2", position: 2),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(100, "Beschichter A", "Montage-1"),
            Info(100, "Beschichter B", "Montage-2"),
        };

        var group = BuildTeil4(nodes, infos).Groups.Single();

        group.Positions.Should().HaveCount(2);   // NICHT 4 (kein kartesisches Produkt)
        group.OrderInfos.Should().HaveCount(2);
        group.IsAmbiguous.Should().BeTrue();
    }

    [Fact]
    public void GroupPaging_TotalCountCountsGroups_NotPositionRows()
    {
        var nodes = new List<FaHierarchyNode>();
        for (int hf = 1; hf <= 5; hf++)
        {
            nodes.Add(Node(hf, subFa: 0, beschichtet: true, artnr: $"A{hf}", position: 1));
            nodes.Add(Node(hf, subFa: 3, beschichtet: true, artnr: $"B{hf}", position: 2)); // auch SubFA != 0
        }

        var page1 = BuildTeil4(nodes, new List<FaHierarchyOrderInfo>(), page: 1, pageSize: 2);
        page1.TotalGroupCount.Should().Be(5);                       // Gruppen, nicht Zeilen
        page1.Groups.Select(g => g.HauptFA).Should().Equal(1, 2);

        var page2 = BuildTeil4(nodes, new List<FaHierarchyOrderInfo>(), page: 2, pageSize: 2);
        page2.Groups.Select(g => g.HauptFA).Should().Equal(3, 4);
    }

    [Fact]
    public void EmptyGroup_AfterFilter_Disappears()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "B-1", position: 1),
            Node(200, subFa: 0, beschichtet: false, artnr: "N-1", position: 1), // ganze Gruppe faellt weg
        };

        var result = BuildTeil4(nodes, new List<FaHierarchyOrderInfo>());

        result.Groups.Select(g => g.HauptFA).Should().Equal(100);
        result.TotalGroupCount.Should().Be(1);
    }

    [Fact]
    public async Task BuildAsync_LogsWarning_ForAmbiguousCombigeraet()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "B-1", position: 1),
        };
        var infos = new List<FaHierarchyOrderInfo>
        {
            Info(100, "Beschichter A", "Montage-1"),
            Info(100, "Beschichter B", "Montage-2"),
        };

        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(infos);

        var recording = new RecordingLogger<BeschichtungsauftragService>();
        var service = new BeschichtungsauftragService(
            nodeRepo.Object, infoRepo.Object, NewBuilder(), recording);

        var result = await service.BuildAsync(null, 1, 100, paginate: true);

        result.Groups.Single().IsAmbiguous.Should().BeTrue();
        recording.Warnings.Should().ContainSingle()
            .Which.Should().Contain("100").And.Contain("2"); // HauptFA + Anzahl Kopfzeilen
    }

    [Fact]
    public async Task BuildAsync_NoWarning_WhenHeadUnambiguous()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "B-1", position: 1),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, "Beschichter A", "Montage-1") };

        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(infos);

        var recording = new RecordingLogger<BeschichtungsauftragService>();
        var service = new BeschichtungsauftragService(
            nodeRepo.Object, infoRepo.Object, NewBuilder(), recording);

        await service.BuildAsync(null, 1, 100, paginate: true);

        recording.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Teil3Default_StaysLeafOnly_AndAnomalyOn_WhenNewParamsOmitted()
    {
        // Regression: die neuen Opt-in-Parameter defaulten auf Teil-3-Verhalten. Wird Build OHNE
        // sie aufgerufen, bleibt der Blattfilter aktiv und SubFA != 0 mit Flag ist Anomalie.
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, subFa: 0, beschichtet: true, artnr: "LEAF", position: 1),
            Node(100, subFa: 7, beschichtet: true, artnr: "BAUGRUPPE", position: 2),
        };

        var result = NewBuilder().Build(nodes, new List<FaHierarchyOrderInfo>(),
            BeschichtungsauftragService.FlagPredicate, "Beschichtet",
            columnFilters: null, columnMap: null, page: 1, pageSize: 100, paginate: true);

        // Default leafOnly => nur das Blatt ist gelistet ...
        result.Groups.Single().Positions.Select(p => p.Artnr).Should().Equal("LEAF");
        // ... und die SubFA != 0-Zeile ist Anomalie (Default anomalyOnNonLeaf).
        result.Anomalies.HasAnomalies.Should().BeTrue();
        result.Anomalies.PositionCount.Should().Be(1);
    }

    // ----- Summiert-Ansicht (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-1, v1.48.0) -----

    private static FaHierarchyNode SNode(int hauptFa, string? artnr, decimal sollmenge,
        int subFa = 0, bool beschichtet = true, int? position = null,
        decimal breite = 0m, decimal hoehe = 0m, decimal tiefe = 0m, string? matchcode = null) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa, SubFA = subFa, Beschichtet = beschichtet, Artnr = artnr,
            Sollmenge = sollmenge, Position = position, Breite = breite, Hoehe = hoehe, Tiefe = tiefe,
            Matchcode = matchcode
        };

    private static FaHierarchyBeschichtungSummiertResult Aggregate(
        IEnumerable<FaHierarchyNode> nodes, IEnumerable<FaHierarchyOrderInfo>? infos = null,
        Dictionary<string, string>? filters = null, int page = 1, int pageSize = 100, bool paginate = true) =>
        BeschichtungsauftragService.AggregateSummiert(
            nodes, infos ?? new List<FaHierarchyOrderInfo>(), filters, page, pageSize, paginate);

    [Fact]
    public void Summiert_SameHauptFaAndArtnr_IsOneRow_WithSumAndPositionCount()
    {
        var result = Aggregate(new[]
        {
            SNode(100, "A1", 2m, position: 1, breite: 500m, hoehe: 20m, tiefe: 3m, matchcode: "Blende"),
            SNode(100, "A1", 3m, position: 2, breite: 500m, hoehe: 20m, tiefe: 3m, matchcode: "Blende"),
        });

        var row = result.Rows.Should().ContainSingle().Subject;
        row.SummeSollmenge.Should().Be(5m);
        row.PositionCount.Should().Be(2);
        row.Matchcode.Should().Be("Blende");
        (row.Breite, row.Hoehe, row.Tiefe).Should().Be((500m, 20m, 3m));
        row.MasseUneinheitlich.Should().BeFalse();
    }

    [Fact]
    public void Summiert_DifferentArtnr_StaysTwoRows()
    {
        var result = Aggregate(new[] { SNode(100, "A1", 1m), SNode(100, "A2", 1m) });

        result.Rows.Select(r => r.Artnr).Should().Equal("A1", "A2");
    }

    [Fact]
    public void Summiert_DifferentHauptFa_NeverMerged()
    {
        var result = Aggregate(new[] { SNode(100, "A1", 1m), SNode(200, "A1", 1m) });

        result.Rows.Select(r => r.HauptFA).Should().Equal(100, 200);
        result.Rows.Should().OnlyContain(r => r.PositionCount == 1);
    }

    [Fact]
    public void Summiert_OnlyBeschichtet_AllLevelsCount()
    {
        var result = Aggregate(new[]
        {
            SNode(100, "A1", 1m, subFa: 0),
            SNode(100, "A1", 4m, subFa: 7),                       // Baugruppe zaehlt mit (AK 5)
            SNode(100, "A1", 9m, beschichtet: false),            // nicht beschichtet -> raus
        });

        result.Rows.Should().ContainSingle().Which.SummeSollmenge.Should().Be(5m);
    }

    [Fact]
    public void Summiert_DimensionDiffersInOneAxis_FlagsAndKeepsFirstPositionValues()
    {
        var result = Aggregate(new[]
        {
            SNode(100, "A1", 1m, position: 2, breite: 500m, hoehe: 20m, tiefe: 4m),
            SNode(100, "A1", 1m, position: 1, breite: 500m, hoehe: 20m, tiefe: 3m), // erste Position
        });

        var row = result.Rows.Should().ContainSingle().Subject;   // Gruppe wird NICHT aufgeteilt
        row.MasseUneinheitlich.Should().BeTrue();
        row.Tiefe.Should().Be(3m);
        row.PositionCount.Should().Be(2);
    }

    [Fact]
    public void Summiert_EmptyArtnr_IsMergedPerHauptFa_LikeKommissionierliste()
    {
        // Review-Focus 1: dieselbe Regel wie KommissionierListenService (leere Artnr = ein Schluessel).
        var result = Aggregate(new[] { SNode(100, null, 1m), SNode(100, "  ", 2m) });

        result.Rows.Should().ContainSingle().Which.SummeSollmenge.Should().Be(3m);
    }

    [Fact]
    public void Summiert_ColumnFilterOnSollmenge_FiltersTheSum()
    {
        var nodes = new[] { SNode(100, "A1", 2m), SNode(100, "A1", 3m), SNode(100, "A2", 2m) };

        var result = Aggregate(nodes, filters: new() { ["sollmenge"] = "5" }); // Summe 5,000 — Einzelwerte 2 und 3 enthalten keine 5 (Komma = ODER im Filter)

        result.Rows.Should().ContainSingle().Which.Artnr.Should().Be("A1");
    }

    [Fact]
    public void Summiert_ColumnFilterOnPositionen_FiltersTheCount()
    {
        var nodes = new[] { SNode(100, "A1", 1m), SNode(100, "A1", 1m), SNode(100, "A2", 1m) };

        var result = Aggregate(nodes, filters: new() { ["positionen"] = "2" });

        result.Rows.Should().ContainSingle().Which.Artnr.Should().Be("A1");
    }

    [Fact]
    public void Summiert_GroupPaging_CountsHauptFaGroups()
    {
        var nodes = new[]
        {
            SNode(100, "A1", 1m), SNode(100, "A2", 1m),
            SNode(200, "A1", 1m), SNode(200, "A2", 1m),
            SNode(300, "A1", 1m),
        };

        var page2 = Aggregate(nodes, page: 2, pageSize: 2);

        page2.TotalGroupCount.Should().Be(3);
        page2.TotalRowCount.Should().Be(5);
        page2.Rows.Select(r => r.HauptFA).Should().Equal(300);
    }

    [Fact]
    public void Summiert_OrderInfos_OnlyForHauptFaOnPage_AndMissingHeadIsEmpty()
    {
        var nodes = new[] { SNode(100, "A1", 1m), SNode(200, "A1", 1m) };
        var infos = new[] { Info(100, "Beschichter A"), Info(100, "Beschichter B"), Info(999, "fremd") };

        var result = Aggregate(nodes, infos);

        result.OrderInfos.Keys.Should().BeEquivalentTo(new[] { 100 });
        result.OrderInfos[100].Should().HaveCount(2);
        result.OrderInfos.ContainsKey(200).Should().BeFalse(); // kein Kopf -> View zeigt Hinweis
    }

    [Fact]
    public async Task BuildSummiertAsync_LogsWarning_ForInconsistentDimensions_AndAmbiguousHead()
    {
        var nodes = new List<FaHierarchyNode>
        {
            SNode(100, "A1", 1m, position: 1, breite: 500m),
            SNode(100, "A1", 1m, position: 2, breite: 600m),
        };
        var infos = new List<FaHierarchyOrderInfo> { Info(100, "A"), Info(100, "B") };
        var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
        nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);
        var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
        infoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(infos);
        var recording = new RecordingLogger<BeschichtungsauftragService>();
        var service = new BeschichtungsauftragService(nodeRepo.Object, infoRepo.Object, NewBuilder(), recording);

        await service.BuildSummiertAsync(null, 1, 100, paginate: true);

        recording.Warnings.Should().HaveCount(2);
        recording.Warnings.Should().Contain(w => w.Contains("100") && w.Contains("A1") && w.Contains("uneinheitlich"));
        recording.Warnings.Should().Contain(w => w.Contains("100") && w.Contains("Kopfzeilen"));
    }

    /// <summary>Minimaler ILogger, der Warnungs-Texte mitschreibt (fuer die Mehrdeutigkeits-Pruefung).</summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
