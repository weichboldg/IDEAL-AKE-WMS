namespace AKEBDELight.Models.ViewModels;

public class ProductionOrderViewModel
{
    public List<ProductionOrder> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
}
