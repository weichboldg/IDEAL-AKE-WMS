using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>
/// <b>Teil-4-spezifische, duenne Schicht ueber dem gemeinsamen <see cref="FaHierarchyListBuilder"/></b>
/// (IDEAL Beschichtungsauftrag). Entsteht 1:1 nach dem Vorbild von
/// <see cref="KommissionierListenService"/> (Teil 3) — nur das Teilspezifische wechselt:
/// <list type="bullet">
///   <item>das <b>Flag-Praedikat</b> <c>Beschichtet == true</c> (die in Teil 1 aus dem Sage-Rohwert
///     <c>-1</c> gemappte <c>bit</c>-Spalte; der Rohwert <c>-1</c> gehoert NUR in die
///     Teil-1-Import-Logik),</item>
///   <item>die <b>ColumnMap</b> fuer den Server-Spaltenfilter (Teil-4-Positionsspalten),</item>
///   <item>die <b>Baustein-Naht</b>: <c>leafOnly: false</c> + <c>anomalyOnNonLeaf: false</c> —
///     Beschichtung betrifft physische Teile auf <b>jeder</b> Ebene, eine Baugruppe
///     (<c>SubFA != 0</c>) kann selbst beschichtet sein. Deshalb KEIN Blattfilter und KEIN
///     Anomalie-Banner (anders als Teil 3, dessen Default-Verhalten unveraendert bleibt).</item>
/// </list>
/// Der Fan-out-freie Kopf-Join (<see cref="FaHierarchyOrderInfo"/> je <c>HauptFA</c>), die
/// Gruppierung nach <c>HauptFA</c> und das Gruppen-Paging bleiben im Baustein.
///
/// <para><b>Mehrdeutigkeit (Kombinationsgeraet):</b> existieren zu einem <c>HauptFA</c> mehrere
/// FAInfos-Zeilen, fuehrt die View alle Kopfvarianten im Klartext auf und kennzeichnet das Dokument
/// als mehrdeutig (<see cref="FaHierarchyListGroupViewModel.IsAmbiguous"/>); hier wird zusaetzlich
/// eine <see cref="ILogger"/>-Warnung (HauptFA + Anzahl Kopfzeilen) geschrieben — <b>kein</b> SyncLog
/// (Web-Lesefunktion, ADR 0010 gilt nur fuer Hintergrund-Services).</para>
/// </summary>
public class BeschichtungsauftragService
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly FaHierarchyListBuilder _builder;
    private readonly ILogger<BeschichtungsauftragService> _logger;

    public BeschichtungsauftragService(
        IFaHierarchyNodeRepository nodes,
        IFaHierarchyOrderInfoRepository orderInfos,
        FaHierarchyListBuilder builder,
        ILogger<BeschichtungsauftragService> logger)
    {
        _nodes = nodes;
        _orderInfos = orderInfos;
        _builder = builder;
        _logger = logger;
    }

    /// <summary>Klartext-Name des Flags fuer Log-Meldungen.</summary>
    public const string FlagName = "Beschichtet";

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Mengen-Format (3 Nachkommastellen, de-DE) — muss exakt der Zelldarstellung entsprechen.</summary>
    public static string FormatMenge(decimal value) => value.ToString("N3", De);

    /// <summary>Mass-Format (bis 3 Nachkommastellen ohne Nullen, de-DE) fuer Breite/Hoehe/Tiefe.</summary>
    public static string FormatMass(decimal value) => value.ToString("0.###", De);

    /// <summary>
    /// Server-Side-Spaltenfilter-Map (Col-Key -&gt; gerenderter Zelltext). Die Getter MUESSEN exakt
    /// das liefern, was die View in der Zelle rendert (Positionstabelle Teil 4).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> ColumnMap =
        new Dictionary<string, Func<FaHierarchyNode, string?>>
        {
            ["hauptfa"] = n => n.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["hauptartnr"] = n => n.HauptArtnr,
            ["artnr"] = n => n.Artnr,
            ["matchcode"] = n => n.Matchcode,
            ["sollmenge"] = n => FormatMenge(n.Sollmenge),
            ["beschichtet"] = n => n.Beschichtet ? "Ja" : "Nein",
            ["breite"] = n => FormatMass(n.Breite),
            ["hoehe"] = n => FormatMass(n.Hoehe),
            ["tiefe"] = n => FormatMass(n.Tiefe),
        };

    /// <summary>Flag-Praedikat: die gemappte <c>bit</c>-Spalte <c>Beschichtet</c> ist wahr.</summary>
    public static readonly Func<FaHierarchyNode, bool> FlagPredicate = n => n.Beschichtet;

    /// <summary>
    /// Baut den gruppierten Beschichtungsauftrag (Filter <c>Beschichtet == true</c> ueber ALLE
    /// Ebenen). <paramref name="paginate"/> = false liefert die gesamte gefilterte Menge (Druck),
    /// true eine Seite (Bildschirm). Schreibt fuer jede mehrdeutige Gruppe (mehrere FAInfos-Zeilen)
    /// eine <see cref="ILogger"/>-Warnung.
    /// </summary>
    public async Task<FaHierarchyListResult> BuildAsync(
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();

        var result = _builder.Build(
            allNodes, allInfos, FlagPredicate, FlagName,
            columnFilters, ColumnMap, page, pageSize, paginate,
            leafOnly: false, anomalyOnNonLeaf: false);

        LogAmbiguities(result.Groups);
        return result;
    }

    /// <summary>
    /// Schreibt je mehrdeutiger Gruppe (mehrere FAInfos-Zeilen zum selben <c>HauptFA</c>,
    /// Kombinationsgeraet) eine <see cref="ILogger"/>-Warnung mit HauptFA + Anzahl Kopfzeilen.
    /// Bewusst public/static und getrennt testbar.
    /// </summary>
    public void LogAmbiguities(IEnumerable<FaHierarchyListGroupViewModel> groups)
    {
        foreach (var g in groups.Where(g => g.IsAmbiguous))
            LogAmbiguity(g.HauptFA, g.OrderInfos.Count);
    }

    private void LogAmbiguity(int hauptFa, int count) =>
        _logger.LogWarning(
            "Beschichtungsauftrag: HauptFA {HauptFA} hat {Count} Kopfzeilen "
            + "(Kombinationsgeraet) — Dokument ist mehrdeutig, alle Kopfvarianten werden "
            + "aufgefuehrt. Zuordnung Dienstleister/RAL/Termin von Hand pruefen.",
            hauptFa, count);

    // ----- Summiert-Ansicht (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-1, v1.48.0) -----

    /// <summary>
    /// Server-Side-Spaltenfilter-Map der <b>Summiert-Ansicht</b> ueber die Aggregat-Projektion. Die Getter
    /// MUESSEN exakt das liefern, was <c>Summiert.cshtml</c> in der Zelle rendert (der Hinweis
    /// "Masse uneinheitlich" in der Matchcode-Zelle ist Zusatz und bewusst nicht filterbar).
    /// <c>sollmenge</c> filtert die SUMME, <c>positionen</c> die Anzahl.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyBeschichtungSummiertRowViewModel, string?>> SummiertColumnMap =
        new Dictionary<string, Func<FaHierarchyBeschichtungSummiertRowViewModel, string?>>
        {
            ["hauptfa"] = a => a.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["hauptartnr"] = a => a.HauptArtnr,
            ["artnr"] = a => a.Artnr,
            ["matchcode"] = a => a.Matchcode,
            ["breite"] = a => FormatMass(a.Breite),
            ["hoehe"] = a => FormatMass(a.Hoehe),
            ["tiefe"] = a => FormatMass(a.Tiefe),
            ["positionen"] = a => a.PositionCount.ToString(CultureInfo.InvariantCulture),
            ["sollmenge"] = a => FormatMenge(a.SummeSollmenge),
        };

    /// <summary>
    /// Laedt Positionen + Kopfzeilen, delegiert an die DB-freie <see cref="AggregateSummiert"/> und meldet
    /// je Seitenzeile mit uneinheitlichen Massen sowie je mehrdeutigem Kopf eine <see cref="ILogger"/>-Warnung
    /// (Hausregel "melden statt still behandeln").
    /// </summary>
    public async Task<FaHierarchyBeschichtungSummiertResult> BuildSummiertAsync(
        IReadOnlyDictionary<string, string>? columnFilters, int page, int pageSize, bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        var result = AggregateSummiert(allNodes, allInfos, columnFilters, page, pageSize, paginate);

        foreach (var row in result.Rows.Where(r => r.MasseUneinheitlich))
            _logger.LogWarning(
                "Beschichtungsauftrag summiert: HauptFA {HauptFA}, Artnr {Artnr} — Masse uneinheitlich "
                + "({Count} Positionen). Angezeigt werden die Masse der ersten Position; Strukturdaten pruefen.",
                row.HauptFA, row.Artnr, row.PositionCount);
        foreach (var (hauptFa, infos) in result.OrderInfos.Where(kv => kv.Value.Count > 1))
            LogAmbiguity(hauptFa, infos.Count);

        return result;
    }

    /// <summary>
    /// <b>Reine, DB-freie Aggregation der Summiert-Ansicht</b> (Muster
    /// <see cref="KommissionierListenService.AggregateSummiert"/>): Flag <see cref="FlagPredicate"/> ueber alle
    /// Ebenen, Gruppierung je <c>(HauptFA, Artnr)</c> (Antwort 1; leere Artnr = ein Schluessel, wie dort),
    /// Summe nur <c>Sollmenge</c>, Eigenschaften und Masse der ersten Position (<c>Position</c>, dann <c>SubFA</c>),
    /// <see cref="FaHierarchyBeschichtungSummiertRowViewModel.MasseUneinheitlich"/> bei Abweichung in irgendeiner
    /// Dimension (<c>NULL</c> = <c>0</c>, im Cache nicht unterscheidbar), Spaltenfilter auf der Projektion,
    /// Gruppen-Paging je HauptFA. Kopfzeilen werden nur fuer die HauptFA der Seite beigelegt.
    /// </summary>
    public static FaHierarchyBeschichtungSummiertResult AggregateSummiert(
        IEnumerable<FaHierarchyNode> allNodes,
        IEnumerable<FaHierarchyOrderInfo>? allInfos,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var aggregates = allNodes
            .Where(FlagPredicate)
            .GroupBy(n => (n.HauptFA, Artnr: (n.Artnr ?? string.Empty).Trim()))
            .Select(g =>
            {
                var first = g.OrderBy(n => n.Position ?? int.MaxValue).ThenBy(n => n.SubFA).First();
                return new FaHierarchyBeschichtungSummiertRowViewModel
                {
                    HauptFA = g.Key.HauptFA,
                    Artnr = g.Key.Artnr,
                    HauptArtnr = first.HauptArtnr,
                    Matchcode = first.Matchcode,
                    SummeSollmenge = g.Sum(n => n.Sollmenge),
                    PositionCount = g.Count(),
                    Breite = first.Breite,
                    Hoehe = first.Hoehe,
                    Tiefe = first.Tiefe,
                    MasseUneinheitlich = g.Any(n => n.Breite != first.Breite || n.Hoehe != first.Hoehe || n.Tiefe != first.Tiefe),
                };
            })
            .OrderBy(a => a.HauptFA)
            .ThenBy(a => a.Artnr, StringComparer.OrdinalIgnoreCase)
            .AsEnumerable();

        var filtered = ColumnFilterHelper.Apply(aggregates, columnFilters, SummiertColumnMap).ToList();

        // Gruppen-Paging: Seiteneinheit = HauptFA, nie ueber Seiten getrennt.
        var groups = filtered.GroupBy(a => a.HauptFA).ToList();
        var pageRows = (paginate
                ? groups.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize)
                : groups)
            .SelectMany(g => g)
            .ToList();

        var pageHauptFas = pageRows.Select(r => r.HauptFA).ToHashSet();
        var infos = (allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>())
            .Where(i => pageHauptFas.Contains(i.HauptFA))
            .GroupBy(i => i.HauptFA)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new FaHierarchyBeschichtungSummiertResult
        {
            Rows = pageRows,
            TotalRowCount = filtered.Count,
            TotalGroupCount = groups.Count,
            OrderInfos = infos,
        };
    }

    /// <summary>Query-Keys, die der Umschalter Liste/Summiert mitnimmt: die Spaltenfilter der in BEIDEN
    /// Sichten vorhandenen Spalten. Bewusst OHNE <c>colf_sollmenge</c> (Einzel- vs. Summenwert) und ohne
    /// <c>colf_positionen</c>/<c>colf_beschichtet</c> (nur in einer Sicht).</summary>
    public static readonly string[] ToggleSharedQueryKeys =
    [
        "colf_hauptfa", "colf_hauptartnr", "colf_artnr", "colf_matchcode", "colf_breite", "colf_hoehe", "colf_tiefe",
    ];

    /// <summary>Query fuer den Umschalter Liste/Summiert (<see cref="ToggleSharedQueryKeys"/>).</summary>
    public static string BuildToggleQuery(HttpRequest? request) =>
        KommissionierListenService.BuildToggleQuery(request, ToggleSharedQueryKeys);
}
