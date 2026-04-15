using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BdeTerminalRepository : IBdeTerminalRepository
{
    private readonly ApplicationDbContext _ctx;
    public BdeTerminalRepository(ApplicationDbContext ctx) { _ctx = ctx; }

    public Task<List<BdeTerminal>> GetAllAsync() =>
        _ctx.BdeTerminals
            .Include(t => t.User)
            .Include(t => t.DefaultProductionWorkplace)
            .OrderBy(t => t.DefaultProductionWorkplace.Name)
            .ToListAsync();

    public Task<BdeTerminal?> GetByIdAsync(int id) =>
        _ctx.BdeTerminals
            .Include(t => t.User)
            .Include(t => t.DefaultProductionWorkplace)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<BdeTerminal?> GetByUserIdAsync(int userId) =>
        _ctx.BdeTerminals
            .Include(t => t.DefaultProductionWorkplace)
            .FirstOrDefaultAsync(t => t.UserId == userId);

    public async Task AddAsync(BdeTerminal terminal)
    {
        _ctx.BdeTerminals.Add(terminal);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(BdeTerminal terminal)
    {
        _ctx.BdeTerminals.Update(terminal);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(BdeTerminal terminal)
    {
        _ctx.BdeTerminals.Remove(terminal);
        await _ctx.SaveChangesAsync();
    }
}
