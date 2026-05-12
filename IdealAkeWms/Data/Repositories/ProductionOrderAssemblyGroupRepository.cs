using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderAssemblyGroupRepository : IProductionOrderAssemblyGroupRepository
{
    private static readonly HashSet<string> AllowedGroupKeys = ["VK", "VL", "VE", "VT", "VA"];

    private readonly ApplicationDbContext _context;

    public ProductionOrderAssemblyGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductionOrderAssemblyGroup>> GetByProductionOrderIdAsync(int productionOrderId)
        => await _context.ProductionOrderAssemblyGroups
            .Where(g => g.ProductionOrderId == productionOrderId)
            .ToListAsync();

    public Task<ProductionOrderAssemblyGroup?> GetByPoAndKeyAsync(int productionOrderId, string groupKey)
        => _context.ProductionOrderAssemblyGroups
            .FirstOrDefaultAsync(g => g.ProductionOrderId == productionOrderId && g.GroupKey == groupKey);

    public async Task<Dictionary<int, Dictionary<string, bool>>> GetIsApplicablePivotAsync(IEnumerable<int> productionOrderIds)
    {
        // Round-4: Chunking gegen SQL-Server-2100-Parameter-Limit (Spec 7.3).
        // IN-Query mit > 2000 IDs schlaegt fehl. 1000 IDs/Chunk ist defensiv-konservativ.
        var ids = productionOrderIds.Distinct().ToList();
        var result = new Dictionary<int, Dictionary<string, bool>>();
        if (ids.Count == 0) return result;

        const int chunkSize = 1000;
        for (int offset = 0; offset < ids.Count; offset += chunkSize)
        {
            var chunk = ids.Skip(offset).Take(chunkSize).ToList();
            var rows = await _context.ProductionOrderAssemblyGroups
                .Where(g => chunk.Contains(g.ProductionOrderId))
                .Select(g => new { g.ProductionOrderId, g.GroupKey, g.IsApplicable })
                .ToListAsync();

            foreach (var r in rows)
            {
                if (!result.TryGetValue(r.ProductionOrderId, out var dict))
                {
                    dict = new Dictionary<string, bool>();
                    result[r.ProductionOrderId] = dict;
                }
                dict[r.GroupKey] = r.IsApplicable;
            }
        }
        return result;
    }

    public async Task SetIsApplicableAsync(int productionOrderId, string groupKey, bool value,
        string modifiedBy, string modifiedByWindows)
    {
        if (!AllowedGroupKeys.Contains(groupKey))
            throw new ArgumentException($"GroupKey '{groupKey}' is not in whitelist.", nameof(groupKey));

        var row = await _context.ProductionOrderAssemblyGroups
            .FirstOrDefaultAsync(g => g.ProductionOrderId == productionOrderId && g.GroupKey == groupKey)
            ?? throw new InvalidOperationException($"AssemblyGroup row missing for FA {productionOrderId} / {groupKey}.");

        row.IsApplicable = value;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }
}
