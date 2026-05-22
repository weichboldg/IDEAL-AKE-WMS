using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderBdeStatusRepository
{
    Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);
}
