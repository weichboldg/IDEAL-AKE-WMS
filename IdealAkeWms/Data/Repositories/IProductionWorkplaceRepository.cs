using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionWorkplaceRepository : IRepository<ProductionWorkplace>
{
    Task<List<ProductionWorkplace>> GetAllOrderedAsync();
}
