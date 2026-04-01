using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class StorageLocationRepository : Repository<StorageLocation>, IStorageLocationRepository
{
    public StorageLocationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<StorageLocation>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(sl => sl.Code).ToListAsync();
    }

    public async Task<List<StorageLocation>> GetAllOrderedExcludingPickingTransportAsync()
    {
        return await _dbSet
            .Where(sl => !sl.IsPickingTransport)
            .OrderBy(sl => sl.Code)
            .ToListAsync();
    }

    public async Task<StorageLocation?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(sl => sl.Code == code);
    }
}
