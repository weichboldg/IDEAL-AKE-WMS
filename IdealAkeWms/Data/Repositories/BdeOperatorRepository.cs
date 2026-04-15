using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BdeOperatorRepository : IBdeOperatorRepository
{
    private readonly ApplicationDbContext _ctx;
    public BdeOperatorRepository(ApplicationDbContext ctx) { _ctx = ctx; }

    public Task<List<BdeOperator>> GetAllAsync() =>
        _ctx.BdeOperators.OrderBy(o => o.LastName).ThenBy(o => o.FirstName).ToListAsync();

    public Task<List<BdeOperator>> GetAllActiveAsync() =>
        _ctx.BdeOperators.Where(o => o.IsActive).OrderBy(o => o.LastName).ThenBy(o => o.FirstName).ToListAsync();

    public Task<BdeOperator?> GetByIdAsync(int id) =>
        _ctx.BdeOperators.FirstOrDefaultAsync(o => o.Id == id);

    public Task<BdeOperator?> GetByPersonnelNumberAsync(string personnelNumber) =>
        _ctx.BdeOperators.FirstOrDefaultAsync(o => o.PersonnelNumber == personnelNumber && o.IsActive);

    public async Task AddAsync(BdeOperator op)
    {
        _ctx.BdeOperators.Add(op);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(BdeOperator op)
    {
        _ctx.BdeOperators.Update(op);
        await _ctx.SaveChangesAsync();
    }
}
