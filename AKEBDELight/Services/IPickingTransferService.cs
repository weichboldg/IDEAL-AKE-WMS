namespace AKEBDELight.Services;

public interface IPickingTransferService
{
    Task<int> TransferPickedItemsAsync(
        int productionOrderId,
        int targetStorageLocationId,
        int? appUserId,
        string displayName,
        string windowsUser);
}
