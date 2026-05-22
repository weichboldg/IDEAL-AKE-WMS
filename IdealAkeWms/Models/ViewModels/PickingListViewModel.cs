namespace IdealAkeWms.Models.ViewModels;

public class PickingListViewModel
{
    public List<PickingListItem> Items { get; set; } = new();
    public bool ShowAllOrders { get; set; }
    public bool PickerAssignmentEnabled { get; set; }
    public PaginationState Pagination { get; set; } = new();
}

public class PickingListItem
{
    public int Id { get; set; }
    public int? PickingPriority { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Customer { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? KommissionierTermin { get; set; }
    public string? PickingStatus { get; set; }
    public int? AssignedPickerId { get; set; }
    public string? AssignedPickerName { get; set; }
}
