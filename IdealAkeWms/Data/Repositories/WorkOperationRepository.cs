using IdealAkeWms.Models;
using IdealAkeWms.Services;
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

    public async Task<List<WorkOperation>> GetOpenByWorkplaceIdAsync(int workplaceId, bool excludePackedOrders = false)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(wo => wo.ProductionOrder)
            .Include(wo => wo.ProductionWorkplace)
            .Where(wo => wo.ProductionWorkplaceId == workplaceId && !wo.IsReported);

        if (excludePackedOrders)
        {
            // Fold 2 (Spec §10.6): Filter IN der EF-Query (die Methode laedt ExtraInfo
            // nicht — kein nachgelagerter In-Memory-Filter moeglich). Ausgeschriebenes
            // Null-Guard-Praedikat, InMemory-kompatibel (kein statischer Helper in EF).
            query = query.Where(wo => wo.ProductionOrder.ExtraInfo == null
                || wo.ProductionOrder.ExtraInfo.SageStatus == null
                || (wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                    && wo.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt));
        }

        return await query
            .OrderBy(wo => wo.ProductionOrder.OrderNumber)
            .ThenBy(wo => wo.Sequence)
            .ToListAsync();
    }

    public Task<WorkOperation?> GetByFaAndOperationAsync(string faNumber, string operationNumber) =>
        _dbSet
            .Include(w => w.ProductionOrder)
            .Include(w => w.ProductionWorkplace)
            .FirstOrDefaultAsync(w =>
                w.ProductionOrder.OrderNumber == faNumber &&
                w.OperationNumber == operationNumber);
}
