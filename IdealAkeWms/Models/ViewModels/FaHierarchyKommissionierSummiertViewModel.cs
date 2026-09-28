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

    /// <summary>Artikeleigenschaft (je Summenschluessel konstant, aus der ersten Position uebernommen).</summary>
    public string? Matchcode { get; init; }

    /// <summary>Artikeleigenschaft (je Summenschluessel konstant, aus der ersten Position uebernommen).</summary>
    public string? Hauptlagerplatz { get; init; }
}

/// <summary>
/// Ergebnis einer Kommissionier-Summiert-Aggregation: die (paginierten) Aggregatzeilen plus die
/// Gesamtzahl VOR dem Paging (fuer <c>PaginationState.TotalCount</c>). <b>Gruppen-Paging</b>: Seiteneinheit
/// ist die HauptFA-Gruppe, nicht die Aggregatzeile — eine Seite enthaelt nie nur einen Teil der Zeilen
/// einer HauptFA (Muster <c>FaHierarchyListBuilder.Build</c>).
/// </summary>
public class FaHierarchyKommissionierSummiertResult
{
    public List<FaHierarchyKommissionierSummiertRowViewModel> Rows { get; init; } = new();

    /// <summary>Anzahl aller Aggregatzeilen NACH Spaltenfilter, VOR dem Paging.</summary>
    public int TotalRowCount { get; init; }

    /// <summary>Anzahl aller HauptFA-Gruppen NACH Spaltenfilter (Seiteneinheit), VOR dem Paging.</summary>
    public int TotalGroupCount { get; init; }
}

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Kommissionier-<b>Summiert-Ansicht</b> (Teil 3, Nachtrag
/// 2026-08-13). Flache Liste von <c>(HauptFA, Artnr, Ziel)</c>-Aggregatzeilen mit der
/// <c>Sollmenge</c>-Summe, plus Filterkarte (KW-Zeitraum + Ziel-Dropdown) und Paging-State
/// (Gruppen-Paging: Seiteneinheit = HauptFA-Gruppe).
/// </summary>
public class FaHierarchyKommissionierSummiertViewModel
{
    public List<FaHierarchyKommissionierSummiertRowViewModel> Rows { get; init; } = new();

    /// <summary>Paging-State (Gruppen-Paging: Seiteneinheit = HauptFA-Gruppe; <c>TotalCount</c> zaehlt Gruppen).</summary>
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
    /// <c>true</c>, sobald mindestens eine gueltige KW-Grenze gesetzt ist. Der KW-Filter ist optional
    /// (Antwort 4, Spec 2026-09-25-kommissionierliste-summierung-pdf — ersetzt N2d): ohne KW werden
    /// ALLE HauptFA einbezogen, dieses Flag steuert nur, ob eine KW-Einschraenkung aktiv ist.
    /// </summary>
    public bool HasKwFilter { get; init; }

    /// <summary>Edge beim Start gefunden (<see cref="Services.PdfRenderStatus"/>): nur dann erscheint der
    /// PDF-Knopf an der Gruppen-Kopfzeile (analog <see cref="FaHierarchyKommissionierListViewModel.PdfVerfuegbar"/>).</summary>
    public bool PdfVerfuegbar { get; init; }
}

/// <summary>
/// Druck-ViewModel der IDEAL-Kommissionier-<b>Summiert-Ansicht</b>: dieselbe gefilterte Menge wie
/// der Bildschirm, aber ohne Paging (ganze Menge, ein Ausdruck je <c>HauptFA</c>-Gruppe mit
/// Seitenumbruch). Analog <see cref="FaHierarchyKommissionierPrintViewModel"/>.
/// </summary>
public class FaHierarchyKommissionierSummiertPrintViewModel
{
    public List<FaHierarchyKommissionierSummiertRowViewModel> Rows { get; init; } = new();

    /// <summary>HauptFA -&gt; Barcode-PNG als Base64 (Code 128).</summary>
    public IReadOnlyDictionary<int, string> Barcodes { get; init; } = new Dictionary<int, string>();

    public string? SelectedTarget { get; init; }

    /// <summary>Mindestens ein Spaltenfilter (bzw. Ziel-Filter) aktiv: das Dokument zeigt eine Teilmenge
    /// und muss das im Kopf ausweisen — am Bildschirm sieht man die Filterzeile, im Druck/PDF nicht.</summary>
    public bool IsFiltered { get; init; }

    /// <summary>Zur Klickzeit aus dem DOM gelesene sichtbare Spalten (Query-Parameter <c>visibleColumns</c>,
    /// PrintBom-Konvention) bzw. Server-Rueckfall auf die gespeicherte Praeferenz. Leer = alle Spalten sichtbar.</summary>
    public IReadOnlyList<string> VisibleColumns { get; init; } = new List<string>();
}
