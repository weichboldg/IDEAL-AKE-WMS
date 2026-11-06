using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class StorageLocationRepository : Repository<StorageLocation>, IStorageLocationRepository
{
    public StorageLocationRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// E9/F11: steht der neue Platz auf Uebergabe <c>Offen</c> (im WMS angelegt), wird ein Outbox-Eintrag vorgemerkt – nicht gesendet.
    /// ponytail: zweites SaveChanges, weil der Outbox-Schluessel die WMS-Id ist (erst nach dem Insert bekannt). Scheitert es, steht
    /// der Platz trotzdem in „Offene Übergaben“ (die Liste liest den Status am Platz); ein kuenftiger Worker legt fehlende Eintraege nach.
    /// </summary>
    public override async Task<StorageLocation> AddAsync(StorageLocation entity)
    {
        await base.AddAsync(entity);
        if (entity.SageUebergabeStatus == SageUebergabeStatus.Offen)
        {
            SageStammdatenOutbox.Vormerken(_context, SageStammdatenOutbox.TypLagerplatz, entity,
                new { entity.Id, entity.Code, entity.Description, entity.Zone });
            await _context.SaveChangesAsync();
        }
        return entity;
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
