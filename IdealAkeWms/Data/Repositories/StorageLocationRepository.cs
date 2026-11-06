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
        // StorageArea fuer die Lagerort-Spalte der Lagerplatzliste (v1.73.0); kleine Tabelle, ein LEFT JOIN.
        return await _dbSet.Include(sl => sl.StorageArea).OrderBy(sl => sl.Code).ToListAsync();
    }

    public async Task<List<StorageLocation>> GetAllOrderedExcludingPickingTransportAsync()
    {
        return await _dbSet
            .Where(sl => !sl.IsPickingTransport)
            .OrderBy(sl => sl.Code)
            .ToListAsync();
    }

    public async Task<List<StorageLocation>> GetActiveOrderedExcludingPickingTransportAsync()
    {
        return await _dbSet
            .Where(sl => sl.IsActive && !sl.IsPickingTransport && sl.IstBuchbar)
            .OrderBy(sl => sl.Code)
            .ToListAsync();
    }

    public async Task<List<StorageLocation>> GetPickingTransportLocationsAsync()
    {
        return await _dbSet
            .Where(sl => sl.IsPickingTransport)
            .OrderBy(sl => sl.Code)
            .ToListAsync();
    }

    public async Task<List<StorageLocation>> GetActivePickingTransportLocationsAsync()
    {
        return await _dbSet
            .Where(sl => sl.IsActive && sl.IsPickingTransport && sl.IstBuchbar)
            .OrderBy(sl => sl.Code)
            .ToListAsync();
    }

    public async Task<StorageLocation?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(sl => sl.Code == code);
    }
}
