using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services;

/// <summary>
/// <b>Teil-5-spezifische Schicht fuer die IDEAL-Vormontage-Listen.</b> Bedient <b>zwei</b> Sichten
/// auf dieselbe Basis (alle Positionen mit gesetztem <c>VMBedarf</c>, unabhaengig von <c>SubFA</c> —
/// Nachtrag 2026-08-13, Aenderung 1):
/// <list type="number">
///   <item><b>Sicht 1 — Einzelne Teile</b>: nutzt den gemeinsamen
///     <see cref="FaHierarchyListBuilder"/> mit <c>leafOnly: false</c> + <c>anomalyOnNonLeaf: false</c>
///     (wie Teil 4) — kein Blattfilter, kein Anomalie-Banner; Fan-out-freier Kopf-Join, Gruppierung +
///     Gruppen-Paging nach <c>HauptFA</c> stammen aus dem Baustein. Teilspezifisch sind nur das
///     Flag-Praedikat (<c>VMBedarf</c> gesetzt, optional auf einen Reiter eingeschraenkt) und die
///     <see cref="ColumnMap"/>.</item>
///   <item><b>Sicht 2 — Summiert</b>: <b>eigene Aggregat-Logik</b> (nicht ueber den Baustein), da
///     eine Matchcode-Aggregatzeile per Definition zu mehreren <c>HauptFA</c> gehoert. Aggregiert
///     je <c>VMBedarf</c>-Reiter nach <b>Matchcode</b> mit zwei getrennten Summen
///     (<c>Sollmenge</c> UND <c>Fertigungmenge</c>); Seiteneinheit ist die Aggregatzeile
///     (begruendete ADR-0005-Ausnahme), Server-Spaltenfilter wirken auf der Aggregat-Projektion.</item>
/// </list>
///
/// <para><b>Reiter-Dimension:</b> <c>VMBedarf</c> ist eine nvarchar-Bereichsbezeichnung (KEIN
/// Sage-Bit — kein <c>-1</c>-Vergleich). Der Filter ist ueberall
/// <c>VMBedarf IS NOT NULL AND VMBedarf &lt;&gt; ''</c>; die Reiter-Liste unterliegt demselben
/// Ausschluss (kein leerer „(kein Bereich)"-Reiter).</para>
///
/// <para><b>Wochenbezug (Nachtrag 2026-08-13, Aenderung 2):</b> KW-Zeitraum-Filter auf
/// <see cref="FaHierarchyOrderInfo.FE_Termin"/> (ISO-Woche Mo–So, ueber
/// <see cref="IsoWeekRange"/>), fan-out-frei ueber die HauptFA-Menge gebunden. In <b>Sicht 2</b>
/// wirkt er VOR der Matchcode-Aggregation; in <b>Sicht 1</b> steht er optional zur konsistenten
/// Bedienbarkeit zur Verfuegung.</para>
///
/// <para><b>Mehrdeutigkeit (Kombigeraet):</b> existieren zu einem <c>HauptFA</c> mehrere FAInfos-
/// Zeilen, fuehrt Sicht 1 alle Kopfvarianten im Klartext auf und kennzeichnet die Gruppe als
/// mehrdeutig (<see cref="FaHierarchyListGroupViewModel.IsAmbiguous"/>); zusaetzlich wird eine
/// <see cref="ILogger"/>-Warnung geschrieben (kein SyncLog — Web-Lesefunktion, ADR 0010 gilt nur
/// fuer Hintergrund-Services).</para>
/// </summary>
public class VormontageService
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly FaHierarchyListBuilder _builder;
    private readonly ILogger<VormontageService> _logger;

    public VormontageService(
        IFaHierarchyNodeRepository nodes,
        IFaHierarchyOrderInfoRepository orderInfos,
        FaHierarchyListBuilder builder,
        ILogger<VormontageService> logger)
    {
        _nodes = nodes;
        _orderInfos = orderInfos;
        _builder = builder;
        _logger = logger;
    }

    /// <summary>Klartext-Name des Flags fuer Anomalie-Log/Banner.</summary>
    public const string FlagName = "VMBedarf";

    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Mengen-Format (3 Nachkommastellen, de-DE) — muss exakt der Zelldarstellung entsprechen.</summary>
    public static string FormatMenge(decimal value) => value.ToString("N3", De);

    /// <summary>
    /// Server-Side-Spaltenfilter-Map fuer <b>Sicht 1</b> (Col-Key -&gt; gerenderter Zelltext). Die
    /// Getter MUESSEN exakt das liefern, was die View in der Zelle rendert.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> ColumnMap =
        new Dictionary<string, Func<FaHierarchyNode, string?>>
        {
            ["hauptfa"] = n => n.HauptFA.ToString(CultureInfo.InvariantCulture),
            ["hauptartnr"] = n => n.HauptArtnr,
            ["artnr"] = n => n.Artnr,
            ["matchcode"] = n => n.Matchcode,
            ["sollmenge"] = n => FormatMenge(n.Sollmenge),
            ["fertigungmenge"] = n => FormatMenge(n.Fertigungmenge),
            ["vmbedarf"] = n => n.VMBedarf,
            ["material"] = n => n.Material,
        };

    /// <summary>
    /// Server-Side-Spaltenfilter-Map fuer <b>Sicht 2</b> ueber die <b>Aggregat-Projektion</b>
    /// (Matchcode + zwei Summenspalten) — bewusst NICHT die Rohpositionen (Spec Teil 5, Fachliche
    /// Anforderung 8). Die Getter MUESSEN exakt das liefern, was die Summiert-View rendert.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyVormontageAggregatViewModel, string?>> AggregatColumnMap =
        new Dictionary<string, Func<FaHierarchyVormontageAggregatViewModel, string?>>
        {
            ["matchcode"] = a => a.Matchcode,
            ["sollmenge"] = a => FormatMenge(a.SummeSollmenge),
            ["fertigungmenge"] = a => FormatMenge(a.SummeFertigungmenge),
        };

    /// <summary>
    /// Flag-Praedikat: gesetztes <c>VMBedarf</c> (<c>IS NOT NULL AND &lt;&gt; ''</c>), optional auf
    /// einen konkreten Reiter (Arbeitsbereich) eingeschraenkt. Wird im Baustein sowohl auf Blaetter
    /// als auch auf Anomalie-Zeilen angewandt, damit eine Reiter-Einschraenkung in Liste und Banner
    /// konsistent wirkt.
    /// </summary>
    public static Func<FaHierarchyNode, bool> BuildFlagPredicate(string? bereich)
    {
        if (string.IsNullOrWhiteSpace(bereich))
            return n => !string.IsNullOrWhiteSpace(n.VMBedarf);

        var target = bereich.Trim();
        return n => !string.IsNullOrWhiteSpace(n.VMBedarf)
                    && string.Equals(n.VMBedarf!.Trim(), target, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Distinct-Liste der am Datenbestand vorkommenden <c>VMBedarf</c>-Arbeitsbereiche, alphabetisch
    /// — Quelle der Reiter. Derselbe NULL/Leer-Ausschluss wie der Filter, damit kein leerer Reiter
    /// entsteht (AK 1).
    ///
    /// <para><b>Nachtrag 2026-08-13, Aenderung 1:</b> Die fruehere <c>SubFA = 0</c>-Einschraenkung
    /// entfaellt — <c>VMBedarf</c>-Werte, die nur auf Baugruppen-Ebene (<c>SubFA != 0</c>) vorkommen,
    /// erzeugen jetzt ebenfalls einen Reiter (Positionen werden unabhaengig von <c>SubFA</c> gelistet).</para>
    /// </summary>
    public async Task<List<string>> GetBereicheAsync()
    {
        var allNodes = await _nodes.GetAllAsync();
        return allNodes
            .Where(n => !string.IsNullOrWhiteSpace(n.VMBedarf))
            .Select(n => n.VMBedarf!.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <b>Sicht 1 — Einzelne Teile.</b> Baut die gruppierte Positionsliste ueber den gemeinsamen
    /// Baustein, optional auf einen Reiter eingeschraenkt. Schreibt fuer jede mehrdeutige Gruppe eine
    /// <see cref="ILogger"/>-Warnung.
    ///
    /// <para><b>Nachtrag 2026-08-13, Aenderung 1:</b> Aufruf mit
    /// <c>leafOnly: false, anomalyOnNonLeaf: false</c> (wie Teil 4) — ALLE Positionen mit gesetztem
    /// <c>VMBedarf</c>, unabhaengig von <c>SubFA</c>; kein Blattfilter, kein Anomalie-Banner mehr.</para>
    /// <para><b>Aenderung 2:</b> Optionaler KW-Vorfilter auf <see cref="FaHierarchyOrderInfo.FE_Termin"/>
    /// (fan-out-frei ueber die HauptFA-Menge) — in Sicht 1 nur zur konsistenten Bedienbarkeit.</para>
    /// </summary>
    public async Task<FaHierarchyListResult> BuildEinzelteileAsync(
        string? bereich,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        var predicate = BuildFlagPredicate(bereich);

        // (Aenderung 2) KW-Vorfilter auf FE_Termin: nur Positionen der HauptFAs in der KW.
        IEnumerable<FaHierarchyNode> nodes = allNodes;
        if (kwRange is not null)
        {
            var included = IsoWeekRange.HauptFasInRange(allInfos, o => o.FE_Termin, kwRange.Value);
            nodes = allNodes.Where(n => included.Contains(n.HauptFA));
        }

        var result = _builder.Build(
            nodes, allInfos, predicate, FlagName,
            columnFilters, ColumnMap, page, pageSize, paginate,
            leafOnly: false, anomalyOnNonLeaf: false);

        LogAmbiguities(result.Groups);
        return result;
    }

    /// <summary>
    /// <b>Sicht 2 — Summiert.</b> Laedt die Positionen + Kopfzeilen und delegiert an die reine,
    /// DB-freie <see cref="AggregateSummiert"/>.
    /// </summary>
    public async Task<FaHierarchyVormontageAggregatResult> BuildSummiertAsync(
        string? bereich,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        return AggregateSummiert(allNodes, allInfos, bereich, kwRange, columnFilters, page, pageSize, paginate);
    }

    /// <summary>
    /// <b>Reine, DB-freie Aggregations-Mechanik der Summierten Sicht</b> (unit-testbar). Reihenfolge
    /// nach Spec Teil 5, Loesungsentwurf Sicht 2 + Nachtrag 2026-08-13:
    /// <list type="number">
    ///   <item>Basis: gesetztes <c>VMBedarf</c> + aktueller Reiter — <b>unabhaengig von <c>SubFA</c></b>
    ///     (Aenderung 1).</item>
    ///   <item><b>KW-Vorfilter (Aenderung 2):</b> nur Positionen der HauptFAs, deren
    ///     <see cref="FaHierarchyOrderInfo.FE_Termin"/> in der ISO-Woche (Mo–So) liegt — VOR der
    ///     Aggregation, fan-out-frei; <c>HauptFA</c> selbst bleibt Aggregations-neutral (die Summe
    ///     laeuft ueber alle HauptFA des Reiters).</item>
    ///   <item>Gruppieren nach <c>Matchcode</c>; <c>Sollmenge</c> UND <c>Fertigungmenge</c> je Gruppe summieren.</item>
    ///   <item>Server-Spaltenfilter auf der Aggregat-Projektion (Matchcode + zwei Summen).</item>
    ///   <item>Paging auf Aggregatzeilen-Ebene; <c>TotalRowCount</c> zaehlt Aggregatzeilen NACH Filter.</item>
    /// </list>
    /// Der Matchcode ist der Aggregations-Schluessel (nicht die Artnr) — gleicher Artnr mit
    /// verschiedenen Matchcodes ergibt bewusst mehrere Zeilen.
    /// </summary>
    public static FaHierarchyVormontageAggregatResult AggregateSummiert(
        IEnumerable<FaHierarchyNode> allNodes,
        IEnumerable<FaHierarchyOrderInfo> allInfos,
        string? bereich,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        var predicate = BuildFlagPredicate(bereich);

        // (1) Basis: gesetztes VMBedarf (+ Reiter) — unabhaengig von SubFA (Aenderung 1).
        var leaves = allNodes.Where(n => predicate(n));

        // (2) KW-Vorfilter auf FE_Termin (fan-out-frei ueber die HauptFA-Menge), VOR der Aggregation.
        if (kwRange is not null)
        {
            var included = IsoWeekRange.HauptFasInRange(
                allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>(),
                o => o.FE_Termin,
                kwRange.Value);
            leaves = leaves.Where(n => included.Contains(n.HauptFA));
        }

        // (3) Aggregation je Matchcode (Schluessel; Trim, damit " X " und "X" nicht auseinanderfallen;
        //     ordinal, damit unterschiedliche Matchcodes bewusst getrennte Zeilen bleiben).
        var aggregates = leaves
            .GroupBy(n => (n.Matchcode ?? string.Empty).Trim(), StringComparer.Ordinal)
            .Select(g => new FaHierarchyVormontageAggregatViewModel
            {
                Matchcode = g.Key,
                VMBedarf = bereich,
                SummeSollmenge = g.Sum(n => n.Sollmenge),
                SummeFertigungmenge = g.Sum(n => n.Fertigungmenge),
                PositionCount = g.Count()
            })
            .OrderBy(a => a.Matchcode, StringComparer.OrdinalIgnoreCase)
            .AsEnumerable();

        // (4) Server-Spaltenfilter auf der Aggregat-Projektion (nicht auf den Rohpositionen).
        aggregates = ColumnFilterHelper.Apply(aggregates, columnFilters, AggregatColumnMap);

        var filtered = aggregates.ToList();
        var totalRowCount = filtered.Count;

        // (5) Paging auf Aggregatzeilen-Ebene.
        var pageRows = paginate
            ? filtered.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize).ToList()
            : filtered;

        return new FaHierarchyVormontageAggregatResult
        {
            Rows = pageRows,
            TotalRowCount = totalRowCount
        };
    }

    /// <summary>
    /// Schreibt je mehrdeutiger Gruppe (mehrere FAInfos-Zeilen zum selben <c>HauptFA</c>,
    /// Kombigeraet) eine <see cref="ILogger"/>-Warnung mit HauptFA + Anzahl Kopfzeilen.
    /// </summary>
    public void LogAmbiguities(IEnumerable<FaHierarchyListGroupViewModel> groups)
    {
        foreach (var g in groups.Where(g => g.IsAmbiguous))
        {
            _logger.LogWarning(
                "Vormontage-Liste: HauptFA {HauptFA} hat {Count} Kopfzeilen (Kombigeraet) — "
                + "Dokument ist mehrdeutig, alle Kopfvarianten werden aufgefuehrt. "
                + "Montage-Abteilung/Termine von Hand pruefen.",
                g.HauptFA, g.OrderInfos.Count);
        }
    }
}
