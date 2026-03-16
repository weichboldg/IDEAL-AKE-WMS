using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionWorkplaceRepository : IRepository<ProductionWorkplace>
{
    Task<List<ProductionWorkplace>> GetAllOrderedAsync();
    Task<ProductionWorkplace?> GetByIdWithUsersAsync(int id);
    Task<List<ProductionWorkplace>> GetAllWithUsersOrderedAsync();
    Task SetProductionWorkplaceUsersAsync(int workplaceId, List<int> userIds, string createdBy, string createdByWindows);
}
