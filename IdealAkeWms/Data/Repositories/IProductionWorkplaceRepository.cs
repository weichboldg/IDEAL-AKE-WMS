using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionWorkplaceRepository : IRepository<ProductionWorkplace>
{
    Task<List<ProductionWorkplace>> GetAllOrderedAsync();
    Task<ProductionWorkplace?> GetByIdWithUsersAsync(int id);
    Task<List<ProductionWorkplace>> GetAllWithUsersOrderedAsync();
    Task<List<ProductionWorkplace>> GetBdeActiveAsync();
    Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId);
    Task SetProductionWorkplaceUsersAsync(int workplaceId, List<int> userIds, string createdBy, string createdByWindows);
}
