using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public class BulkReleaseResult
{
    public int Processed { get; set; }
    public List<string> SkippedNoArticle { get; set; } = new();
}

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

    /// <summary>
    /// Returns the current MAX(PickingPriority) over all released-and-not-done orders,
    /// or 0 if none exist. Optionally excludes one ProductionOrderId from the calculation.
    /// </summary>
    Task<int> GetMaxPickingPriorityAsync(int? excludeProductionOrderId = null);

    /// <summary>
    /// Bulk release/unrelease for a set of production orders in a single SaveChanges roundtrip.
    /// When releasing: skips orders without ArticleNumber and reports them in SkippedNoArticle.
    /// Assigns priorities continuing from MAX(existing released priority) for orders that don't have one.
    /// When releasing and assignedPickerId is provided, sets picker on every processed row.
    /// </summary>
    Task<BulkReleaseResult> SetReleaseBatchAsync(
        IEnumerable<int> productionOrderIds,
        bool release,
        int? assignedPickerId,
        string? assignedPickerName,
        string? releasedBy,
        string modifiedBy,
        string modifiedByWindows);

    /// <summary>
    /// Releases all Sub-FAs of the given HauptFA (OrderNumber) that are not yet released for
    /// picking, in a single SaveChanges roundtrip. Already-released rows are not loaded and stay
    /// fully untouched (value, audit fields, assigned picker). Priorities are assigned continuing
    /// from MAX(existing released priority), in SubOrderNumber ascending order, so parts of one
    /// device don't scatter across the day. The picker is set on every newly released row only
    /// when assignedPickerId is provided (inactive assignment duty leaves it empty).
    /// </summary>
    /// <returns>The number of rows actually changed (0 for an unknown OrderNumber).</returns>
    Task<int> SetReleaseForOrderNumberAsync(string orderNumber, int? assignedPickerId,
        string? assignedPickerName, string? releasedBy, string modifiedBy, string modifiedByWindows);

    /// <summary>
    /// Counts how many Sub-FAs of the given HauptFA (OrderNumber) are already released for picking.
    /// </summary>
    Task<int> CountReleasedByOrderNumberAsync(string orderNumber);
}
