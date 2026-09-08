using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Models;

/// <summary>
/// Stuecklisten-Zeile aus der FA-Hierarchie (BOM-Bridge). Erbt die AKE-Felder von <see cref="BomItem"/>
/// (das selbst unveraendert bleibt — es wird per <c>SqlQueryRaw</c> auf die AKE-View gemappt, jede neue
/// Property dort braeche das Mapping) und traegt die hierarchischen Zusatzfelder.
/// </summary>
public sealed class FaHierarchyBomItem : BomItem
{
    /// <summary>Zeile ist eine Baugruppe mit eigenem Sub-FA (<c>FaHierarchyNode.SubFA != 0</c>).</summary>
    public bool IsBaugruppe { get; set; }
    public string? Kommissionieren { get; set; }
    public string? Hauptlagerplatz { get; set; }
    /// <summary>1 = direktes Kind des abgefragten FA; bei FullStructure die Tiefe ab der Wurzel.</summary>
    public int Ebene { get; set; }
    /// <summary>SubFA des unmittelbaren Elternknotens (bei DirectChildren: der abgefragte FA).</summary>
    public string? VaterSubFa { get; set; }
    /// <summary>Urspruengliche Sage-Position (<c>FaHierarchyNode.Position</c>). In FullStructure ist
    /// <see cref="BomItem.Position"/> ein rekursiver Anzeige-Pfad — die Sage-Position bleibt hier sichtbar
    /// (Freigabe-Bedingung a).</summary>
    public string? SagePosition { get; set; }
    /// <summary>Blatt-/Struktur-Waise (VaterFA ohne importierten Elternknoten), markiert an die HauptFA gehaengt.</summary>
    public bool IsWaise { get; set; }
    /// <summary>Zwei Geschwister mit gleicher Position unter demselben Eltern-FA — Pfad wurde mit Suffix
    /// eindeutig gemacht und der Fall protokolliert, nie still ueberschrieben (Freigabe-Bedingung b).</summary>
    public bool PositionsKollision { get; set; }
}
