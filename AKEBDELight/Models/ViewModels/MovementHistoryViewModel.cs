namespace AKEBDELight.Models.ViewModels;

public class MovementHistoryViewModel
{
    public List<MovementHistoryItem> Items { get; set; } = new();
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
    public string? FilterArticle { get; set; }
    public int? FilterStorageLocationId { get; set; }
    public MovementType? FilterMovementType { get; set; }
    public int? FilterUserId { get; set; }
    public string? FilterProductionOrder { get; set; }
    public List<StorageLocation> StorageLocations { get; set; } = new();
    public List<User> Users { get; set; } = new();

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class MovementHistoryItem
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string? ArticleDescription { get; set; }
    public decimal Quantity { get; set; }
    public string StorageLocationCode { get; set; } = string.Empty;
    public string? SourceStorageLocationCode { get; set; }
    public MovementType MovementType { get; set; }
    public string MovementTypeName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? ProductionOrder { get; set; }
}
