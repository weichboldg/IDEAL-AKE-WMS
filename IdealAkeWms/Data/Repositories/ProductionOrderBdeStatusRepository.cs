using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderBdeStatusRepository : IProductionOrderBdeStatusRepository
{
    private readonly ApplicationDbContext _context;

    public ProductionOrderBdeStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProductionOrderBdeStatus?> GetByProductionOrderIdAsync(int productionOrderId)
        => _context.ProductionOrderBdeStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId);

    public async Task SetIsDoneBdeAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderBdeStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"BdeStatus row missing for FA {productionOrderId}.");

        row.IsDoneBde = value;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
