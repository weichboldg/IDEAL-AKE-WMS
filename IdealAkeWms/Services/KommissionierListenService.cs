using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

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

    /// <summary>Spaltenschluessel-Liste der Liste (Drift-Waechter); die Werte der Kopf-Spalten liefert erst
    /// <see cref="BuildColumnMap"/> mit den Daten der Anfrage.</summary>
    public static readonly IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> ColumnMap =
        BuildColumnMap(new Dictionary<int, string?>());

    /// <summary>v1.99.43 (A3): Montage-Abteilung(en) je HauptFA aus den Kopfzeilen — alle verschiedenen, kommagetrennt
    /// (<see cref="VormontageService.FormatTexte"/>), bewusst NICHT <see cref="VormontageService.FormatAbteilung"/> („Kombigerät“).</summary>
    public static IReadOnlyDictionary<int, string?> AbteilungJeHauptFa(IEnumerable<FaHierarchyOrderInfo>? infos) =>
        (infos ?? Enumerable.Empty<FaHierarchyOrderInfo>()).GroupBy(i => i.HauptFA)
            .ToDictionary(g => g.Key, g => VormontageService.FormatTexte(g.Select(i => i.MontageAbteilung)));

    /// <summary>
    /// Server-Side-Spaltenfilter-Map (Col-Key -&gt; gerenderter Zelltext). Die Getter MUESSEN exakt
    /// das liefern, was die View in der Zelle rendert. <c>SubFA</c> wird nicht als Spalte gerendert
    /// (kein Filter); <c>HauptArtnr</c> ist gemaess Anhang-Spaltenliste als Positionsspalte enthalten.
    /// v1.99.43: je Anfrage gebaut, weil „Abteilung“ aus den Kopfzeilen kommt (Filter wirkt vor dem Paging).
    /// </summary>
    public static IReadOnlyDictionary<string, Func<FaHierarchyNode, string?>> BuildColumnMap(IReadOnlyDictionary<int, string?> abteilung) =>
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
            ["montage-abteilung"] = n => abteilung.GetValueOrDefault(n.HauptFA),
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
            ["matchcode"] = a => a.Matchcode,
            ["hauptlagerplatz"] = a => a.Hauptlagerplatz,
            ["montage-abteilung"] = a => a.Abteilung,
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
        IReadOnlyCollection<string>? ziele,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        var predicate = BuildFlagPredicate(ziele);

        return _builder.Build(
            allNodes, allInfos, predicate, FlagName,
            columnFilters, BuildColumnMap(AbteilungJeHauptFa(allInfos)), page, pageSize, paginate,
            leafOnly: false, anomalyOnNonLeaf: false,
            columnTypes: ColumnFilterTypes.For("FaHierarchyKommissionierListen/Index"));
    }

    /// <summary>
    /// <b>Summiert-Ansicht</b> (Nachtrag 2026-08-13, Aenderung 2): Laedt Positionen + Kopfzeilen und
    /// delegiert an die reine, DB-freie <see cref="AggregateSummiert"/>.
    /// </summary>
    public async Task<FaHierarchyKommissionierSummiertResult> BuildSummiertAsync(
        IReadOnlyCollection<string>? ziele,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        var allNodes = await _nodes.GetAllAsync();
        var allInfos = await _orderInfos.GetAllAsync();
        return AggregateSummiert(allNodes, allInfos, ziele, kwRange, columnFilters, page, pageSize, paginate);
    }

    /// <summary>
    /// <b>Reine, DB-freie Aggregations-Mechanik der Summiert-Ansicht</b> (unit-testbar). Reihenfolge:
    /// <list type="number">
    ///   <item><b>KW-Vorfilter (optional):</b> Ohne KW-Bereich werden ALLE HauptFA einbezogen — keine
    ///     implizite Einschraenkung mehr (Antwort 4, Spec 2026-09-25-kommissionierliste-summierung-pdf,
    ///     ersetzt N2d).</item>
    ///   <item>Ist eine KW gesetzt, waehlt sie die einzubeziehenden <c>HauptFA</c> ueber deren
    ///     <see cref="FaHierarchyOrderInfo.KO_Termin"/> (Konstruktions-Termin, unveraendert, Mo–So,
    ///     fan-out-frei).</item>
    ///   <item>Positionen mit gesetztem <c>Kommissionieren</c> (optional Ziel) innerhalb dieser
    ///     HauptFA-Menge — <b>unabhaengig von <c>SubFA</c></b> (Aenderung 1).</item>
    ///   <item>Aggregation je <c>(HauptFA, Artnr, Kommissionieren)</c>, Summe nur <c>Sollmenge</c>;
    ///     Matchcode/Hauptlagerplatz werden als Artikeleigenschaften (je Summenschluessel konstant)
    ///     mitgefuehrt.</item>
    ///   <item>Server-Spaltenfilter auf der Aggregat-Projektion; danach Gruppen-Paging je HauptFA
    ///     (Muster <see cref="FaHierarchyListBuilder.Build"/>).</item>
    /// </list>
    /// </summary>
    public static FaHierarchyKommissionierSummiertResult AggregateSummiert(
        IEnumerable<FaHierarchyNode> allNodes,
        IEnumerable<FaHierarchyOrderInfo> allInfos,
        IReadOnlyCollection<string>? ziele,
        (DateTime StartInclusive, DateTime EndExclusive)? kwRange,
        IReadOnlyDictionary<string, string>? columnFilters,
        int page,
        int pageSize,
        bool paginate)
    {
        ArgumentNullException.ThrowIfNull(allNodes);

        // (1) KW waehlt die HauptFA optional ueber KO_Termin. null = keine Einschraenkung
        //     (Antwort 4, Spec 2026-09-25-kommissionierliste-summierung-pdf — ersetzt N2d).
        var includedHauptFas = kwRange is null
            ? null
            : IsoWeekRange.HauptFasInRange(
                allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>(), o => o.KO_Termin, kwRange.Value);

        var abteilung = AbteilungJeHauptFa(allInfos);
        var predicate = BuildFlagPredicate(ziele);
        var relevant = allNodes.Where(n => (includedHauptFas is null || includedHauptFas.Contains(n.HauptFA)) && predicate(n));

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
                PositionCount = g.Count(),
                Verschwunden = g.FirstOrDefault(n => n.VerschwundenSeit != null),
                // Artikeleigenschaften, je Summenschluessel konstant — Schluessel unveraendert.
                Matchcode = g.First().Matchcode,
                Hauptlagerplatz = g.First().Hauptlagerplatz,
                Abteilung = abteilung.GetValueOrDefault(g.Key.HauptFA),
            })
            .OrderBy(a => a.HauptFA)
            .ThenBy(a => a.Artnr, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Kommissionieren, StringComparer.OrdinalIgnoreCase)
            .AsEnumerable();

        // (5) Server-Spaltenfilter auf der Aggregat-Projektion.
        aggregates = ColumnFilterHelper.Apply(aggregates, columnFilters, SummiertColumnMap, ColumnFilterTypes.For("FaHierarchyKommissionierListen/Summiert"));

        var filtered = aggregates.ToList();
        var totalRowCount = filtered.Count;

        // (6) Gruppen-Paging: Seiteneinheit = HauptFA (Muster FaHierarchyListBuilder.Build), nie ueber Seiten getrennt.
        var groups = filtered.GroupBy(a => a.HauptFA).ToList(); // Reihenfolge bleibt HauptFA-sortiert
        var pageRows = (paginate
                ? groups.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize)
                : groups)
            .SelectMany(g => g)
            .ToList();

        return new FaHierarchyKommissionierSummiertResult
        {
            Rows = pageRows,
            TotalRowCount = totalRowCount,
            TotalGroupCount = groups.Count
        };
    }

    /// <summary>Query-Keys, die der Umschalter Liste&lt;-&gt;Summiert mitnimmt (Spec 2026-09-25-kommissionierliste-
    /// summierung-pdf, Abschnitt E): Ziel, KW (in der Liste wirkungslos, nur Rueckweg — Freigabe-Antwort 5) und die
    /// Spaltenfilter der in BEIDEN Ansichten vorhandenen Spalten. Bewusst OHNE colf_sollmenge: dort Einzelmenge,
    /// hier Summe — derselbe Wert wuerde still umgedeutet.</summary>
    public static readonly string[] ToggleSharedQueryKeys =
    [
        "target", "kwVon", "kwBis",
        "colf_hauptfa", "colf_artnr", "colf_matchcode", "colf_kommissionieren", "colf_hauptlagerplatz",
    ];

    /// <summary>Baut die Query fuer den Umschalter Liste&lt;-&gt;Summiert aus den geteilten Keys
    /// (<see cref="ToggleSharedQueryKeys"/>). Liefert <c>""</c> ohne passende Query-Werte.</summary>
    public static string BuildToggleQuery(HttpRequest? request) => BuildToggleQuery(request, ToggleSharedQueryKeys);

    /// <summary>Baut eine Query aus den angegebenen Keys der aktuellen Anfrage (geteilt mit dem
    /// Beschichtungsauftrag, <see cref="BeschichtungsauftragService.BuildToggleQuery"/>).</summary>
    public static string BuildToggleQuery(HttpRequest? request, IEnumerable<string> keys)
    {
        if (request == null) return string.Empty;
        var qb = new QueryBuilder();
        foreach (var key in keys)
            foreach (var value in request.Query[key]) // v1.99.43: wiederholter Key (target=K-01&target=K-02) bleibt mehrwertig
                if (!string.IsNullOrEmpty(value)) qb.Add(key, value);
        return qb.ToQueryString().ToString();
    }

    /// <summary>„Alle Ziele“ in Query und Gedaechtnis (A1); in der UI „— alle Ziele —“.</summary>
    public const string AlleZiele = "*";

    /// <summary>v1.99.43 (A1): Zielmenge normalisieren — getrimmt, ohne Leere, distinct; leer oder mit „*“ = alle (<c>["*"]</c>).</summary>
    public static List<string> ZieleNormalisieren(IEnumerable<string?>? werte)
    {
        var ziele = (werte ?? Enumerable.Empty<string?>()).Select(w => w?.Trim() ?? "").Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return ziele.Count == 0 || ziele.Contains(AlleZiele) ? new List<string> { AlleZiele } : ziele;
    }

    /// <summary>true, wenn die Menge wirklich einschraenkt (nicht leer, nicht „alle“).</summary>
    public static bool SchraenktEin(IReadOnlyCollection<string>? ziele) =>
        ziele is { Count: > 0 } && !ziele.Contains(AlleZiele);

    /// <summary>v1.99.43 (A1): aktuelle Query mit ersetztem <c>target</c> (je Ziel ein Eintrag) — fuer Druck/PDF-Links, damit sie die
    /// wirksamen Ziele tragen, auch wenn sie aus Gedaechtnis oder Einstellung kommen (Druck/PDF lesen nur die Query).</summary>
    public static string QueryMitZielen(HttpRequest? request, IEnumerable<string> ziele)
    {
        var qb = new QueryBuilder();
        foreach (var z in ziele) qb.Add("target", z);
        if (request != null)
            foreach (var (key, values) in request.Query.Where(q => q.Key != "target"))
                foreach (var v in values) qb.Add(key, v ?? "");
        return qb.ToQueryString().ToString();
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
    /// Flag-Praedikat: gesetztes <c>Kommissionieren</c>, optional auf eine Zielmenge eingeschraenkt (v1.99.43, A1: mehrwertig;
    /// null, leer oder „*“ = alle). Wird im Baustein sowohl auf Blaetter als auch auf Anomalie-Zeilen angewandt,
    /// damit eine Ziel-Einschraenkung in Liste und Banner konsistent wirkt.
    /// </summary>
    public static Func<FaHierarchyNode, bool> BuildFlagPredicate(IReadOnlyCollection<string>? ziele)
    {
        if (!SchraenktEin(ziele))
            return n => KommissionierRelevanzFilter.IsRelevant(n.Kommissionieren);

        var menge = ziele!.Select(z => z.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return n => KommissionierRelevanzFilter.IsRelevant(n.Kommissionieren) && menge.Contains(n.Kommissionieren!.Trim());
    }
}
