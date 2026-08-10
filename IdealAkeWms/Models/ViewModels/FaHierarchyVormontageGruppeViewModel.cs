using System.Collections.Generic;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Bildschirm-ViewModel der IDEAL-Vormontage-<b>Sicht 1 (Einzelne Teile)</b> (Teil 5). Die Gruppen
/// sind die wiederverwendbaren <see cref="FaHierarchyListGroupViewModel"/> aus dem gemeinsamen
/// Baustein (<see cref="Services.FaHierarchyListBuilder"/>, <c>leafOnly: true</c> +
/// <c>anomalyOnNonLeaf: true</c> wie Teil 3) — hier kommen nur die Teil-5-Steuerelemente
/// (<c>VMBedarf</c>-Reiter/Arbeitsbereich), der Paging-State und die Anomalie-Banner-Daten dazu.
///
/// <para><b>Reiter-Dimension (neu ggue. Teil 3/4):</b> <see cref="AvailableBereiche"/> ist die
/// Distinct-Liste der vorkommenden <c>VMBedarf</c>-Werte (ohne NULL/Leer); je Wert ein Reiter.
/// <see cref="SelectedBereich"/> ist der aktive Reiter. <c>VMBedarf</c> ist eine Filter-/Reiter-
/// Dimension, KEINE Seiteneinheit — Seiteneinheit bleibt die <c>HauptFA</c>-Gruppe (wie Teil 3/4).</para>
/// </summary>
public class FaHierarchyVormontageListViewModel
{
    public List<FaHierarchyListGroupViewModel> Groups { get; init; } = new();

    /// <summary>Paging-State (Seiteneinheit = Gruppe/HauptFA; <c>TotalCount</c> zaehlt Gruppen).</summary>
    public PaginationState Pagination { get; init; } = new();

    /// <summary>Vorkommende <c>VMBedarf</c>-Arbeitsbereiche (Reiter, ohne NULL/Leer, sortiert).</summary>
    public IReadOnlyList<string> AvailableBereiche { get; init; } = new List<string>();

    /// <summary>Aktiver Reiter (<c>VMBedarf</c>-Wert) oder null (nur wenn gar kein Bereich existiert).</summary>
    public string? SelectedBereich { get; init; }

    /// <summary>Anomalie-Banner (SubFA != 0 mit gesetztem <c>VMBedarf</c>) — nur sichtbar wenn vorhanden.</summary>
    public FaHierarchyAnomalyBannerViewModel Anomalies { get; init; } = new();
}
