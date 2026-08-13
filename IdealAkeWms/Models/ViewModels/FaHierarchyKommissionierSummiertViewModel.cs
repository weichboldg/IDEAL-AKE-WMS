using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Eine Aggregatzeile der IDEAL-Kommissionier-<b>Summiert-Ansicht</b> (Teil 3, Nachtrag 2026-08-13,
/// Aenderung 2). Analog zur Vormontage-Sicht 2, aber mit <b>eigenen</b> Regeln: aggregiert wird
/// <b>je <see cref="HauptFA"/></b>, gruppiert nach <b>Artikelnummer (<see cref="Artnr"/>)</b>,
/// summiert wird <b>nur die <see cref="SummeSollmenge"/></b> (kein Fertigungmenge-Pendant),
/// <b>je Kommissionierziel (<see cref="Kommissionieren"/>) getrennt</b>. Eine Aggregatzeile ist
/// damit eindeutig durch <c>(HauptFA, Artnr, Kommissionieren)</c> bestimmt.
/// </summary>
public class FaHierarchyKommissionierSummiertRowViewModel
{
    /// <summary>Der HauptFA, auf den diese Aggregatzeile bezogen ist (Aggregation je HauptFA).</summary>
    public int HauptFA { get; init; }

    /// <summary>Aggregations-Schluessel innerhalb des HauptFA: die Artikelnummer.</summary>
    public string? Artnr { get; init; }

    /// <summary>Kommissionierziel (<c>Kommissionieren</c>-Wert) — je Ziel eine getrennte Zeile.</summary>
    public string? Kommissionieren { get; init; }

    /// <summary>Summe der <c>Sollmenge</c> ueber alle Positionen dieses (HauptFA, Artnr, Ziel).</summary>
    public decimal SummeSollmenge { get; init; }

    /// <summary>Anzahl der aggregierten Positionen (informativ).</summary>
    public int PositionCount { get; init; }
}

/// <summary>
/// Ergebnis einer Kommissionier-Summiert-Aggregation: die (paginierten) Aggregatzeilen plus die
/// Gesamtzahl VOR dem Paging (fuer <c>PaginationState.TotalCount</c>). <b>Seiteneinheit ist die
/// Aggregatzeile</b> (begruendete ADR-0005-Ausnahme, analog Vormontage-Sicht 2).
/// </summary>
public class FaHierarchyKommissionierSummiertResult
{
    public List<FaHierarchyKommissionierSummiertRowViewModel> Rows { get; init; } = new();

    /// <summary>Anzahl aller Aggregatzeilen (Seiteneinheit) NACH Spaltenfilter, VOR dem Paging.</summary>
    public int TotalRowCount { get; init; }
}

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Kommissionier-<b>Summiert-Ansicht</b> (Teil 3, Nachtrag
/// 2026-08-13). Flache Liste von <c>(HauptFA, Artnr, Ziel)</c>-Aggregatzeilen mit der
/// <c>Sollmenge</c>-Summe, plus Filterkarte (KW-Zeitraum + Ziel-Dropdown) und Paging-State
/// (Seiteneinheit = Aggregatzeile).
/// </summary>
public class FaHierarchyKommissionierSummiertViewModel
{
    public List<FaHierarchyKommissionierSummiertRowViewModel> Rows { get; init; } = new();

    /// <summary>Paging-State (Seiteneinheit = Aggregatzeile; <c>TotalCount</c> zaehlt Aggregatzeilen).</summary>
    public PaginationState Pagination { get; init; } = new();

    /// <summary>Vorkommende Kommissionier-Ziele fuer das Filter-Dropdown.</summary>
    public IReadOnlyList<string> AvailableTargets { get; init; } = new List<string>();

    /// <summary>Aktuell gewaehltes Ziel (oder null = alle).</summary>
    public string? SelectedTarget { get; init; }

    /// <summary>KW-von-Eingabe (Format <c>YYYY-Www</c>, HTML <c>&lt;input type="week"&gt;</c>).</summary>
    public string? KwVon { get; init; }

    /// <summary>KW-bis-Eingabe (Format <c>YYYY-Www</c>).</summary>
    public string? KwBis { get; init; }

    /// <summary>
    /// <c>true</c>, sobald mindestens eine gueltige KW-Grenze gesetzt ist. Ohne KW zeigt die Ansicht
    /// bewusst einen Hinweistext statt einer impliziten Gesamtsumme ueber alle HauptFA (Nachtrag AK N2d).
    /// </summary>
    public bool HasKwFilter { get; init; }
}
