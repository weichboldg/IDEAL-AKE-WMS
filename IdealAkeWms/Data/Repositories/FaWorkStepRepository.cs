using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class FaWorkStepRepository : IFaWorkStepRepository
{
    private readonly ApplicationDbContext _context;

    public FaWorkStepRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FaWorkStep>> GetByProductionOrderIdAsync(int productionOrderId, bool includeRemoved = false)
    {
        var query = _context.FaWorkSteps
            .Include(f => f.WorkStep)
            .Include(f => f.Specs)
            .Where(f => f.ProductionOrderId == productionOrderId);

        if (!includeRemoved)
            query = query.Where(f => !f.IsRemoved);

        return await query
            .OrderBy(f => f.WorkStep.SortOrder).ThenBy(f => f.WorkStep.Code)
            .ToListAsync();
    }

    public async Task<List<FaWorkStep>> GetForWorkStepAsync(int workStepId)
        => await _context.FaWorkSteps
            .Where(f => f.WorkStepId == workStepId && !f.IsRemoved)
            .ToListAsync();

    public async Task<Dictionary<int, Dictionary<string, bool>>> GetWorkStepPivotAsync(List<int> productionOrderIds)
    {
        // Chunking gegen SQL-Server-2100-Parameter-Limit (Pattern aus dem alten
        // ProductionOrderAssemblyGroupRepository.GetIsApplicablePivotAsync).
        var ids = productionOrderIds.Distinct().ToList();
        var result = new Dictionary<int, Dictionary<string, bool>>();
        if (ids.Count == 0) return result;

        const int chunkSize = 1000;
        for (int offset = 0; offset < ids.Count; offset += chunkSize)
        {
            var chunk = ids.Skip(offset).Take(chunkSize).ToList();
            var rows = await _context.FaWorkSteps
                .Where(f => chunk.Contains(f.ProductionOrderId))
                .Select(f => new { f.ProductionOrderId, f.WorkStep.Code, f.IsRemoved })
                .ToListAsync();

            foreach (var r in rows)
            {
                if (!result.TryGetValue(r.ProductionOrderId, out var dict))
                {
                    dict = new Dictionary<string, bool>();
                    result[r.ProductionOrderId] = dict;
                }
                dict[r.Code] = !r.IsRemoved;
            }
        }
        return result;
    }

    public async Task<Dictionary<int, FaWorkStepCounts>> GetCountsByProductionOrderIdsAsync(List<int> productionOrderIds)
    {
        var ids = productionOrderIds.Distinct().ToList();
        var result = new Dictionary<int, FaWorkStepCounts>();
        if (ids.Count == 0) return result;

        const int chunkSize = 1000;
        for (int offset = 0; offset < ids.Count; offset += chunkSize)
        {
            var chunk = ids.Skip(offset).Take(chunkSize).ToList();
            var rows = await _context.FaWorkSteps
                .Where(f => chunk.Contains(f.ProductionOrderId) && !f.IsRemoved)
                .Select(f => new { f.ProductionOrderId, f.IsSpecComplete, SpecCount = f.Specs.Count })
                .ToListAsync();

            foreach (var grp in rows.GroupBy(r => r.ProductionOrderId))
            {
                result[grp.Key] = new FaWorkStepCounts(
                    ActiveCount: grp.Count(),
                    SpecCompleteCount: grp.Count(r => r.IsSpecComplete),
                    SpecCount: grp.Sum(r => r.SpecCount));
            }
        }
        return result;
    }

    public async Task SetActiveAsync(int productionOrderId, int workStepId, bool active,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaWorkSteps
            .FirstOrDefaultAsync(f => f.ProductionOrderId == productionOrderId && f.WorkStepId == workStepId);

        if (row == null)
        {
            row = new FaWorkStep
            {
                ProductionOrderId = productionOrderId,
                WorkStepId = workStepId,
                IsRemoved = !active,
                Source = FaWorkStepSources.Manual,
                CreatedAt = DateTime.Now,
                CreatedBy = modifiedBy,
                CreatedByWindows = modifiedByWindows
            };
            _context.FaWorkSteps.Add(row);
        }
        else
        {
            row.IsRemoved = !active;
            row.Source = FaWorkStepSources.Manual;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = modifiedBy;
            row.ModifiedByWindows = modifiedByWindows;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SetIsCompletedAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaWorkSteps.FirstOrDefaultAsync(f => f.Id == faWorkStepId)
            ?? throw new InvalidOperationException($"FaWorkStep row missing for Id {faWorkStepId}.");

        row.IsCompleted = value;
        row.CompletedAt = value ? DateTime.Now : null;
        row.CompletedBy = value ? modifiedBy : null;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetIsSpecCompleteAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.FaWorkSteps.FirstOrDefaultAsync(f => f.Id == faWorkStepId)
            ?? throw new InvalidOperationException($"FaWorkStep row missing for Id {faWorkStepId}.");

        row.IsSpecComplete = value;
        row.SpecCompletedAt = value ? DateTime.Now : null;
        row.SpecCompletedBy = value ? modifiedBy : null;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public Task<FaWorkStep?> GetByIdAsync(int id)
        => _context.FaWorkSteps
            .Include(f => f.WorkStep)
            .FirstOrDefaultAsync(f => f.Id == id);

    public Task<FaWorkStepSpec?> GetSpecByIdAsync(int id)
        => _context.FaWorkStepSpecs
            .Include(s => s.Article)
            .Include(s => s.FaWorkStep)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddSpecAsync(FaWorkStepSpec spec)
    {
        _context.FaWorkStepSpecs.Add(spec);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSpecAsync(FaWorkStepSpec spec)
    {
        _context.FaWorkStepSpecs.Update(spec);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSpecAsync(int id)
    {
        var row = await _context.FaWorkStepSpecs.FindAsync(id);
        if (row == null) return;
        _context.FaWorkStepSpecs.Remove(row);
        await _context.SaveChangesAsync();
    }
}
