using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionWorkplaceRepository : Repository<ProductionWorkplace>, IProductionWorkplaceRepository
{
    public ProductionWorkplaceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionWorkplace>> GetAllOrderedAsync()
    {
        return await _dbSet
            .OrderBy(w => w.Name)
            .ToListAsync();
    }
}
