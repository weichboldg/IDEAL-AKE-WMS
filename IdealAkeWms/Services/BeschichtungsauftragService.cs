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
        {
            _logger.LogWarning(
                "Beschichtungsauftrag: HauptFA {HauptFA} hat {Count} Kopfzeilen "
                + "(Kombinationsgeraet) — Dokument ist mehrdeutig, alle Kopfvarianten werden "
                + "aufgefuehrt. Zuordnung Dienstleister/RAL/Termin von Hand pruefen.",
                g.HauptFA, g.OrderInfos.Count);
        }
    }
}
