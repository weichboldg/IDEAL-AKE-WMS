using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class HolidayRepository : Repository<Holiday>, IHolidayRepository
{
    public HolidayRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Holiday>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(h => h.Date).ToListAsync();
    }

    public async Task<HashSet<DateTime>> GetHolidayDatesAsync()
    {
        var dates = await _dbSet.Select(h => h.Date.Date).ToListAsync();
        return new HashSet<DateTime>(dates);
    }
}
