using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>EF-Implementierung von <see cref="IBeschichtungsauftragRepository"/>. Ohne Cache-Decorator: der Workflow
/// braucht fuer die Vergabe-Pruefung immer den aktuellen Stand.</summary>
public class BeschichtungsauftragRepository : IBeschichtungsauftragRepository
{
    private readonly ApplicationDbContext _db;

    public BeschichtungsauftragRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Beschichtungsauftrag?> GetMitPositionenAsync(int id) =>
        _db.Beschichtungsauftraege
            .Include(a => a.Positionen)
            .FirstOrDefaultAsync(a => a.Id == id);

    public virtual async Task<int> GetMaxLaufendeNummerAsync(int jahr) =>
        await _db.Beschichtungsauftraege
            .Where(a => a.Jahr == jahr)
            .MaxAsync(a => (int?)a.LaufendeNummer) ?? 0;

    public Task<List<Vergabe>> GetVergabenAsync(int hauptFa) => GetVergabenAsync(new[] { hauptFa });

    public async Task<List<Vergabe>> GetVergabenAsync(IReadOnlyCollection<int> hauptFas)
    {
        if (hauptFas.Count == 0) return new List<Vergabe>();
        var rows = await _db.BeschichtungsauftragPositionen
            .AsNoTracking()
            .Where(p => hauptFas.Contains(p.HauptFA) && p.Beschichtungsauftrag.Status != BeschichtungsauftragStatus.Storniert)
            .Select(p => new { p.HauptFA, p.VaterFA, p.SubFA, p.Position, p.BeschichtungsauftragId, p.Beschichtungsauftrag.Nummer })
            .ToListAsync();
        return rows.Select(r => new Vergabe(new PositionsSchluessel(r.VaterFA, r.SubFA, r.Position), r.BeschichtungsauftragId, r.Nummer, r.HauptFA))
            .ToList();
    }

    public Task<List<Beschichtungsauftrag>> GetAlleAsync() =>
        _db.Beschichtungsauftraege
            .AsNoTracking()
            .Include(a => a.Positionen)
            .Include(a => a.Ursprungsauftrag)
            .Include(a => a.Folgeauftraege)
            .AsSplitQuery()
            .ToListAsync();

    public void Add(Beschichtungsauftrag auftrag) => _db.Beschichtungsauftraege.Add(auftrag);

    public void Remove(BeschichtungsauftragPosition position) => _db.BeschichtungsauftragPositionen.Remove(position);

    // Ein Workflow-Aufruf je Request (Teil 4) — der Context traegt dann nur diesen Vorgang.
    public void VerwerfeAenderungen() => _db.ChangeTracker.Clear();

    public void SetOriginalRowVersion(Beschichtungsauftrag auftrag, byte[] rowVersion) =>
        _db.Entry(auftrag).Property(a => a.RowVersion).OriginalValue = rowVersion;

    public virtual Task SaveChangesAsync() => _db.SaveChangesAsync();

    public virtual async Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action)
    {
        if (!_db.Database.IsRelational()) return await action();

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var result = await action();
        await tx.CommitAsync();
        return result;
    }
}
