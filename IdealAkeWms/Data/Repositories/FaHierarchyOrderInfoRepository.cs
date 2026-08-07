using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class FaHierarchyOrderInfoRepository : IFaHierarchyOrderInfoRepository
{
    private readonly ApplicationDbContext _db;

    public FaHierarchyOrderInfoRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<FaHierarchyOrderInfo>> GetByHauptFaAsync(int hauptFa)
    {
        return await _db.FaHierarchyOrderInfos
            .AsNoTracking()
            .Where(o => o.HauptFA == hauptFa)
            .OrderBy(o => o.MontageAbteilung)
            .ToListAsync();
    }

    public async Task<List<FaHierarchyOrderInfo>> GetAllAsync()
    {
        return await _db.FaHierarchyOrderInfos
            .AsNoTracking()
            .OrderBy(o => o.HauptFA)
            .ToListAsync();
    }
}
