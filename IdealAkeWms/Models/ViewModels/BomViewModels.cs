namespace IdealAkeWms.Models.ViewModels;

public record BomQueryResult(List<BomItem> Items, string DataSource);

public class BomItem
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

public class StockLocationInfo
{
    public string Code { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int StorageLocationId { get; set; }
}

public class BomItemViewModel
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
    public List<StockLocationInfo> StockLocations { get; set; } = new();

    // Tree structure
    public int TreeLevel { get; set; }
    public bool IsBaugruppe { get; set; }

    // Picking fields
    public int? PickingItemId { get; set; }
    public bool IsPicked { get; set; }
    public int? SourceStorageLocationId { get; set; }
    public int? SuggestedSourceStorageLocationId { get; set; }
    public bool IsTransferred { get; set; }
}

public class BomViewModel
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public List<BomItemViewModel> Items { get; set; } = new();
    public string? FilterText { get; set; }
    public string? DefaultFilterBeschaffung { get; set; }
    public string? DefaultFilterArtikelgruppe { get; set; }
    public List<StorageLocation> AllStorageLocations { get; set; } = new();
    public string? DataSource { get; set; }
    public bool RecursiveFilterSearch { get; set; }
}

public class PrintPickingViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? PickedBy { get; set; }
    public List<PrintPickingItem> Items { get; set; } = new();
}

public class PrintPickingItem
{
    public string Artikelnummer { get; set; } = string.Empty;
    public string? Bezeichnung1 { get; set; }
    public decimal Menge { get; set; }
}

public class PrintBomViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public List<PrintBomItem> Items { get; set; } = new();
    /// <summary>Aktive Filterinfo für Header-Anzeige (z.B. "Artikelgruppe=960")</summary>
    public string? FilterInfo { get; set; }
}

public class PrintBomItem
{
    public string? Position { get; set; }
    public string? Baugruppe { get; set; }
    public string? Ressourcenummer { get; set; }
    public string? Bezeichnung1 { get; set; }
    public string? Bezeichnung2 { get; set; }
    public decimal Menge { get; set; }
    public string? Beschaffungsartikel { get; set; }
    public string? Artikelgruppe { get; set; }
    public string? Lagerplatz { get; set; }
    public int TreeLevel { get; set; }
    public bool IsBaugruppe { get; set; }
}
