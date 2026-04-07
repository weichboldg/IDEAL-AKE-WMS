using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderRepository : Repository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet
            .Include(o => o.ProductionWorkplace)
            .OrderBy(o => o.OrderNumber)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersAsync()
    {
        return await _dbSet.Where(o => !o.IsDone).OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20)
    {
        var q = _dbSet.Where(o => !o.IsDone);

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(o =>
                o.OrderNumber.Contains(query) ||
                (o.ArticleNumber != null && o.ArticleNumber.Contains(query)) ||
                (o.Customer != null && o.Customer.Contains(query)));
        }

        return await q
            .OrderBy(o => o.ProductionDate.HasValue ? 0 : 1)
            .ThenBy(o => o.ProductionDate)
            .Take(limit).ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetReleasedForPickingAsync()
    {
        return await _dbSet
            .Where(o => o.IsReleasedForPicking && !o.IsDone)
            .OrderBy(o => o.PickingPriority.HasValue ? 0 : 1)
            .ThenBy(o => o.PickingPriority)
            .ThenBy(o => o.ProductionDate)
            .ToListAsync();
    }

    public async Task<int> GetReleasedForPickingCountAsync()
    {
        return await _dbSet.CountAsync(o => o.IsReleasedForPicking && !o.IsDone);
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount)
    {
        if (weeksAhead <= 0) weeksAhead = 8;
        if (maxCount <= 0) maxCount = 200;

        var cutoff = DateTime.Now.AddDays(weeksAhead * 7);

        return await _dbSet
            .Where(po => !po.IsDone
                         && po.ProductionDate != null
                         && po.ProductionDate <= cutoff)
            .OrderBy(po => po.ProductionDate)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task SetCoatingFlagsAsync(Dictionary<int, bool> orderIdToHasCoatingParts)
    {
        if (orderIdToHasCoatingParts == null || orderIdToHasCoatingParts.Count == 0) return;

        var ids = orderIdToHasCoatingParts.Keys.ToList();
        var orders = await _dbSet
            .Where(po => ids.Contains(po.Id))
            .ToListAsync();

        foreach (var order in orders)
        {
            if (!orderIdToHasCoatingParts.TryGetValue(order.Id, out var newFlag)) continue;

            var changed = order.HasCoatingParts != newFlag;
            order.HasCoatingParts = newFlag;

            // Spec fallstrick #11: when HasCoatingParts flips to false, reset IsCoatingDone
            if (!newFlag && order.IsCoatingDone)
            {
                order.IsCoatingDone = false;
                changed = true;
            }

            if (changed)
            {
                order.ModifiedAt = DateTime.Now;
                // No ModifiedBy — sync job, not a user action
            }
        }

        await _context.SaveChangesAsync();
    }
}
