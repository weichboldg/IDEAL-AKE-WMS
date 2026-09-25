using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// <b>Teil-3-spezifische, duenne Schicht ueber dem gemeinsamen <see cref="FaHierarchyListBuilder"/></b>
/// (IDEAL Kommissionierlisten). Steuert nur das Teilspezifische bei:
/// <list type="bullet">
///   <item>das <b>Flag-Praedikat</b> <c>Kommissionieren</c> (optional auf ein konkretes Ziel
///     eingeschraenkt),</item>
///   <item>die <b>ColumnMap</b> fuer den Server-Spaltenfilter (Anhang-Spaltenliste),</item>
///   <item>die Liste der vorkommenden <c>Kommissionieren</c>-Ziele fuer das Filter-Dropdown.</item>
/// </list>
/// Die gesamte Mechanik (Blattfilter, Anomalie, Header-Join, Gruppen-Paging) bleibt im Baustein.
///
/// <para><b>Erweiterungs-Naht Teil 4/5:</b> <c>BeschichtungService</c> bzw. <c>VormontageService</c>
/// entstehen 1:1 nach diesem Vorbild — nur Flag-Praedikat (<c>Beschichtet == true</c> /
/// <c>VMBedarf</c>), ColumnMap und Dropdown-Werte wechseln; <see cref="FaHierarchyListBuilder.Build"/>
/// wird unveraendert wiederverwendet.</para>
/// </summary>
public class KommissionierListenService
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly FaHierarchyListBuilder _builder;

    public KommissionierListenService(
        IFaHierarchyNodeRepository nodes,
        IFaHierarchyOrderInfoRepository orderInfos,
        FaHierarchyListBuilder builder)
    {
        _nodes = nodes;
        _orderInfos = orderInfos;
        _builder = builder;
    }

    /// <summary>Klartext-Name des Flags fuer Anomalie-Log/Banner.</summary>
    public const string FlagName = "Kommissionieren";

    /// <summary>
    /// Server-Side-Spaltenfilter-Map (Col-Key -&gt; gerenderter Zelltext). Die Getter MUESSEN exakt
    /// das liefern, was die View in der Zelle rendert. <c>SubFA</c> wird nicht als Spalte gerendert
    /// (kein Filter); <c>HauptArtnr</c> ist gemaess Anhang-Spaltenliste als Positionsspalte enthalten.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> ColumnMap =
        new Dictionary<string, Func<FaHierarchyNode, string?>>
        {
            ["hauptfa"] = n => n.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["hauptartnr"] = n => n.HauptArtnr,
            ["artnr"] = n => n.Artnr,
            ["matchcode"] = n => n.Matchcode,
            ["sollmenge"] = n => n.Sollmenge.ToString("N3", CultureInfo.GetCultureInfo("de-DE")),
            ["hauptlagerplatz"] = n => n.Hauptlagerplatz,
            ["kommissionieren"] = n => n.Kommissionieren,
            ["arbeitsbereich"] = n => n.Arbeitsbereich,
            ["artikeltyp"] = n => n.Artikeltyp,
            ["beschichtet"] = n => n.Beschichtet ? "Ja" : "Nein",
            ["material"] = n => n.Material,
        };

    /// <summary>
    /// Server-Side-Spaltenfilter-Map der <b>Summiert-Ansicht</b> ueber die Aggregat-Projektion
    /// (HauptFA + Artnr + Ziel + Sollmenge-Summe) — bewusst NICHT die Rohpositionen. Die Getter
    /// MUESSEN exakt das liefern, was die Summiert-View rendert.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyKommissionierSummiertRowViewModel, string?>> SummiertColumnMap =
        new Dictionary<string, Func<FaHierarchyKommissionierSummiertRowViewModel, string?>>
        {
            ["hauptfa"] = a => a.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["artnr"] = a => a.Artnr,
            ["kommissionieren"] = a => a.Kommissionieren,
            ["sollmenge"] = a => a.SummeSollmenge.ToString("N3", CultureInfo.GetCultureInfo("de-DE")),
        };

    /// <summary>
    /// Baut die gruppierte Kommissionierliste. <paramref name="paginate"/> = false liefert die
    /// gesamte gefilterte Menge (Druck), true eine Seite (Bildschirm).
    ///
    /// <para><b>Nachtrag 2026-08-13, Aenderung 1:</b> Der Baustein wird mit
    /// <c>leafOnly: false, anomalyOnNonLeaf: false</c> aufgerufen (wie Teil 4/Beschichtung) —
    /// ALLE Positionen mit gesetztem <c>Kommissionieren</c> werden gelistet, unabhaengig von
    /// <c>SubFA</c>. Kein Blattfilter, kein Anomalie-Banner mehr.</para>
    /// </summary>
    public async Task<FaHierarchyListResult> BuildAsync(
        string? targetValue,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        var predicate = BuildFlagPredicate(targetValue);

        return _builder.Build(
            allNodes, allInfos, predicate, FlagName,
            columnFilters, ColumnMap, page, pageSize, paginate,
            leafOnly: false, anomalyOnNonLeaf: false);
    }

    /// <summary>
    /// <b>Summiert-Ansicht</b> (Nachtrag 2026-08-13, Aenderung 2): Laedt Positionen + Kopfzeilen und
    /// delegiert an die reine, DB-freie <see cref="AggregateSummiert"/>.
    /// </summary>
    public async Task<FaHierarchyKommissionierSummiertResult> BuildSummiertAsync(
        string? targetValue,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        return AggregateSummiert(allNodes, allInfos, targetValue, kwRange, columnFilters, page, pageSize, paginate);
    }

    /// <summary>
    /// <b>Reine, DB-freie Aggregations-Mechanik der Summiert-Ansicht</b> (unit-testbar). Reihenfolge:
    /// <list type="number">
    ///   <item><b>KW-Vorfilter (Pflicht):</b> Ohne KW-Bereich liefert die Methode ein leeres Ergebnis
    ///     — es gibt <b>keine</b> implizite Gesamtsumme ueber alle HauptFA (Nachtrag AK N2d).</item>
    ///   <item>Die KW waehlt die einzubeziehenden <c>HauptFA</c> ueber deren
    ///     <see cref="FaHierarchyOrderInfo.KO_Termin"/> (Konstruktions-Termin, unveraendert, Mo–So,
    ///     fan-out-frei).</item>
    ///   <item>Positionen mit gesetztem <c>Kommissionieren</c> (optional Ziel) innerhalb dieser
    ///     HauptFA-Menge — <b>unabhaengig von <c>SubFA</c></b> (Aenderung 1).</item>
    ///   <item>Aggregation je <c>(HauptFA, Artnr, Kommissionieren)</c>, Summe nur <c>Sollmenge</c>.</item>
    ///   <item>Server-Spaltenfilter auf der Aggregat-Projektion; Paging auf Aggregatzeilen-Ebene.</item>
    /// </list>
    /// </summary>
    public static FaHierarchyKommissionierSummiertResult AggregateSummiert(
        IEnumerable<FaHierarchyNode> allNodes,
        IEnumerable<FaHierarchyOrderInfo> allInfos,
        string? targetValue,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        // (1) Ohne KW: keine implizite Gesamtsumme (N2d) — leeres Ergebnis.
        if (kwRange is null)
            return new FaHierarchyKommissionierSummiertResult();

        // (2) KW waehlt die HauptFA ueber KO_Termin (fan-out-frei ueber die Kopf-Zeilen).
        var includedHauptFas = IsoWeekRange.HauptFasInRange(
            allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>(),
            o => o.KO_Termin,
            kwRange.Value);

        var predicate = BuildFlagPredicate(targetValue);

        // (3) Positionen mit gesetztem Kommissionieren (+ Ziel) in der KW-HauptFA-Menge — unabhaengig
        //     von SubFA (Aenderung 1).
        var relevant = allNodes.Where(n => includedHauptFas.Contains(n.HauptFA) && predicate(n));

        // (4) Aggregation je (HauptFA, Artnr, Ziel); Summe nur Sollmenge.
        var aggregates = relevant
            .GroupBy(n => (
                n.HauptFA,
                Artnr: (n.Artnr ?? string.Empty).Trim(),
                Ziel: (n.Kommissionieren ?? string.Empty).Trim()))
            .Select(g => new FaHierarchyKommissionierSummiertRowViewModel
            {
                HauptFA = g.Key.HauptFA,
                Artnr = g.Key.Artnr,
                Kommissionieren = g.Key.Ziel,
                SummeSollmenge = g.Sum(n => n.Sollmenge),
                PositionCount = g.Count()
            })
            .OrderBy(a => a.HauptFA)
            .ThenBy(a => a.Artnr, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Kommissionieren, StringComparer.OrdinalIgnoreCase)
            .AsEnumerable();

        // (5) Server-Spaltenfilter auf der Aggregat-Projektion.
        aggregates = ColumnFilterHelper.Apply(aggregates, columnFilters, SummiertColumnMap);

        var filtered = aggregates.ToList();
        var totalRowCount = filtered.Count;

        var pageRows = paginate
            ? filtered.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize).ToList()
            : filtered;

        return new FaHierarchyKommissionierSummiertResult
        {
            Rows = pageRows,
            TotalRowCount = totalRowCount
        };
    }

    /// <summary>
    /// Distinct-Liste der am Datenbestand vorkommenden Kommissionier-Ziele, alphabetisch — Quelle des
    /// Filter-Dropdowns (Fachliche Anforderung 1).
    ///
    /// <para><b>Nachtrag 2026-08-13, Aenderung 1 (N1d):</b> Die fruehere <c>SubFA = 0</c>-Einschraenkung
    /// entfaellt — sonst fehlten Ziel-Werte, die nur auf Baugruppen-Ebene (<c>SubFA != 0</c>)
    /// vorkommen, weiterhin im Dropdown.</para>
    /// </summary>
    public async Task<List<string>> GetTargetValuesAsync()
    {
        var allNodes = await _nodes.GetAllAsync();
        return allNodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Kommissionieren))
            .Select(n => n.Kommissionieren!.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Flag-Praedikat: gesetztes <c>Kommissionieren</c>, optional auf einen konkreten Ziel-Wert
    /// eingeschraenkt. Wird im Baustein sowohl auf Blaetter als auch auf Anomalie-Zeilen angewandt,
    /// damit eine Ziel-Einschraenkung in Liste und Banner konsistent wirkt.
    /// </summary>
    public static Func<FaHierarchyNode, bool> BuildFlagPredicate(string? targetValue)
    {
        if (string.IsNullOrWhiteSpace(targetValue))
            return n => KommissionierRelevanzFilter.IsRelevant(n.Kommissionieren);

        var target = targetValue.Trim();
        return n => KommissionierRelevanzFilter.IsRelevant(n.Kommissionieren)
                    && string.Equals(n.Kommissionieren!.Trim(), target, StringComparison.OrdinalIgnoreCase);
    }
}
