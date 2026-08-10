using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Wiederverwendbare Gruppe des gemeinsamen FaHierarchy-Listen-/Druck-Bausteins
/// (<see cref="Services.FaHierarchyListBuilder"/>): genau eine <c>HauptFA</c>-Gruppe mit ihren
/// gefilterten Blatt-Positionen (<see cref="FaHierarchyNode"/>, <c>SubFA = 0</c>) und den ueber
/// eine <b>eigene</b> Abfrage (kein Fan-out-Join) zugeordneten Kopf-Zeilen
/// (<see cref="FaHierarchyOrderInfo"/>).
///
/// <para><b>Baustein/Erweiterungs-Naht (Teil 4/5):</b> Diese Klasse ist bewusst flag-neutral —
/// Teil 3 (Kommissionieren), Teil 4 (Beschichtet) und Teil 5 (VMBedarf) verwenden dieselbe Gruppe;
/// nur das Filter-Flag, die angezeigten Spalten und das Kopf-Layout sind teilspezifisch (in der
/// jeweiligen View). Kein teilspezifischer Zustand in der Gruppe selbst.</para>
/// </summary>
public class FaHierarchyListGroupViewModel
{
    public int HauptFA { get; init; }

    /// <summary>Gefilterte Blatt-Positionen (<c>SubFA = 0</c> + Flag), nach <c>Position</c> sortiert.</summary>
    public List<FaHierarchyNode> Positions { get; init; } = new();

    /// <summary>Alle Kopf-Zeilen dieses <c>HauptFA</c> (Kombigeraet =&gt; mehrere Montage-Abteilungen).</summary>
    public List<FaHierarchyOrderInfo> OrderInfos { get; init; } = new();

    /// <summary>Kombigeraet-Indikator: mehrere Kopf-Zeilen (Montage-Abteilungen) zum selben HauptFA.</summary>
    public bool IsAmbiguous => OrderInfos.Count > 1;
}

/// <summary>
/// View-seitiges Aggregat fuer das Anomalie-Warnbanner (kein Domain-Objekt): Positionen mit
/// <c>SubFA != 0</c> UND gesetztem Flag werden nicht kommissioniert/gelistet, aber operator-sichtbar
/// gemeldet (Anzahl + betroffene <c>HauptFA</c>). Wird von Bildschirmliste UND Druck gemeinsam
/// gelesen, damit die Absicherung aus der Arbeitsannahme (Blatt-nur, Teil 3 Fachliche Anforderung 3)
/// in beiden Kanaelen identisch erscheint.
/// </summary>
public class FaHierarchyAnomalyBannerViewModel
{
    /// <summary>Summe der ausgeschlossenen Positionszeilen (ueber alle betroffenen HauptFA).</summary>
    public int PositionCount { get; init; }

    /// <summary>Betroffene HauptFA-Nummern (aufsteigend, dedupliziert).</summary>
    public IReadOnlyList<int> AffectedHauptFas { get; init; } = new List<int>();

    public bool HasAnomalies => PositionCount > 0;

    /// <summary>Kompakte HauptFA-Aufzaehlung fuer den Banner-Text.</summary>
    public string HauptFaList => string.Join(", ", AffectedHauptFas);
}
