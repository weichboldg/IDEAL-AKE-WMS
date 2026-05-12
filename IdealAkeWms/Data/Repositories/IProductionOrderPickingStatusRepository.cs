using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderPickingStatusRepository
{
    Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task<Dictionary<int, ProductionOrderPickingStatus>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds);
    Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows);
    Task SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy, string modifiedBy, string modifiedByWindows);
    Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName, string modifiedBy, string modifiedByWindows);
    Task SetPickingStatusTextAsync(int productionOrderId, string? statusText, string modifiedBy, string modifiedByWindows);
    Task SetIsDonePickingAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);
    Task SetPriorityAsync(int productionOrderId, int? priority, string modifiedBy, string modifiedByWindows);
    Task SetCoatingPartsAsync(Dictionary<int, bool> orderIdToHasCoatingParts);
    Task<List<ProductionOrder>> GetReleasedForPickingAsync();
    Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId);
    Task<int> GetReleasedForPickingCountAsync();
}
