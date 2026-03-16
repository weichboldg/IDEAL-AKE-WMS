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

    public async Task<List<WorkOperation>> GetAllWithOrderAndWorkplaceAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetByWorkplaceIdAsync(int workplaceId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId)
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public async Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId && !wo.IsReported)
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }
}
