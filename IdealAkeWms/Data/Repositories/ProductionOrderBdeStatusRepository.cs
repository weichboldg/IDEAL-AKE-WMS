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

    public async Task<int> SetIsDoneBdeForOrderNumberAsync(
        string orderNumber, bool value, string modifiedBy, string modifiedByWindows)
    {
        // Alle Sub-FAs dieser HauptFA (materialisiert: gleiche OrderNumber) -> ihre BdeStatus-Zeilen.
        // Zwei-Schritt (IDs lokal, dann Contains) statt Subquery: laeuft auch auf dem InMemory-Provider.
        var orderIds = await _context.ProductionOrders
            .Where(o => o.OrderNumber == orderNumber)
            .Select(o => o.Id)
            .ToListAsync();

        if (orderIds.Count == 0) return 0;

        var rows = await _context.ProductionOrderBdeStatuses
            .Where(s => orderIds.Contains(s.ProductionOrderId))
            .ToListAsync();

        // Ein gemeinsamer Zeitstempel fuer die ganze Kaskade (AK D — als EINE Aktion erkennbar).
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            row.IsDoneBde = value;
            row.ModifiedAt = now;
            row.ModifiedBy = modifiedBy;
            row.ModifiedByWindows = modifiedByWindows;
        }

        // EIN SaveChanges = eine Transaktion -> atomar (AK C): alle Zeilen oder keine.
        await _context.SaveChangesAsync();
        return rows.Count;
    }
}
