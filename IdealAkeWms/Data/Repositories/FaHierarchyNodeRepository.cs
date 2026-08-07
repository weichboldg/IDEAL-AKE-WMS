using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class FaHierarchyNodeRepository : IFaHierarchyNodeRepository
{
    private readonly ApplicationDbContext _db;

    public FaHierarchyNodeRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<FaHierarchyNode>> GetByHauptFaAsync(int hauptFa)
    {
        return await _db.FaHierarchyNodes
            .AsNoTracking()
            .Where(n => n.HauptFA == hauptFa)
            .OrderBy(n => n.VaterFA)
            .ThenBy(n => n.Position)
            .ToListAsync();
    }

    public async Task<List<FaHierarchyNode>> GetAllAsync()
    {
        return await _db.FaHierarchyNodes
            .AsNoTracking()
            .OrderBy(n => n.HauptFA)
            .ThenBy(n => n.VaterFA)
            .ThenBy(n => n.Position)
            .ToListAsync();
    }
}
