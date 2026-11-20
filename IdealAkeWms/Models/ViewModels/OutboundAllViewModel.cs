using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

public class OutboundAllViewModel
{
    public int? StorageLocationId { get; set; }
    public string? StorageLocationCode { get; set; }
    public string? ProductionOrder { get; set; }
    public bool IsPickingTransport { get; set; }
    public List<StockOverviewItem> Items { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
    /// <summary>v1.99.47: FA-Bezug je ArticleId (fehlend = kein FA).</summary>
    public Dictionary<int, FaAmPaar> FaJeArtikel { get; set; } = new();
    /// <summary>v1.99.47: nur IDEAL (Master an) – Sub-FA-Nummer → Haupt-FA-Nummer (OrdinalIgnoreCase); AKE leer.</summary>
    public Dictionary<string, string> HauptJeSub { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
