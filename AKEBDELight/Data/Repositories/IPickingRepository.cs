using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;

namespace AKEBDELight.Data.Repositories;

public interface IPickingRepository
{
    Task<List<PickingItem>> GetByProductionOrderAsync(int productionOrderId);
    Task InitializePickingAsync(int productionOrderId, List<BomItem> bomItems,
        string createdBy, string createdByWindows);
    Task<PickingItem?> GetByIdAsync(int pickingItemId);
    Task TogglePickedAsync(int pickingItemId, int? storageLocationId,
        string userName, string windowsUser, bool isBaugruppe = false);
    Task<List<PickingItem>> GetPickedNotTransferredAsync(int productionOrderId);
    Task MarkAsTransferredAsync(List<int> pickingItemIds, DateTime transferredAt);
}
