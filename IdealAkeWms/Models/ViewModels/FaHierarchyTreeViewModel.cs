using System.Collections.Generic;
using System.Linq;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// Seiten-ViewModel der rekursiven FA-Baumanzeige (IDEAL Teil 2).
/// Seiteneinheit ist die <b>Struktur</b> (nicht die Knoten-Zeile): <see cref="Pagination"/>.TotalCount
/// zaehlt Strukturen (Wurzeln + Waisen-Pseudowurzeln), eine Struktur wird nie ueber Seiten getrennt.
/// </summary>
public class FaHierarchyTreeViewModel
{
    /// <summary>Die auf dieser Seite dargestellten (vollstaendigen) Strukturen.</summary>
    public List<FaHierarchyStructure> Structures { get; set; } = new();

    // Filterkarte (server-seitig, auf Struktur-Ebene — entscheidet WELCHE Strukturen erscheinen).
    public string? FilterHauptFa { get; set; }
    public string? FilterCustomer { get; set; }
    public string? FilterText { get; set; }

    public PaginationState Pagination { get; set; } = new();

    /// <summary>User-Setting: Auto-Expand des Pfads zu einem client-seitigen Filtertreffer
    /// (gekoppelt an <see cref="Models.User.RecursiveFilterSearch"/>, wie beim BOM-Tree).</summary>
    public bool RecursiveFilterSearch { get; set; }

    /// <summary>Effektiver Tiefen-Cap (AppSettings, Default 500) — nur informativ fuer die UI.</summary>
    public int DepthCap { get; set; }

    /// <summary>True, wenn eine Struktur auf dieser Seite wegen Tiefen-/Zyklenverstoss unvollstaendig ist.</summary>
    public bool HasErrorStructures => Structures.Any(s => s.HasError);
}

/// <summary>
/// Eine vollstaendige Struktur = eine Wurzel (oder Waisen-Pseudowurzel) samt rekursivem Knotenbaum
/// und den zugehoerigen <see cref="FaHierarchyOrderInfo"/>-Kopfdaten ihres HauptFA.
/// </summary>
public class FaHierarchyStructure
{
    public int HauptFA { get; set; }

    /// <summary>Wurzelknoten der Struktur (im Normalfall genau einer).</summary>
    public List<FaHierarchyTreeNode> Roots { get; set; } = new();

    /// <summary>Kopfdaten des HauptFA. Bei Kombigeraeten mehrere Zeilen (mehrere MontageAbteilung).</summary>
    public List<FaHierarchyOrderInfo> OrderInfos { get; set; } = new();

    /// <summary>Q1: HauptFA traegt mehrere OrderInfo-Zeilen (mehrere MontageAbteilung) — als
    /// „mehrdeutig" gekennzeichnet, alle Kopfzeilen werden im Klartext gezeigt (kein Positions-Split).</summary>
    public bool IsAmbiguous { get; set; }

    /// <summary>Struktur ist eine Waisen-Pseudowurzel (VaterFA zeigt auf keinen importierten Parent).</summary>
    public bool IsOrphan { get; set; }

    /// <summary>Der nicht auffindbare VaterFA-Wert (nur bei <see cref="IsOrphan"/>).</summary>
    public int? OrphanVaterFa { get; set; }

    /// <summary>Aufbau abgebrochen (Tiefen-Cap oder Zyklus) — Struktur wird als fehlerhaft markiert,
    /// nicht leer dargestellt und nicht still verworfen.</summary>
    public bool HasError { get; set; }

    /// <summary>Grund des Abbruchs (fuer UI-Badge + ILogger-Warnung).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>SubFA-Wert der Abbruchstelle (fuer ILogger-Warnung).</summary>
    public int? ErrorAtSubFa { get; set; }

    /// <summary>Anzahl aufgebauter Knoten (inkl. Wurzel).</summary>
    public int NodeCount { get; set; }

    internal void MarkError(string reason, int atSubFa)
    {
        if (HasError) return; // erster Verstoss gewinnt (Abbruch)
        HasError = true;
        ErrorMessage = reason;
        ErrorAtSubFa = atSubFa;
    }
}

/// <summary>Ein Knoten im rekursiven FA-Baum (Zeiger-Rekursion ueber VaterFA/SubFA).</summary>
public class FaHierarchyTreeNode
{
    public FaHierarchyNode Node { get; set; } = null!;
    public List<FaHierarchyTreeNode> Children { get; set; } = new();
    public int Level { get; set; }

    public bool HasChildren => Children.Count > 0;

    /// <summary>Rein praesentativer Knotentyp fuer das Typ-Icon (kein DB-Feld).</summary>
    public FaNodeIconType IconType => FaNodeClassifier.Classify(Node);
}

/// <summary>
/// Praesentativer Knotentyp fuer das Typ-Icon der FA-Struktur-Baumtabelle.
/// Kein Datenfeld — ausschliesslich aus vorhandenen Feldern abgeleitet.
/// </summary>
public enum FaNodeIconType
{
    /// <summary>Wurzel/Endprodukt der Struktur (VaterFA IS NULL).</summary>
    Root,
    /// <summary>Baugruppe mit eigenem Sub-FA (Eigenfertigung, SubFA != 0).</summary>
    Assembly,
    /// <summary>Zukaufteil (Blatt, Beschaffungsartikel).</summary>
    Purchased,
    /// <summary>Lager-/Fertigungsmaterial (Blatt, kein Beschaffungsartikel).</summary>
    Material
}

/// <summary>
/// Ordnet einem <see cref="FaHierarchyNode"/> seinen Typ-Icon-Typ zu (IDEAL Teil 2,
/// Darstellungs-Nachtrag Etappe 7). Geordnete Praezedenzkette — die Reihenfolge legt
/// fest, welcher Typ gewinnt, wenn ein Knoten mehrere Kriterien erfuellt (eine Wurzel
/// ist typischerweise zugleich Baugruppe; das Wurzel-Icon gewinnt):
/// Wurzel -> Baugruppe -> Zukaufteil -> Material.
/// Rein praesentativ, unit-testbar (kein DB-Zugriff, keine Seiteneffekte).
/// </summary>
public static class FaNodeClassifier
{
    public static FaNodeIconType Classify(FaHierarchyNode node)
    {
        // 1. Wurzel/Endprodukt: oberste Zeile der Struktur (VaterFA IS NULL) —
        //    unabhaengig von SubFA (eine Wurzel ist meist zugleich Baugruppe).
        if (node.VaterFA == null)
            return FaNodeIconType.Root;

        // 2. Baugruppe (Eigenfertigung): eigener Sub-FA.
        if (node.SubFA != 0)
            return FaNodeIconType.Assembly;

        // 3. Zukaufteil: Blatt (SubFA == 0) UND Beschaffungsartikel.
        if (node.Beschaffungsartikel)
            return FaNodeIconType.Purchased;

        // 4. Lager-/Fertigungsmaterial: Blatt, kein Beschaffungsartikel.
        return FaNodeIconType.Material;
    }
}
