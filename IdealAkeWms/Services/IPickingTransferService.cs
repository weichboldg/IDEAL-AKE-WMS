namespace IdealAkeWms.Services;

public class PickingSelectionItem
{
    public int PickingItemId { get; set; }
    public int? SourceStorageLocationId { get; set; }
    public bool IsBaugruppe { get; set; }
}

public class PickingTransferResult
{
    public bool Success { get; set; }
    public int TransferredCount { get; set; }
    public bool IsPickingTransportConflict { get; set; }
    public int? ConflictStorageLocationId { get; set; }
    public string? ConflictStorageLocationCode { get; set; }
    public string? CurrentWaNumbers { get; set; }
    public string? NewWaNumber { get; set; }
}

public interface IPickingTransferService
{
    Task<int> TransferPickedItemsAsync(
        int productionOrderId,
        int targetStorageLocationId,
        int? appUserId,
        string displayName,
        string windowsUser);

    Task<PickingTransferResult> CheckAndTransferPickedItemsAsync(
        int productionOrderId,
        int targetStorageLocationId,
        bool forceTransfer,
        List<PickingSelectionItem>? selectedItems,
        int? appUserId,
        string displayName,
        string windowsUser);
}
