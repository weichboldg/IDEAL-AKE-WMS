using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class WorkOperationRepository : Repository<WorkOperation>, IWorkOperationRepository
{
    public WorkOperationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<WorkOperation>> GetByProductionOrderIdAsync(int productionOrderId)
    {
        return await _dbSet
            .Where(wo => wo.ProductionOrderId == productionOrderId)
            .OrderBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetByProductionOrderIdWithWorkplaceAsync(int productionOrderId)
    {
        return await _dbSet
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionOrderId == productionOrderId)
            .OrderBy(wo => wo.Sequence)
            .ToListAsync();
    }
}
