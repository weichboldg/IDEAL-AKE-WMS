namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.88.0: Eingabe eines Kommissionierbereichs (Anlegen/Bearbeiten).</summary>
public sealed record PickingAreaEingabe(string? Name, string? Kuerzel, int Reihenfolge, bool IsActive, bool IsDefault, bool IstVorkommissionierung)
{
    public PickingAreaEingabe() : this(null, null, 0, true, false, false) { }
    public int Id { get; init; }
}

/// <summary>v1.88.0: Regelliste (Server-Spaltenfilter + Pagination) mit Auswahllisten fuer das Anlegen.</summary>
public class PickingAreaRegelnViewModel
{
    public List<PickingAreaRule> Regeln { get; set; } = new();
    public int? BereichId { get; set; }
    public List<PickingArea> Bereiche { get; set; } = new();
    public List<string> Kommissionierziele { get; set; } = new();
    public List<string> Lagerorte { get; set; } = new();
    public List<string> Artikelgruppen { get; set; } = new();
    public bool CanEdit { get; set; }
    public PaginationState Pagination { get; set; } = new();
}
