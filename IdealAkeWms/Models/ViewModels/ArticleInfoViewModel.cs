namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ArticleGroup { get; set; }
    public decimal ReorderLevel { get; set; }
    public string VaultUrl { get; set; } = string.Empty;
    public List<StockOverviewItem> StockByLocation { get; set; } = new();
    public decimal TotalStock => StockByLocation.Sum(s => s.CurrentQuantity);

    /// <summary>
    /// Summe der geplanten Verbraeuche dieses Bauteils ueber alle OFFENEN
    /// Fertigungsauftraege (BOM-Menge pro Geraet x FA-Stueckzahl).
    /// </summary>
    public decimal PlannedConsumption { get; set; }

    /// <summary>Gesamtbestand abzueglich der geplanten Verbraeuche offener FAs.</summary>
    public decimal AvailableStock => TotalStock - PlannedConsumption;

    public string? CategoryName { get; set; }
    public List<AttributeDisplayValue> AttributeDisplayValues { get; set; } = new();
    public List<ArticleUsageItem> UsedInOrders { get; set; } = new();
}

public class ArticleUsageItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
}

public class AttributeDisplayValue
{
    public string Name { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
}
