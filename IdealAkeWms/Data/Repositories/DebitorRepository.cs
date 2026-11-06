using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Debitoren (E8b), Muster <see cref="ICreditorRepository"/>. Ohne Caching-Decorator. Kein Delete — nur deaktivieren.</summary>
public interface IDebitorRepository
{
    Task<List<Debitor>> GetAllAsync();
    Task<Debitor?> GetByIdAsync(int id);
    Task AddAsync(Debitor debitor);
    Task UpdateAsync(Debitor debitor);

    /// <summary>App-Layer-Guard (InMemory erzwingt keine UNIQUE-Indexe): Kurzname getrimmt, Gross/Klein egal.</summary>
    Task<bool> ExistsShortNameAsync(string shortName, int? exceptId);

    /// <summary>App-Layer-Guard fuer die Sage-Kontonummer (getrimmt, exakt — Zeichenkette, nie numerisch).</summary>
    Task<bool> ExistsSageKtoAsync(string sageKto, int? exceptId);
}

public class DebitorRepository : IDebitorRepository
{
    private readonly ApplicationDbContext _db;

    public DebitorRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Debitor>> GetAllAsync() =>
        _db.Debitors.AsNoTracking().OrderBy(d => d.ShortName == null).ThenBy(d => d.ShortName).ThenBy(d => d.Name).ToListAsync();

    public Task<Debitor?> GetByIdAsync(int id) => _db.Debitors.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Debitor debitor)
    {
        _db.Debitors.Add(debitor);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Debitor debitor)
    {
        _db.Debitors.Update(debitor);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsShortNameAsync(string shortName, int? exceptId)
    {
        var key = shortName.Trim().ToUpperInvariant();
        return await _db.Debitors.AnyAsync(d => d.ShortName != null && d.ShortName.ToUpper() == key && d.Id != exceptId);
    }

    public async Task<bool> ExistsSageKtoAsync(string sageKto, int? exceptId)
    {
        var key = sageKto.Trim();
        return await _db.Debitors.AnyAsync(d => d.SageKto == key && d.Id != exceptId);
    }
}
