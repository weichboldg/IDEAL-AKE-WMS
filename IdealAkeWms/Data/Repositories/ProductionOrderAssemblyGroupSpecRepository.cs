using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderAssemblyGroupSpecRepository : IProductionOrderAssemblyGroupSpecRepository
{
    private readonly ApplicationDbContext _context;

    public ProductionOrderAssemblyGroupSpecRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProductionOrderAssemblyGroupSpec?> GetByIdAsync(int id)
        => _context.ProductionOrderAssemblyGroupSpecs
            .Include(s => s.Article)
            .Include(s => s.AssemblyGroup)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<ProductionOrderAssemblyGroupSpec>> GetByAssemblyGroupIdAsync(int assemblyGroupId)
        => await _context.ProductionOrderAssemblyGroupSpecs
            .Include(s => s.Article)
            .Where(s => s.AssemblyGroupId == assemblyGroupId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync();

    public async Task<Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>>
        GetByAssemblyGroupIdsAsync(IEnumerable<int> assemblyGroupIds)
    {
        var ids = assemblyGroupIds.Distinct().ToList();
        var result = new Dictionary<int, List<ProductionOrderAssemblyGroupSpec>>();
        if (ids.Count == 0) return result;

        // Chunking analog zu Phase-1-Pivot (Spec 6) — defensive Versicherung gegen 2100-Parameter-Limit
        const int chunkSize = 1000;
        var rows = new List<ProductionOrderAssemblyGroupSpec>();
        for (int offset = 0; offset < ids.Count; offset += chunkSize)
        {
            var chunk = ids.Skip(offset).Take(chunkSize).ToList();
            var batch = await _context.ProductionOrderAssemblyGroupSpecs
                .Include(s => s.Article)
                .Where(s => chunk.Contains(s.AssemblyGroupId))
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .ToListAsync();
            rows.AddRange(batch);
        }

        foreach (var grp in rows.GroupBy(s => s.AssemblyGroupId))
            result[grp.Key] = grp.ToList();

        return result;
    }

    public async Task<int> AddAsync(ProductionOrderAssemblyGroupSpec spec)
    {
        _context.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await _context.SaveChangesAsync();
        return spec.Id;
    }

    public async Task UpdateAsync(ProductionOrderAssemblyGroupSpec spec)
    {
        _context.ProductionOrderAssemblyGroupSpecs.Update(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var row = await _context.ProductionOrderAssemblyGroupSpecs.FindAsync(id);
        if (row == null) return;
        _context.ProductionOrderAssemblyGroupSpecs.Remove(row);
        await _context.SaveChangesAsync();
    }
}
