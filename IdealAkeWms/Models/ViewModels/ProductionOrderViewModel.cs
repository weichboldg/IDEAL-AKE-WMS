namespace IdealAkeWms.Models.ViewModels;

public class ProductionOrderViewModel
{
    public List<ProductionOrderViewItem> Items { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public string? FilterArticleNumber { get; set; }
    public string? FilterCustomer { get; set; }
    public bool ShowDone { get; set; }
    public int KommissionierTage { get; set; }
    public int VorkommissionierTage { get; set; }
    public int BeschichtungTage { get; set; }
    public bool CanPick { get; set; }
    public bool CanManagePickingRelease { get; set; }
    public bool LeitstandAktiv { get; set; }
    public bool PickerAssignmentEnabled { get; set; }

    /// <summary>enaio DMS-Links pro FA-Nummer (Key=OrderNumber, Value=Liste von DMS-Dokumenten)</summary>
    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();
}

public class ProductionOrderViewItem
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
    public string? PickingStatus { get; set; }

    // Calculated dates
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    public bool HasGlass { get; set; }
    public bool HasExternalPurchase { get; set; }
    public bool HasCooling { get; set; }
    public bool HasFan { get; set; }
    public bool HasElectric { get; set; }
    public bool HasDoors { get; set; }
    public bool HasSuperstructure { get; set; }
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
    public string? WorkplaceName { get; set; }

    // Leitstand: Freigabe
    public bool IsReleasedForPicking { get; set; }
    public int? PickingPriority { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }
}
