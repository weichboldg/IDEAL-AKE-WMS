namespace IdealAkeWms.Models.ViewModels;

public class ArticleInfoViewModel
{
    public string ArticleNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Matchcode { get; set; }
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

    // IDEAL FA-Liste-Hierarchie (Teil 7/8, Z4): nur bei Master ProduktionsauftragHierarchisch = true.
    /// <summary>Hierarchischer Modus → „Teil enthalten in"-Zaehlung als „N Aufträge · M Sub-FAs".</summary>
    public bool Hierarchical { get; set; }
    /// <summary>Z4: Anzahl distinkter HauptFAs (OrderNumber) unter <see cref="UsedInOrders"/> (Sub-FAs sind die Zeilen).</summary>
    public int UsedInHauptFaCount => UsedInOrders.Select(o => o.OrderNumber).Distinct().Count();
}

public class ArticleUsageItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    /// <summary>Sub-FA-Nummer (IDEAL Teil 7). Im Flachmodus == OrderNumber (Backfill); hierarchisch die Zeilen-Nummer.</summary>
    public string? SubOrderNumber { get; set; }
}

public class AttributeDisplayValue
{
    public string Name { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
}
