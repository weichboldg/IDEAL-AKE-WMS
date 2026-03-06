namespace IdealAkeWms.Models.ViewModels;

public class LocationTransferViewModel
{
    public int? SourceStorageLocationId { get; set; }
    public string? SourceStorageLocationCode { get; set; }
    public int? TargetStorageLocationId { get; set; }
    public List<StorageLocation> AllStorageLocations { get; set; } = new();
    public List<StockOverviewItem> SourceItems { get; set; } = new();
}
