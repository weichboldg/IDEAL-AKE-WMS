namespace IdealAkeWms.Models.ViewModels;

public class TrackingViewModel
{
    public List<TrackingOrderGroup> OrderGroups { get; set; } = new();
    public string? FilterOrderNumber { get; set; }
    public int? FilterWorkplaceId { get; set; }
    public bool ShowReported { get; set; }
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public bool CanReport { get; set; }
    public PaginationState Pagination { get; set; } = new();
}

public class TrackingOrderGroup
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Customer { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string? WorkplaceName { get; set; }
    public int TotalOperations { get; set; }
    public int ReportedOperations { get; set; }
    public List<TrackingOperationItem> Operations { get; set; } = new();
}

public class TrackingOperationItem
{
    public int Id { get; set; }
    public string OperationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string? WorkplaceName { get; set; }
    public string? OrderNumber { get; set; }
    public bool IsReportable { get; set; }
    public bool IsExternalSystem { get; set; }
    public bool IsReported { get; set; }
    public DateTime? ReportedAt { get; set; }
    public string? ReportedBy { get; set; }
    public string? ExternalSource { get; set; }
}

public class TrackingByWorkplaceViewModel
{
    public ProductionWorkplace Workplace { get; set; } = null!;
    public List<TrackingOperationItem> Operations { get; set; } = new();
    public bool CanReport { get; set; }
    public string? FilterOrderNumber { get; set; }
    public bool ShowReported { get; set; }
}
