using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Eine Aggregatzeile der IDEAL-Vormontage-<b>Sicht 2 (Summiert)</b> (Teil 5). Aggregations-
/// <b>Schluessel ist der <see cref="Matchcode"/></b> (NICHT die Artnr — der Matchcode identifiziert
/// eindeutig, was der Werker holt), aggregiert <b>je <c>VMBedarf</c>-Reiter</b>. Es werden
/// <b>zwei getrennte Mengen</b> summiert: <see cref="SummeSollmenge"/> UND
/// <see cref="SummeFertigungmenge"/> — keine ersetzt die andere.
///
/// <para><b>Bewusst akzeptierte Folge:</b> Fuehrt derselbe <c>Artnr</c> positionsabhaengig
/// verschiedene Matchcodes, entstehen daraus <b>mehrere</b> Aggregatzeilen. Das ist gewollt (der
/// Matchcode ist die Identitaet) und kein Fehler.</para>
/// </summary>
public class FaHierarchyVormontageAggregatViewModel
{
    /// <summary>Aggregations-Schluessel: der Matchcode (kann leer sein, dann eine eigene Sammelzeile).</summary>
    public string? Matchcode { get; init; }

    /// <summary>Der Arbeitsbereich (<c>VMBedarf</c>-Reiter), auf den diese Aggregatzeile bezogen ist.</summary>
    public string? VMBedarf { get; init; }

    /// <summary>Summe der <c>Sollmenge</c> ueber alle Blattpositionen dieses Matchcodes.</summary>
    public decimal SummeSollmenge { get; init; }

    /// <summary>Summe der <c>Fertigungmenge</c> ueber alle Blattpositionen dieses Matchcodes.</summary>
    public decimal SummeFertigungmenge { get; init; }

    /// <summary>Anzahl der aggregierten Blattpositionen (informativ).</summary>
    public int PositionCount { get; init; }
}

/// <summary>
/// Ergebnis einer Vormontage-Sicht-2-Aggregation: die (paginierten) Aggregatzeilen plus die
/// Gesamtzahl der Aggregatzeilen VOR dem Paging (fuer <c>PaginationState.TotalCount</c>).
/// <b>Seiteneinheit ist die Aggregatzeile</b> (begruendete ADR-0005-Ausnahme; siehe Spec Teil 5,
/// Fachliche Anforderung 8): eine Aggregatzeile gehoert per Definition zu mehreren Auftraegen und
/// laesst sich nicht nach <c>HauptFA</c> paginieren.
/// </summary>
public class FaHierarchyVormontageAggregatResult
{
    public List<FaHierarchyVormontageAggregatViewModel> Rows { get; init; } = new();

    /// <summary>Anzahl aller Aggregatzeilen (Seiteneinheit) NACH Spaltenfilter, VOR dem Paging.</summary>
    public int TotalRowCount { get; init; }
}

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Vormontage-<b>Sicht 2 (Summiert)</b> (Teil 5): flache Liste von
/// Matchcode-Aggregatzeilen (kein Gruppen-Rahmen), plus dieselbe <c>VMBedarf</c>-Reiter-Dimension
/// wie Sicht 1 und der Paging-State (Seiteneinheit = Aggregatzeile).
/// </summary>
public class FaHierarchyVormontageSummiertViewModel
{
    public List<FaHierarchyVormontageAggregatViewModel> Rows { get; init; } = new();

    /// <summary>Paging-State (Seiteneinheit = Aggregatzeile; <c>TotalCount</c> zaehlt Aggregatzeilen).</summary>
    public PaginationState Pagination { get; init; } = new();

    /// <summary>Vorkommende <c>VMBedarf</c>-Arbeitsbereiche (Reiter, ohne NULL/Leer, sortiert).</summary>
    public IReadOnlyList<string> AvailableBereiche { get; init; } = new List<string>();

    /// <summary>Aktiver Reiter (<c>VMBedarf</c>-Wert) oder null (nur wenn gar kein Bereich existiert).</summary>
    public string? SelectedBereich { get; init; }
}
