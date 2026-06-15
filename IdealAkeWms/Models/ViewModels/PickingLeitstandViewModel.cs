namespace IdealAkeWms.Models.ViewModels;

public class PickingLeitstandViewModel
{
    public List<PickingLeitstandItem> Items { get; set; } = new();
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

    public Dictionary<string, List<Data.Repositories.EnaioDmsDocumentLink>> EnaioDmsLinks { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();
}

public class PickingLeitstandItem
{
    // Sage-Master (identisch zu ProductionOrderListItem)
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
    public DateTime? KommissionierTermin { get; set; }
    public DateTime? VorkommissionierTermin { get; set; }
    public DateTime? BeschichtungTermin { get; set; }

    // PickingStatus-Felder (aus ProductionOrderPickingStatus)
    public string? PickingStatus { get; set; }
    public bool HasGlass { get; set; }
    public bool HasExternalPurchase { get; set; }
    public bool HasCoatingParts { get; set; }
    public bool IsCoatingDone { get; set; }
    public bool IsReleasedForPicking { get; set; }
    public int? PickingPriority { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleasedBy { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }

    // FaWorkStep-Detail-Pivot: Code (VK/VL/VE/VT/VA) -> Zelle (FaWorkStepId + IsCompleted).
    // Nur aktive AGs sind enthalten; fehlender Key = AG nicht anwendbar (leere Zelle).
    // Die Checkbox zeigt/togglet IsCompleted (Erledigt) — dasselbe Flag wie die FA-Abarbeitungsliste.
    public Dictionary<string, Data.Repositories.FaWorkStepPivotCell> WorkSteps { get; set; } = new();
}
