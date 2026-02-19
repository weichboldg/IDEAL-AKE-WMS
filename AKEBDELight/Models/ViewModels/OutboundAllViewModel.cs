namespace AKEBDELight.Models.ViewModels;

public class OutboundAllViewModel
{
    public int? StorageLocationId { get; set; }
    public string? StorageLocationCode { get; set; }
    public string? ProductionOrder { get; set; }
    public bool IsPickingTransport { get; set; }
    public List<StockOverviewItem> Items { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
}
