namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal ReorderLevel { get; set; }
    public string VaultUrl { get; set; } = string.Empty;
    public List<StockOverviewItem> StockByLocation { get; set; } = new();
    public decimal TotalStock => StockByLocation.Sum(s => s.CurrentQuantity);
}
