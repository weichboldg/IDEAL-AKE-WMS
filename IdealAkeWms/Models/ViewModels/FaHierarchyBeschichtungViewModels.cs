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
}
