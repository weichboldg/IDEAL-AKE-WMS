namespace IdealAkeWms.Models.ViewModels;

public class StockOverviewViewModel
{
    public List<StockOverviewItem> Items { get; set; } = new();
    public string? FilterArticle { get; set; }
    public int? FilterStorageLocationId { get; set; }
    public decimal? FilterMinQuantity { get; set; }
    public decimal? FilterMaxQuantity { get; set; }
    public string? FilterProductionOrder { get; set; }
    public List<StorageLocation> StorageLocations { get; set; } = new();
    public int WarningThresholdPercent { get; set; } = 150;
    public int CriticalThresholdPercent { get; set; } = 100;
}

public class StockOverviewItem
{
    public int ArticleId { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public string? Unit { get; set; }
    public int StorageLocationId { get; set; }
    public string StorageLocationCode { get; set; } = string.Empty;
    public string? StorageLocationDescription { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal? ReorderLevel { get; set; }
    public bool IsPickingTransport { get; set; }
}
