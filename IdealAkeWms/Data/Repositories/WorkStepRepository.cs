using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class WorkStepRepository : IWorkStepRepository
{
    private readonly ApplicationDbContext _context;

    public WorkStepRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkStep>> GetAllAsync()
        => await _context.WorkSteps
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Code)
            .ToListAsync();

    public async Task<List<WorkStep>> GetActiveAsync()
        => await _context.WorkSteps
            .Where(w => w.IsActive)
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Code)
            .ToListAsync();

    public Task<WorkStep?> GetByIdAsync(int id)
        => _context.WorkSteps.FirstOrDefaultAsync(w => w.Id == id);

    public Task<WorkStep?> GetByCodeAsync(string code)
        => _context.WorkSteps.FirstOrDefaultAsync(w => w.Code == code);

    public async Task AddAsync(WorkStep step)
    {
        _context.WorkSteps.Add(step);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkStep step)
    {
        _context.WorkSteps.Update(step);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsInUseAsync(int id)
        => await _context.FaWorkSteps.AnyAsync(f => f.WorkStepId == id)
           || await _context.FaAttributeWorkSteps.AnyAsync(x => x.WorkStepId == id)
           || await _context.ProductionWorkplaceWorkSteps.AnyAsync(x => x.WorkStepId == id);

    public async Task<bool> DeleteAsync(int id)
    {
        if (await IsInUseAsync(id)) return false;

        var row = await _context.WorkSteps.FindAsync(id);
        if (row == null) return false;

        _context.WorkSteps.Remove(row);
        await _context.SaveChangesAsync();
        return true;
    }
}
