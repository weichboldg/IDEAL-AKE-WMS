using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Kommissionierliste (Teil 3). Die Gruppen sind die
/// wiederverwendbaren <see cref="FaHierarchyListGroupViewModel"/> aus dem gemeinsamen Baustein;
/// hier kommen nur die Teil-3-Steuerelemente (Ziel-Dropdown) und der Paging-State dazu.
///
/// <para><b>Nachtrag 2026-08-13, Aenderung 1:</b> Das Anomalie-Banner ist ersatzlos entfallen —
/// alle Positionen mit gesetztem <c>Kommissionieren</c> werden unabhaengig von <c>SubFA</c> gelistet.</para>
/// </summary>
public class FaHierarchyKommissionierListViewModel
{
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>Paging-State (Seiteneinheit = Gruppe/HauptFA; <c>TotalCount</c> zaehlt Gruppen).</summary>
    public PaginationState Pagination { get; init; } = new();

    /// <summary>Vorkommende Kommissionier-Ziele fuer das Filter-Dropdown.</summary>
    public IReadOnlyList<string> AvailableTargets { get; init; } = new List<string>();

    /// <summary>Aktuell gewaehltes Ziel (oder null = alle).</summary>
    public string? SelectedTarget { get; init; }

    /// <summary>Edge beim Start gefunden (<see cref="Services.PdfRenderStatus"/>): nur dann erscheint der
    /// PDF-Knopf an der Gruppen-Kopfzeile (Freigabe-Antwort 2).</summary>
    public bool PdfVerfuegbar { get; init; }
}

/// <summary>
/// Druck-ViewModel der IDEAL-Kommissionierliste (Teil 3): dieselbe gefilterte Menge wie der
/// Bildschirm, aber ohne Paging (ganze Menge, ein Ausdruck je <c>HauptFA</c>-Gruppe mit
/// Seitenumbruch). Barcode = <c>HauptFA</c> (vorberechnet als PNG-Base64 je Gruppe).
/// </summary>
public class FaHierarchyKommissionierPrintViewModel
{
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>HauptFA -&gt; Barcode-PNG als Base64 (Code 128).</summary>
    public IReadOnlyDictionary<int, string> Barcodes { get; init; } = new Dictionary<int, string>();

    public string? SelectedTarget { get; init; }

    /// <summary>Mindestens ein Spaltenfilter (bzw. Ziel-Filter) aktiv: das Dokument zeigt eine Teilmenge
    /// und muss das im Kopf ausweisen (AK 18) — am Bildschirm sieht man die Filterzeile, im Druck/PDF nicht.</summary>
    public bool IsFiltered { get; init; }
}
