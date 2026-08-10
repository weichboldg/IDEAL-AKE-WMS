using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

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
    /// das liefern, was die View in der Zelle rendert. <c>SubFA</c> fehlt bewusst (durch
    /// <c>SubFA = 0</c> konstant, als Filter sinnlos); <c>HauptArtnr</c> ist gemaess Anhang-
    /// Spaltenliste als Positionsspalte enthalten.
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
    /// Baut die gruppierte Kommissionierliste. <paramref name="paginate"/> = false liefert die
    /// gesamte gefilterte Menge (Druck), true eine Seite (Bildschirm).
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
            columnFilters, ColumnMap, page, pageSize, paginate);
    }

    /// <summary>
    /// Distinct-Liste der am Datenbestand vorkommenden Kommissionier-Ziele (nur Blattpositionen),
    /// alphabetisch — Quelle des Filter-Dropdowns (Fachliche Anforderung 1).
    /// </summary>
    public async Task<List<string>> GetTargetValuesAsync()
    {
        var allNodes = await _nodes.GetAllAsync();
        return allNodes
            .Where(n => n.SubFA == 0 && !string.IsNullOrWhiteSpace(n.Kommissionieren))
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
            return n => !string.IsNullOrWhiteSpace(n.Kommissionieren);

        var target = targetValue.Trim();
        return n => !string.IsNullOrWhiteSpace(n.Kommissionieren)
                    && string.Equals(n.Kommissionieren!.Trim(), target, StringComparison.OrdinalIgnoreCase);
    }
}
