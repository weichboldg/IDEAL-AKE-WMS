using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWorkOperationRepository : IRepository<WorkOperation>
{
    Task<List<WorkOperation>> GetByProductionOrderIdAsync(int productionOrderId);
    Task<List<WorkOperation>> GetByProductionOrderIdWithWorkplaceAsync(int productionOrderId);
}
