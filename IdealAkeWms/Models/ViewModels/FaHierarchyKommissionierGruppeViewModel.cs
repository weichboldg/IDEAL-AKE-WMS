using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Kommissionierliste (Teil 3). Die Gruppen sind die
/// wiederverwendbaren <see cref="FaHierarchyListGroupViewModel"/> aus dem gemeinsamen Baustein;
/// hier kommen nur die Teil-3-Steuerelemente (Ziel-Dropdown), der Paging-State und die
/// Anomalie-Banner-Daten dazu.
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

    /// <summary>Anomalie-Banner (SubFA != 0 mit gesetztem Kommissionieren) — nur sichtbar wenn vorhanden.</summary>
    public FaHierarchyAnomalyBannerViewModel Anomalies { get; init; } = new();
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

    /// <summary>Anomalie-Banner (erscheint auch auf dem Ausdruck, wenn vorhanden).</summary>
    public FaHierarchyAnomalyBannerViewModel Anomalies { get; init; } = new();
}
