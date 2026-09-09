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

    /// <summary>
    /// Alle Auftragskoepfe zu einer Menge von HauptFA-Nummern — EINE Abfrage je angezeigter
    /// Seite (Fan-out-Vermeidung). Liefert bei Kombigeraeten bewusst MEHRERE Zeilen je HauptFA.
    /// </summary>
    public async Task<List<FaHierarchyOrderInfo>> GetByHauptFaKeysAsync(IReadOnlyCollection<int> hauptFaKeys)
    {
        if (hauptFaKeys == null || hauptFaKeys.Count == 0) return new List<FaHierarchyOrderInfo>();
        var keys = hauptFaKeys.Distinct().ToList();
        return await _db.FaHierarchyOrderInfos.AsNoTracking()
            .Where(o => keys.Contains(o.HauptFA))
            .OrderBy(o => o.HauptFA).ThenBy(o => o.MontageAbteilung)
            .ToListAsync();
    }
}
