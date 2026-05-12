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

    public async Task<List<ProductionOrder>> GetByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (articleNumbers == null || articleNumbers.Count == 0)
            return new List<ProductionOrder>();

        return await _dbSet
            .AsNoTracking()
            .Where(o => o.ArticleNumber != null && articleNumbers.Contains(o.ArticleNumber))
            .OrderBy(o => o.ProductionDate)
            .ToListAsync();
    }
}
