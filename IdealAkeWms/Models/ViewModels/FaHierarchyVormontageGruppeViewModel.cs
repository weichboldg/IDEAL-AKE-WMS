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
///
/// <para><b>Nachtrag 2026-08-13:</b> Das Anomalie-Banner ist entfallen (alle Positionen unabhaengig
/// von <c>SubFA</c>); der optionale KW-Zeitraum-Filter (<see cref="KwVon"/>/<see cref="KwBis"/>) auf
/// <c>FE_Termin</c> steht zur konsistenten Bedienbarkeit zur Verfuegung.</para>
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

    /// <summary>KW-von-Eingabe (Format <c>YYYY-Www</c>, HTML <c>&lt;input type="week"&gt;</c>), optional.</summary>
    public string? KwVon { get; init; }

    /// <summary>KW-bis-Eingabe (Format <c>YYYY-Www</c>), optional.</summary>
    public string? KwBis { get; init; }
}
