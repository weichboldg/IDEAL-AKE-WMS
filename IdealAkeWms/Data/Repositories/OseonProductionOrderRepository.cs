using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class OseonProductionOrderRepository : Repository<OseonProductionOrder>, IOseonProductionOrderRepository
{
    public OseonProductionOrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<OseonProductionOrder>> GetAllWithOperationsAsync()
    {
        return await _dbSet
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .OrderBy(o => o.CustomerOrderNumber)
            .ThenBy(o => o.OseonOrderNumber)
            .ToListAsync();
    }

    public async Task<List<OseonProductionOrder>> GetByCustomerOrderNumberAsync(string customerOrderNumber)
    {
        return await _dbSet
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .Where(o => o.CustomerOrderNumber == customerOrderNumber)
            .OrderBy(o => o.OseonOrderNumber)
            .ToListAsync();
    }

    public async Task<OseonProductionOrder?> GetByOseonIdAsync(long oseonId)
    {
        return await _dbSet.FirstOrDefaultAsync(o => o.OseonId == oseonId);
    }

    public async Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize)
    {
        // Basis-Query mit Such- und Werkbank-Filter (OHNE Status-Filter)
        var baseQuery = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            baseQuery = baseQuery.Where(o =>
                (o.CustomerOrderNumber != null && o.CustomerOrderNumber.Contains(term)) ||
                o.OseonOrderNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(workplaceName))
            baseQuery = baseQuery.Where(o => o.WorkplaceName != null && o.WorkplaceName == workplaceName);

        // Gruppen-Keys ermitteln:
        // showFinished=false → nur Gruppen die mindestens einen OFFENEN Auftrag haben
        // showFinished=true  → alle Gruppen
        var groupKeyQuery = showFinished
            ? baseQuery
            : baseQuery.Where(o => o.OseonStatus != 90 && o.OseonStatus != 95);

        // CustomerOrderNumber als Gruppierungsschlüssel (Fallback OseonOrderNumber)
        var distinctKeysQuery = groupKeyQuery
            .Select(o => o.CustomerOrderNumber ?? o.OseonOrderNumber)
            .Distinct();

        var totalGroupCount = await distinctKeysQuery.CountAsync();

        var pagedGroupKeys = await distinctKeysQuery
            .OrderBy(k => k)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        if (pagedGroupKeys.Count == 0)
        {
            return new OseonPagedResult
            {
                Items = new List<OseonProductionOrder>(),
                TotalGroupCount = totalGroupCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // ALLE Orders dieser Gruppen laden (inkl. fertige Sub-Aufträge!)
        // So sehen wir z.B. "1/14 fertig" korrekt und fertige Sub-Orders werden angezeigt
        var items = await _dbSet
            .Include(o => o.WorkOperations)
            .Include(o => o.ProductionWorkplace)
            .Where(o => pagedGroupKeys.Contains(o.CustomerOrderNumber ?? o.OseonOrderNumber))
            .OrderBy(o => o.CustomerOrderNumber)
            .ThenBy(o => o.OseonOrderNumber)
            .ToListAsync();

        return new OseonPagedResult
        {
            Items = items,
            TotalGroupCount = totalGroupCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
