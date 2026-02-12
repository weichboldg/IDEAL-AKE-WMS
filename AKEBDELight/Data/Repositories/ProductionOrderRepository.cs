using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class ProductionOrderRepository : Repository<ProductionOrder>, IProductionOrderRepository
{
    public ProductionOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionOrder>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetOpenOrdersAsync()
    {
        return await _dbSet.Where(o => !o.IsDone).OrderBy(o => o.OrderNumber).ToListAsync();
    }

    public async Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }
}
