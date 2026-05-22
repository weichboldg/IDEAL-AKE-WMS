namespace IdealAkeWms.Models.ViewModels;

public class ProductionOrderListViewModel
{
    public List<ProductionOrderListItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }

    /// <summary>enaio DMS-Links pro FA-Nummer (Key=OrderNumber, Value=Liste von DMS-Dokumenten)</summary>
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();
}

public class ProductionOrderListItem
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }
    public string? WorkplaceName { get; set; }

    // Calculated dates
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // Cross-cutting from PickingStatus (siehe Spec 6.1) — fuer Beschichtungstermin-Logik
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
}
