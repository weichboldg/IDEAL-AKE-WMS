using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Kreditoren (E8a). Ohne Caching-Decorator (kleine Tabelle, selten gelesen). Kein Delete — nur deaktivieren.</summary>
public interface ICreditorRepository
{
    Task<List<Creditor>> GetAllAsync();
    Task<Creditor?> GetByIdAsync(int id);
    Task AddAsync(Creditor creditor);
    Task UpdateAsync(Creditor creditor);

    /// <summary>App-Layer-Guard (InMemory erzwingt keine UNIQUE-Indexe): Kurzname getrimmt, Gross/Klein egal.</summary>
    Task<bool> ExistsShortNameAsync(string shortName, int? exceptId);

    /// <summary>App-Layer-Guard fuer die Sage-Kontonummer (getrimmt, exakt — Zeichenkette, nie numerisch).</summary>
    Task<bool> ExistsSageKtoAsync(string sageKto, int? exceptId);
}

public class CreditorRepository : ICreditorRepository
{
    private readonly ApplicationDbContext _db;

    public CreditorRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Creditor>> GetAllAsync() =>
        _db.Creditors.AsNoTracking().OrderBy(c => c.ShortName == null).ThenBy(c => c.ShortName).ThenBy(c => c.Name).ToListAsync();

    public Task<Creditor?> GetByIdAsync(int id) => _db.Creditors.FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Creditor creditor)
    {
        _db.Creditors.Add(creditor);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Creditor creditor)
    {
        _db.Creditors.Update(creditor);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsShortNameAsync(string shortName, int? exceptId)
    {
        var key = shortName.Trim().ToUpperInvariant();
        return await _db.Creditors.AnyAsync(c => c.ShortName != null && c.ShortName.ToUpper() == key && c.Id != exceptId);
    }

    public async Task<bool> ExistsSageKtoAsync(string sageKto, int? exceptId)
    {
        var key = sageKto.Trim();
        return await _db.Creditors.AnyAsync(c => c.SageKto == key && c.Id != exceptId);
    }
}
