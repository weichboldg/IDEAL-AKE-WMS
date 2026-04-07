namespace IDEALAKEWMSService.Models;

/// <summary>
/// Service-internal DTO for a BOM item read from SAGE or OSEON.
/// Mirrors the shape of IdealAkeWms.Models.ViewModels.BomItem
/// but is duplicated here because the service project cannot reference
/// the web project (SDK.Worker vs SDK.Web).
/// </summary>
public class BomCacheItem
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
}
