using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionWorkplaceRepository : Repository<ProductionWorkplace>, IProductionWorkplaceRepository
{
    public ProductionWorkplaceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionWorkplace>> GetAllOrderedAsync()
    {
        return await _dbSet
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<ProductionWorkplace?> GetByIdWithUsersAsync(int id)
    {
        return await _dbSet
            .Include(w => w.ProductionWorkplaceUsers)
                .ThenInclude(wu => wu.User)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<List<ProductionWorkplace>> GetAllWithUsersOrderedAsync()
    {
        return await _dbSet
            .Include(w => w.ProductionWorkplaceUsers)
                .ThenInclude(wu => wu.User)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<ProductionWorkplace>> GetBdeActiveAsync()
    {
        return await _dbSet
            .Where(w => w.BdeAktiv)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId)
    {
        return await _context.ProductionWorkplaceUsers
            .AsNoTracking()
            .Where(wu => wu.UserId == userId)
            .Select(wu => wu.ProductionWorkplace)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task SetProductionWorkplaceUsersAsync(int workplaceId, List<int> userIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.ProductionWorkplaceUsers
            .Where(wu => wu.ProductionWorkplaceId == workplaceId)
            .ToListAsync();

        _context.ProductionWorkplaceUsers.RemoveRange(existing);

        foreach (var userId in userIds)
        {
            _context.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
            {
                ProductionWorkplaceId = workplaceId,
                UserId = userId,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<int>> GetWorkStepIdsAsync(int workplaceId)
    {
        return await _context.ProductionWorkplaceWorkSteps
            .AsNoTracking()
            .Where(ws => ws.ProductionWorkplaceId == workplaceId)
            .Select(ws => ws.WorkStepId)
            .ToListAsync();
    }

    public async Task SetWorkStepsAsync(int workplaceId, List<int> workStepIds, string createdBy = "system", string createdByWindows = "system")
    {
        var existing = await _context.ProductionWorkplaceWorkSteps
            .Where(ws => ws.ProductionWorkplaceId == workplaceId)
            .ToListAsync();

        // Delta-Sync: ueberzaehlige entfernen (bestehende Zeilen behalten ihre Audit-Daten) ...
        var toRemove = existing.Where(ws => !workStepIds.Contains(ws.WorkStepId)).ToList();
        _context.ProductionWorkplaceWorkSteps.RemoveRange(toRemove);

        // ... fehlende adden
        var existingIds = existing.Select(ws => ws.WorkStepId).ToHashSet();
        foreach (var workStepId in workStepIds.Distinct().Where(id => !existingIds.Contains(id)))
        {
            _context.ProductionWorkplaceWorkSteps.Add(new ProductionWorkplaceWorkStep
            {
                ProductionWorkplaceId = workplaceId,
                WorkStepId = workStepId,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }
}
