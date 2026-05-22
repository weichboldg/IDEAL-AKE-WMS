using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

public class OseonTrackingViewModel
{
    public List<OseonOrderGroupViewModel> OrderGroups { get; set; } = new();
    public string? FilterCustomerOrder { get; set; }
    public string? FilterArticle { get; set; }
    public string? FilterWorkplace { get; set; }
    public bool ShowFinished { get; set; }
    public bool UseRelevanceFilter { get; set; } = true;
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();

    // Pagination (legacy fields)
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalGroupCount { get; set; }

    public PaginationState Pagination { get; set; } = new();
}

public class OseonOrderGroupViewModel
{
    public string CustomerOrderNumber { get; set; } = string.Empty;
    public TrafficLightColor WorstColor { get; set; }
    public int TotalSubOrders { get; set; }
    public int FinishedSubOrders { get; set; }
    public List<OseonSubOrderViewModel> SubOrders { get; set; } = new();

    /// <summary>Aggregierter Status-Text der Gruppe (z.B. "In Arbeit" wenn mindestens ein Subauftrag in Arbeit)</summary>
    public string GroupStatusText { get; set; } = string.Empty;
    public string GroupStatusBadgeClass { get; set; } = string.Empty;
}

public class OseonSubOrderViewModel
{
    public int Id { get; set; }
    public string OseonOrderNumber { get; set; } = string.Empty;
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public string? WorkplaceName { get; set; }
    public int OseonStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public decimal QuantityTarget { get; set; }
    public decimal QuantityActual { get; set; }
    public DateTime? DueDate { get; set; }
    public TrafficLightColor Color { get; set; }
    public List<OseonOperationViewModel> Operations { get; set; } = new();
}

public class OseonOperationViewModel
{
    public string PositionNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OseonStatus { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public bool IsFirstOperation { get; set; }
    public bool IsLastOperation { get; set; }
    public TrafficLightColor Color { get; set; }
    public DateTime? CalculatedDueDate { get; set; }
    public bool IsOseonRelevant { get; set; } = true;
}

public class OseonOperationConfigViewModel
{
    public List<OseonOperationConfig> Configs { get; set; } = new();
    public List<string> UnconfiguredNames { get; set; } = new();
}
