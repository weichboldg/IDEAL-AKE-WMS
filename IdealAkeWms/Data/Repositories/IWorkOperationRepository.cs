using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWorkOperationRepository : IRepository<WorkOperation>
{
    Task<List<WorkOperation>> GetByProductionOrderIdAsync(int productionOrderId);
    Task<List<WorkOperation>> GetByProductionOrderIdWithWorkplaceAsync(int productionOrderId);
    Task<List<WorkOperation>> GetAllWithOrderAndWorkplaceAsync();
    Task<List<WorkOperation>> GetByWorkplaceIdAsync(int workplaceId);
    Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId);
    Task<WorkOperation?> GetByFaAndOperationAsync(string faNumber, string operationNumber);
}
