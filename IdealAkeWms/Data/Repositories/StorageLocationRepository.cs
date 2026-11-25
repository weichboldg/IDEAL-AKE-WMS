using System.Globalization;
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

    public Task<List<StorageArea>> GetStorageAreasAsync() =>
        _context.StorageAreas.AsNoTracking().OrderBy(a => a.Lagerkennung).ToListAsync();

    public Task<List<string>> GetAllCodesAsync() => _dbSet.AsNoTracking().Select(l => l.Code).ToListAsync();

    public async Task<Dictionary<int, SageStammdatenOutboxItem>> GetOffeneLagerUebergabenAsync()
    {
        var eintraege = await _context.SageStammdatenOutbox.AsNoTracking()
            .Where(o => o.EntityType == SageStammdatenOutbox.TypLagerort && o.Status != SageBookingQueueStatus.Bestaetigt)
            .OrderBy(o => o.Id).ToListAsync();
        var ergebnis = new Dictionary<int, SageStammdatenOutboxItem>();
        foreach (var o in eintraege)
            if (int.TryParse(o.EntityKey, NumberStyles.None, CultureInfo.InvariantCulture, out var id)) ergebnis[id] = o;
        return ergebnis;
    }

    /// <summary>Muster BfAuftragRepository.InSerializableTransactionAsync: unter InMemory ohne Transaktion (IsRelational-Guard),
    /// ReadCommitted genuegt (Unique-Indizes Code/Lagerkennung schuetzen gegen gleichzeitige Anwender).</summary>
    public async Task AddSageAnlageAsync(StorageArea? neuesLager, IReadOnlyList<StorageLocation> plaetze)
    {
        await using var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            if (neuesLager is not null) _context.StorageAreas.Add(neuesLager);
            foreach (var p in plaetze)
            {
                if (neuesLager is not null) p.StorageArea = neuesLager;
                _dbSet.Add(p);
            }
            await _context.SaveChangesAsync(); // Ids fuer den Outbox-Schluessel
            if (neuesLager is not null)
                SageStammdatenOutbox.Vormerken(_context, SageStammdatenOutbox.TypLagerort, neuesLager,
                    new { neuesLager.Id, neuesLager.Lagerkennung, neuesLager.Bezeichnung, neuesLager.Lagertechnik });
            foreach (var p in plaetze)
            {
                SageLagerplatzAnlageCode(p, out var lager, out var d1, out var d2, out var d3);
                SageStammdatenOutbox.Vormerken(_context, SageStammdatenOutbox.TypLagerplatz, p,
                    new { p.Id, p.Code, Lagerkennung = lager, D1 = d1, D2 = d2, D3 = d3 });
            }
            await _context.SaveChangesAsync();
            if (tx is not null) await tx.CommitAsync();
        }
        catch
        {
            // nichts angelegt: der Kontext darf die verworfenen Entitaeten nicht beim naechsten SaveChanges erneut schreiben
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private static void SageLagerplatzAnlageCode(StorageLocation p, out string lager, out int d1, out int d2, out int d3)
    {
        lager = p.Zone ?? "";
        Services.SageLagerplatzAnlage.TryParseCode(p.Code, lager, out d1, out d2, out d3);
    }

    public async Task<bool> ErneutSendenAsync(string typ, int id, string modifiedBy, string modifiedByWindows)
    {
        var key = id.ToString(CultureInfo.InvariantCulture);
        var eintraege = await _context.SageStammdatenOutbox
            .Where(o => o.EntityType == typ && o.EntityKey == key && o.Status == SageBookingQueueStatus.Fehler).ToListAsync();
        if (eintraege.Count == 0) return false;
        var jetzt = DateTime.Now;
        foreach (var o in eintraege)
        {
            o.Status = SageBookingQueueStatus.Offen;
            o.AttemptCount = 0;
            o.LastError = null;
            o.ModifiedAt = jetzt; o.ModifiedBy = modifiedBy; o.ModifiedByWindows = modifiedByWindows;
        }
        if (typ == SageStammdatenOutbox.TypLagerplatz && await _dbSet.FindAsync(id) is { } platz
            && platz.SageUebergabeStatus == SageUebergabeStatus.Fehler)
        {
            platz.SageUebergabeStatus = SageUebergabeStatus.Offen;
            platz.SageUebergabeFehler = null;
            platz.ModifiedAt = jetzt; platz.ModifiedBy = modifiedBy; platz.ModifiedByWindows = modifiedByWindows;
        }
        await _context.SaveChangesAsync();
        return true;
    }
}
