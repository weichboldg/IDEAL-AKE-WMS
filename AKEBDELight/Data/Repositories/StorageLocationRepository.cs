using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class StorageLocationRepository : Repository<StorageLocation>, IStorageLocationRepository
{
    public StorageLocationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<StorageLocation>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(sl => sl.Code).ToListAsync();
    }
}
