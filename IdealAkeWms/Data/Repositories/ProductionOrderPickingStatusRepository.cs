using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionOrderPickingStatusRepository : IProductionOrderPickingStatusRepository
{
    private static readonly HashSet<string> ToggleableFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone", "IsDonePicking"
    ];

    private readonly ApplicationDbContext _context;

    public ProductionOrderPickingStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int productionOrderId)
        => _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId);

    public async Task<Dictionary<int, ProductionOrderPickingStatus>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds)
    {
        var ids = productionOrderIds.ToList();
        var rows = await _context.ProductionOrderPickingStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .ToListAsync();
        return rows.ToDictionary(s => s.ProductionOrderId);
    }

    public async Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows)
    {
        if (!ToggleableFields.Contains(field))
            throw new ArgumentException($"Field '{field}' is not toggleable.", nameof(field));

        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        switch (field)
        {
            case "HasGlass": row.HasGlass = value; break;
            case "HasExternalPurchase": row.HasExternalPurchase = value; break;
            case "IsCoatingDone": row.IsCoatingDone = value; break;
            case "IsDonePicking": row.IsDonePicking = value; break;
        }

        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.IsReleasedForPicking = released;
        if (released)
        {
            row.ReleasedAt = DateTime.UtcNow;
            row.ReleasedBy = releasedBy;
            if (priority.HasValue) row.PickingPriority = priority;
        }
        else
        {
            row.AssignedPickerId = null;
            row.AssignedPickerName = null;
        }

        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.AssignedPickerId = pickerId;
        row.AssignedPickerName = pickerName;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetPickingStatusTextAsync(int productionOrderId, string? statusText,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.PickingStatus = statusText;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public async Task SetPriorityAsync(int productionOrderId, int? priority,
        string modifiedBy, string modifiedByWindows)
    {
        var row = await _context.ProductionOrderPickingStatuses
            .FirstOrDefaultAsync(s => s.ProductionOrderId == productionOrderId)
            ?? throw new InvalidOperationException($"PickingStatus row missing for FA {productionOrderId}.");

        row.PickingPriority = priority;
        row.ModifiedAt = DateTime.Now;
        row.ModifiedBy = modifiedBy;
        row.ModifiedByWindows = modifiedByWindows;
        await _context.SaveChangesAsync();
    }

    public Task SetIsDonePickingAsync(int productionOrderId, bool value,
        string modifiedBy, string modifiedByWindows)
        => SetFieldAsync(productionOrderId, "IsDonePicking", value, modifiedBy, modifiedByWindows);

    public async Task SetCoatingPartsAsync(Dictionary<int, bool> orderIdToHasCoatingParts)
    {
        if (orderIdToHasCoatingParts == null || orderIdToHasCoatingParts.Count == 0) return;

        var ids = orderIdToHasCoatingParts.Keys.ToList();
        var rows = await _context.ProductionOrderPickingStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .ToListAsync();

        foreach (var row in rows)
        {
            if (!orderIdToHasCoatingParts.TryGetValue(row.ProductionOrderId, out var newFlag)) continue;

            var changed = row.HasCoatingParts != newFlag;
            row.HasCoatingParts = newFlag;

            // Fallstrick #11: HasCoatingParts → false ⇒ IsCoatingDone reset
            if (!newFlag && row.IsCoatingDone)
            {
                row.IsCoatingDone = false;
                changed = true;
            }

            if (changed)
            {
                row.ModifiedAt = DateTime.Now;
                // ModifiedBy bleibt leer — Sync-Job, kein User
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<ProductionOrder>> GetReleasedForPickingAsync()
    {
        return await _context.ProductionOrders
            .Where(p => p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking && !p.IsDone && !p.PickingStatus.IsDonePicking)
            .Include(p => p.ProductionWorkplace)
            .Include(p => p.PickingStatus)
            .OrderBy(p => p.PickingStatus!.PickingPriority.HasValue ? 0 : 1)
            .ThenBy(p => p.PickingStatus!.PickingPriority)
            .ThenBy(p => p.ProductionDate)
            .ToListAsync();
    }

    public async Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId)
    {
        return await _context.ProductionOrders
            .Where(p => p.PickingStatus != null
                        && p.PickingStatus.IsReleasedForPicking
                        && !p.IsDone
                        && !p.PickingStatus.IsDonePicking
                        && p.PickingStatus.AssignedPickerId == pickerId)
            .Include(p => p.ProductionWorkplace)
            .Include(p => p.PickingStatus)
            .OrderBy(p => p.PickingStatus!.PickingPriority.HasValue ? 0 : 1)
            .ThenBy(p => p.PickingStatus!.PickingPriority)
            .ThenBy(p => p.ProductionDate)
            .ToListAsync();
    }

    public Task<int> GetReleasedForPickingCountAsync()
        => _context.ProductionOrderPickingStatuses
            .CountAsync(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone && !s.IsDonePicking);

    public async Task<int> GetMaxPickingPriorityAsync(int? excludeProductionOrderId = null)
    {
        var q = _context.ProductionOrderPickingStatuses
            .Where(s => s.IsReleasedForPicking && !s.ProductionOrder.IsDone && s.PickingPriority != null);

        if (excludeProductionOrderId.HasValue)
            q = q.Where(s => s.ProductionOrderId != excludeProductionOrderId.Value);

        return await q.MaxAsync(s => (int?)s.PickingPriority) ?? 0;
    }

    public async Task<BulkReleaseResult> SetReleaseBatchAsync(
        IEnumerable<int> productionOrderIds,
        bool release,
        int? assignedPickerId,
        string? assignedPickerName,
        string? releasedBy,
        string modifiedBy,
        string modifiedByWindows)
    {
        var result = new BulkReleaseResult();
        var ids = productionOrderIds.Distinct().ToList();
        if (ids.Count == 0) return result;

        var rows = await _context.ProductionOrderPickingStatuses
            .Include(s => s.ProductionOrder)
            .Where(s => ids.Contains(s.ProductionOrderId))
            .ToListAsync();

        var maxPrio = 0;
        if (release)
            maxPrio = await GetMaxPickingPriorityAsync();

        var now = DateTime.UtcNow;

        foreach (var row in rows)
        {
            if (row.ProductionOrder == null) continue;

            if (release && string.IsNullOrEmpty(row.ProductionOrder.ArticleNumber))
            {
                result.SkippedNoArticle.Add(row.ProductionOrder.OrderNumber);
                continue;
            }

            row.IsReleasedForPicking = release;
            if (release)
            {
                row.ReleasedAt = now;
                row.ReleasedBy = releasedBy;
                if (!row.PickingPriority.HasValue) row.PickingPriority = ++maxPrio;

                if (assignedPickerId.HasValue)
                {
                    row.AssignedPickerId = assignedPickerId;
                    row.AssignedPickerName = assignedPickerName;
                }
            }
            else
            {
                row.AssignedPickerId = null;
                row.AssignedPickerName = null;
            }

            row.ModifiedAt = now;
            row.ModifiedBy = modifiedBy;
            row.ModifiedByWindows = modifiedByWindows;
            result.Processed++;
        }

        if (result.Processed > 0)
            await _context.SaveChangesAsync();

        return result;
    }
}
