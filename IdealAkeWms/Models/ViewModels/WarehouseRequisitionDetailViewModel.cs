using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionDetailItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested,
    decimal? QuantityPicked,
    string StorageLocations,
    string? Note = null,
    ShortageStatus ShortageStatus = ShortageStatus.None);

public class WarehouseRequisitionDetailViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public WarehouseRequisitionStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionDetailItemViewModel> Items { get; set; } = new();
}
