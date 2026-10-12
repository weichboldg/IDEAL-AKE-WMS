using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Bildschirm-ViewModel des IDEAL-Beschichtungsauftrags (Teil 4). Die Gruppen sind die
/// wiederverwendbaren <see cref="FaHierarchyListGroupViewModel"/> aus dem gemeinsamen Baustein
/// (<see cref="Services.FaHierarchyListBuilder"/>) — hier ohne Ziel-Dropdown und ohne Anomalie-Banner
/// (Teil 4 filtert fest auf <c>Beschichtet == true</c> ueber <b>alle</b> Ebenen; <c>SubFA != 0</c>
/// ist legitim, keine Anomalie). Nur Paging-State kommt dazu.
/// </summary>
public class FaHierarchyBeschichtungListViewModel
{
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>Paging-State (Seiteneinheit = Gruppe/HauptFA; <c>TotalCount</c> zaehlt Gruppen).</summary>
    public PaginationState Pagination { get; init; } = new();

    /// <summary>Edge beim Start gefunden (<see cref="Services.PdfRenderStatus"/>): nur dann erscheint der
    /// PDF-Knopf an der Gruppen-Kopfzeile (Freigabe-Antwort 2).</summary>
    public bool PdfVerfuegbar { get; init; }

    /// <summary>Vergabe-Stand je HauptFA der Seite (Teil 4): Auftragsnummern, freie Positionen, Nummern je Position.</summary>
    public IReadOnlyDictionary<int, Services.BeschichtungsauftragVergabeInfo> Vergaben { get; init; } =
        new Dictionary<int, Services.BeschichtungsauftragVergabeInfo>();

    /// <summary>Schreibrecht (Rolle beschichtungsauftrag_schreiben): nur dann erscheint „Aus Vorschlagsliste erstellen".</summary>
    public bool DarfSchreiben { get; init; }
}

/// <summary>
/// Druck-ViewModel des IDEAL-Beschichtungsauftrags (Teil 4): dieselbe gefilterte Menge wie der
/// Bildschirm, aber ohne Paging (ganze Menge, ein Ausdruck je <c>HauptFA</c>-Gruppe mit
/// Seitenumbruch). Barcode = <c>HauptFA</c> (vorberechnet als PNG-Base64 je Gruppe). Das Markup ist
/// bewusst mit eingebettetem CSS (kein externer Verweis) gerendert, damit dieselbe View spaeter von
/// der separaten PDF-Erzeugungs-Spec wiederverwendet werden kann.
/// </summary>
public class FaHierarchyBeschichtungPrintViewModel
{
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>HauptFA -&gt; Barcode-PNG als Base64 (Code 128).</summary>
    public IReadOnlyDictionary<int, string> Barcodes { get; init; } = new Dictionary<int, string>();

    /// <summary>Mindestens ein Spaltenfilter aktiv: das Dokument zeigt eine Teilmenge und muss das im
    /// Kopf ausweisen (AK 18) — am Bildschirm sieht man die Filterzeile, im Druck/PDF nicht.</summary>
    public bool IsFiltered { get; init; }
}

/// <summary>
/// Eine Aggregatzeile der <b>Summiert-Ansicht</b> des Beschichtungsauftrags (Spec
/// 2026-10-06-beschichtungsauftrag-ausbau-teil-1, Antwort 1): eindeutig durch <c>(HauptFA, Artnr)</c>.
/// Summiert wird nur die <c>Sollmenge</c>; Haupt-Artnr., Matchcode und die Maße kommen aus der
/// ersten Position (nach <c>Position</c>, dann <c>SubFA</c>). Weichen die Maße innerhalb der Gruppe ab,
/// ist <see cref="MasseUneinheitlich"/> gesetzt (Invarianten-Warnung, Gruppe wird nicht aufgeteilt).
/// </summary>
public class FaHierarchyBeschichtungSummiertRowViewModel
{
    public int HauptFA { get; init; }
    public string? HauptArtnr { get; init; }
    public string Artnr { get; init; } = string.Empty;
    public string? Matchcode { get; init; }
    public decimal SummeSollmenge { get; init; }
    public int PositionCount { get; init; }
    public decimal Breite { get; init; }
    public decimal Hoehe { get; init; }
    public decimal Tiefe { get; init; }

    /// <summary>Maße der Positionen dieser Gruppe sind nicht alle gleich — angezeigt werden die der ersten Position.</summary>
    public bool MasseUneinheitlich { get; init; }
}

/// <summary>Ergebnis der Beschichtungs-Summierung: Seitenzeilen, Gesamtzahlen VOR dem Paging und die
/// Kopfzeilen (<see cref="FaHierarchyOrderInfo"/>) je HauptFA der Seite (fan-out-frei, eigene Menge).</summary>
public class FaHierarchyBeschichtungSummiertResult
{
    public List<FaHierarchyBeschichtungSummiertRowViewModel> Rows { get; init; } = new();
    public int TotalRowCount { get; init; }
    public int TotalGroupCount { get; init; }
    public IReadOnlyDictionary<int, List<FaHierarchyOrderInfo>> OrderInfos { get; init; } =
        new Dictionary<int, List<FaHierarchyOrderInfo>>();
}

/// <summary>Bildschirm-ViewModel der Summiert-Ansicht (Gruppen-Paging, Seiteneinheit = HauptFA).</summary>
public class FaHierarchyBeschichtungSummiertViewModel
{
    public List<FaHierarchyBeschichtungSummiertRowViewModel> Rows { get; init; } = new();
    public IReadOnlyDictionary<int, List<FaHierarchyOrderInfo>> OrderInfos { get; init; } =
        new Dictionary<int, List<FaHierarchyOrderInfo>>();
    public PaginationState Pagination { get; init; } = new();
    public bool PdfVerfuegbar { get; init; }

    /// <summary>Vergabe-Stand je HauptFA der Seite (Teil 4): Auftragsnummern, freie Positionen, Nummern je Position.</summary>
    public IReadOnlyDictionary<int, Services.BeschichtungsauftragVergabeInfo> Vergaben { get; init; } =
        new Dictionary<int, Services.BeschichtungsauftragVergabeInfo>();

    /// <summary>Schreibrecht (Rolle beschichtungsauftrag_schreiben): nur dann erscheint „Aus Vorschlagsliste erstellen".</summary>
    public bool DarfSchreiben { get; init; }
}

/// <summary>Druck-ViewModel der Summiert-Ansicht: ganze gefilterte Menge, ein Ausdruck je HauptFA.</summary>
public class FaHierarchyBeschichtungSummiertPrintViewModel
{
    public List<FaHierarchyBeschichtungSummiertRowViewModel> Rows { get; init; } = new();
    public IReadOnlyDictionary<int, List<FaHierarchyOrderInfo>> OrderInfos { get; init; } =
        new Dictionary<int, List<FaHierarchyOrderInfo>>();

    /// <summary>HauptFA -&gt; Barcode-PNG als Base64 (Code 128).</summary>
    public IReadOnlyDictionary<int, string> Barcodes { get; init; } = new Dictionary<int, string>();

    public bool IsFiltered { get; init; }

    /// <summary>Sichtbare Spalten zur Klickzeit bzw. gespeicherte Praeferenz; leer = alle.</summary>
    public IReadOnlyList<string> VisibleColumns { get; init; } = new List<string>();
}
