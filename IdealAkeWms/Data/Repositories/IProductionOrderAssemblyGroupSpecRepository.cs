using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderAssemblyGroupSpecRepository
{
    Task<ProductionOrderAssemblyGroupSpec?> GetByIdAsync(int id);

    /// <summary>Specs einer AssemblyGroup, sortiert nach SortOrder, dann Id.</summary>
    Task<List<ProductionOrderAssemblyGroupSpec>> GetByAssemblyGroupIdAsync(int assemblyGroupId);

    /// <summary>Bulk-Lookup fuer Edit-View: liefert Specs gruppiert per AssemblyGroupId.</summary>
    Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>>
        GetByAssemblyGroupIdsAsync(IEnumerable<int> assemblyGroupIds);

    Task<int> AddAsync(ProductionOrderAssemblyGroupSpec spec);
    Task UpdateAsync(ProductionOrderAssemblyGroupSpec spec);
    Task DeleteAsync(int id);
}
