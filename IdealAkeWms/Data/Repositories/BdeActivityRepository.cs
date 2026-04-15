using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BdeActivityRepository : IBdeActivityRepository
{
    private readonly ApplicationDbContext _ctx;
    public BdeActivityRepository(ApplicationDbContext ctx) { _ctx = ctx; }

    public Task<List<BdeActivity>> GetAllAsync() =>
        _ctx.BdeActivities.OrderBy(a => a.Name).ToListAsync();

    public Task<List<BdeActivity>> GetAllActiveAsync() =>
        _ctx.BdeActivities.Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();

    public Task<BdeActivity?> GetByIdAsync(int id) =>
        _ctx.BdeActivities.FirstOrDefaultAsync(a => a.Id == id);

    public Task<BdeActivity?> GetByCodeAsync(string code) =>
        _ctx.BdeActivities.FirstOrDefaultAsync(a => a.Code == code && a.IsActive);

    public async Task AddAsync(BdeActivity activity)
    {
        _ctx.BdeActivities.Add(activity);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(BdeActivity activity)
    {
        _ctx.BdeActivities.Update(activity);
        await _ctx.SaveChangesAsync();
    }
}
