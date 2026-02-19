namespace AKEBDELight.Services;

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
        int? appUserId,
        string displayName,
        string windowsUser);
}
