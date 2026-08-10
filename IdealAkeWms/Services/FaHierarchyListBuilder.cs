using System;
using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>
/// <b>Gemeinsamer, wiederverwendbarer FaHierarchy-Listen-/Druck-Baustein</b> — die
/// Referenzmechanik hinter den IDEAL-Positionslisten (Teil 3 Kommissionierlisten, Teil 4
/// Beschichtung, Teil 5 Vormontage). Kapselt die vier Dinge, die alle drei Teile teilen, sodass
/// Teil 4/5 den Baustein <b>erweitern statt duplizieren</b> (Spec Teil 3, Fachliche Anforderung 11):
/// <list type="number">
///   <item><b>Blatt-Filter</b> <c>SubFA = 0</c> zuerst, danach das uebergebene <b>Flag-Praedikat</b>
///     (der einzige teilspezifische Parameter: Kommissionieren / Beschichtet / VMBedarf).</item>
///   <item><b>Anomalie-Erkennung</b>: Zeilen mit <c>SubFA != 0</c> UND gesetztem Flag werden NICHT
///     gelistet, sondern gesammelt (Banner-Daten) und je <c>HauptFA</c>/Request dedupliziert per
///     <see cref="ILogger"/> gewarnt (kein SyncLog — Web-Lesefunktion, ADR 0010 gilt nur fuer
///     Hintergrund-Services).</item>
///   <item><b>Fan-out-freier Header-Join</b>: Kopfdaten (<see cref="FaHierarchyOrderInfo"/>) werden
///     ueber ein Dictionary je <c>HauptFA</c> zugeordnet — <b>kein</b> <c>INNER JOIN ... ON HauptFA</c>
///     gegen die Positionszeilen (der wuerde bei mehreren OrderInfo-Zeilen je HauptFA jede Menge
///     vervielfachen).</item>
///   <item><b>Gruppierung + Gruppen-Paging</b>: Seiteneinheit ist die Gruppe (<c>HauptFA</c>),
///     <c>TotalGroupCount</c> zaehlt Gruppen, keine Gruppe wird ueber Seiten getrennt (ADR 0005,
///     geschaerft fuer den Gruppen-Fall).</item>
/// </list>
///
/// <para><b>Erweiterungs-Naht fuer Teil 4/5 (ohne Raten treffen):</b> Ein neuer Teil ruft
/// <see cref="Build"/> mit (a) seinem Flag-Praedikat + Flag-Namen, (b) seiner teilspezifischen
/// <c>ColumnMap</c> fuer den Server-Spaltenfilter und (c) den Paging-Parametern auf. Alles andere
/// (Blattfilter, Anomalie, Header-Join, Gruppen-Paging) bleibt hier. Der teilspezifische Rest ist
/// nur: Flag-Praedikat, ColumnMap, Kopf-/Spalten-Layout der View, ggf. eine Zweitsicht (Teil 5
/// Aggregat) — die den Baustein nicht braucht.</para>
///
/// Rein funktional/stateless (nur <see cref="ILogger"/> als Abhaengigkeit) und damit ohne DB
/// unit-testbar.
/// </summary>
public class FaHierarchyListBuilder
{
    private readonly ILogger<FaHierarchyListBuilder> _logger;

    public FaHierarchyListBuilder(ILogger<FaHierarchyListBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Baut die gruppierte, (optional) paginierte Positionsliste je <c>HauptFA</c>.
    /// </summary>
    /// <param name="allNodes">Alle geladenen Positionszeilen (ungefiltert).</param>
    /// <param name="allOrderInfos">Alle Kopf-Zeilen (ungefiltert) — fuer den Fan-out-freien Join.</param>
    /// <param name="flagPredicate">Teilspezifisches Flag-Praedikat, z. B.
    ///   <c>n =&gt; !string.IsNullOrWhiteSpace(n.Kommissionieren)</c>. Wird sowohl auf Blaetter
    ///   (Anzeige) als auch auf <c>SubFA != 0</c>-Zeilen (Anomalie) angewandt — so bleibt eine
    ///   optionale Ziel-Wert-Einschraenkung (z. B. Kommissionier-Ziel) in beiden Kanaelen konsistent.</param>
    /// <param name="flagName">Klartext-Name des Flags fuer die Log-Warnung (z. B. "Kommissionieren").</param>
    /// <param name="columnFilters">Server-Spaltenfilter aus <c>colf_*</c> (oder null/leer).</param>
    /// <param name="columnMap">Teilspezifische Col-Key -&gt; Zelltext-Map fuer die Spaltenfilter
    ///   (oder null, wenn keine Spaltenfilter anzuwenden sind — z. B. beim Vorbefuellen des
    ///   Ziel-Dropdowns).</param>
    /// <param name="page">1-basierte Seitennummer (nur wenn <paramref name="paginate"/>).</param>
    /// <param name="pageSize">Effektive Seitengroesse (nur wenn <paramref name="paginate"/>).</param>
    /// <param name="paginate">false = ganze gefilterte Menge (Druck), true = eine Seite (Bildschirm).</param>
    public FaHierarchyListResult Build(
        IEnumerable<FaHierarchyNode> allNodes,
        IEnumerable<FaHierarchyOrderInfo> allOrderInfos,
        Func<FaHierarchyNode, bool> flagPredicate,
        string flagName,
        IReadOnlyDictionary<string, string>? columnFilters,
        IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>>? columnMap,
        int page,
        int pageSize,
        bool paginate)
    {
        ArgumentNullException.ThrowIfNull(allNodes);
        ArgumentNullException.ThrowIfNull(flagPredicate);

        var nodes = allNodes as IList<FaHierarchyNode> ?? allNodes.ToList();

        // (2) Anomalie-Erkennung: SubFA != 0 UND Flag gesetzt. Reihenfolge bewusst: Ebene zuerst,
        // dann Flag — identisch zur Blatt-Logik unten, nur invertierte Ebene.
        var anomalyNodes = nodes.Where(n => n.SubFA != 0 && flagPredicate(n)).ToList();
        var anomalyGroups = anomalyNodes
            .GroupBy(n => n.HauptFA)
            .OrderBy(g => g.Key)
            .ToList();

        // Log je HauptFA/Request dedupliziert (eine Warnung je Gruppe statt je Position) —
        // sonst Log-Rauschen bei wiederholtem Blaettern/Filtern derselben Anomalien.
        foreach (var g in anomalyGroups)
        {
            var positions = string.Join(", ", g.Select(n => n.Position?.ToString() ?? "?"));
            var artnrs = string.Join(", ", g.Select(n => n.Artnr ?? "?").Distinct());
            _logger.LogWarning(
                "FaHierarchy-Liste ({Flag}): {Count} Position(en) mit gesetztem {Flag} auf "
                + "Baugruppen-Ebene (SubFA != 0) in HauptFA {HauptFA} ausgeschlossen "
                + "(Position(en): {Positions}; Artnr: {Artnr}). Datenpflege pruefen.",
                flagName, g.Count(), flagName, g.Key, positions, artnrs);
        }

        var anomalyBanner = new FaHierarchyAnomalyBannerViewModel
        {
            PositionCount = anomalyNodes.Count,
            AffectedHauptFas = anomalyGroups.Select(g => g.Key).ToList()
        };

        // (1) Blatt-Filter: erst SubFA = 0, dann Flag.
        IEnumerable<FaHierarchyNode> leaves = nodes.Where(n => n.SubFA == 0 && flagPredicate(n));

        // (3) Server-Side-Spaltenfilter auf den Positionszeilen — VOR der Gruppierung (ADR 0005).
        if (columnMap != null)
            leaves = ColumnFilterHelper.Apply(leaves, columnFilters, columnMap);

        // (4) Gruppieren nach HauptFA; TotalCount zaehlt Gruppen.
        var groups = leaves
            .GroupBy(n => n.HauptFA)
            .OrderBy(g => g.Key)
            .ToList();
        var totalGroupCount = groups.Count;

        // (4) Gruppen-Paging: Seiteneinheit = Gruppe, nie ueber Seiten getrennt.
        var pageGroups = paginate
            ? groups.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize)
            : groups;

        // (5) Fan-out-freier Header-Join: OrderInfos je HauptFA aus einem Dictionary zuordnen.
        var infoLookup = (allOrderInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>())
            .GroupBy(o => o.HauptFA)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultGroups = pageGroups
            .Select(g => new FaHierarchyListGroupViewModel
            {
                HauptFA = g.Key,
                Positions = g.OrderBy(n => n.Position).ToList(),
                OrderInfos = infoLookup.TryGetValue(g.Key, out var oi)
                    ? oi
                    : new List<FaHierarchyOrderInfo>()
            })
            .ToList();

        return new FaHierarchyListResult
        {
            Groups = resultGroups,
            TotalGroupCount = totalGroupCount,
            Anomalies = anomalyBanner
        };
    }
}

/// <summary>Ergebnis eines <see cref="FaHierarchyListBuilder.Build"/>-Laufs (eine Seite bzw. die
/// gesamte gefilterte Menge im Druckfall) inklusive der Banner-Daten.</summary>
public class FaHierarchyListResult
{
    /// <summary>Die (paginierten) Gruppen dieser Seite bzw. alle Gruppen im Druckfall.</summary>
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>Anzahl aller Gruppen (Seiteneinheit) VOR dem Paging — fuer <c>PaginationState.TotalCount</c>.</summary>
    public int TotalGroupCount { get; init; }

    /// <summary>Anomalie-Banner-Daten (ueber die gesamte gefilterte Flag-Menge, nicht nur die Seite).</summary>
    public FaHierarchyAnomalyBannerViewModel Anomalies { get; init; } = new();
}
