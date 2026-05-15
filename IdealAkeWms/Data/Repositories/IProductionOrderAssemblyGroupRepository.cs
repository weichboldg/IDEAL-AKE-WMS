using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderAssemblyGroupRepository
{
    Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdAsync(int productionOrderId);
    Task<ProductionOrderAssemblyGroup?> GetByPoAndKeyAsync(int productionOrderId, string groupKey);

    /// <summary>Pivot fuer Index-View: orderId → (groupKey → isApplicable). Spec 7.3.</summary>
    Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> productionOrderIds);

    Task SetIsApplicableAsync(int productionOrderId, string groupKey, bool value, string modifiedBy, string modifiedByWindows);

    Task<ProductionOrderAssemblyGroup?> GetByIdAsync(int id);

    Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds);

    Task SetIsCompletedAsync(int assemblyGroupId, bool value, string completedBy,
        string modifiedBy, string modifiedByWindows);
}
