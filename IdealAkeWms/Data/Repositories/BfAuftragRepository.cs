using System.Data;
using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>EF-Implementierung von <see cref="IBfAuftragRepository"/>; ohne Cache (Nummernvergabe braucht den aktuellen Stand).</summary>
public class BfAuftragRepository : IBfAuftragRepository
{
    private const string Sage = ArticleAttributeDefinition.SyncSourceSage;
    private static readonly string[] Felder = BfArtikelFelder.SageFelder.Append(BfArtikelFelder.Bestellmenge).ToArray();

    private readonly ApplicationDbContext _db;

    public BfAuftragRepository(ApplicationDbContext db) => _db = db;

    public Task<BfAuftrag?> GetEntwurfAsync(int userId) =>
        _db.BfAuftraege.Include(a => a.Positionen)
            .Where(a => a.Status == BfAuftragStatus.Entwurf && a.BestellerUserId == userId)
            .OrderBy(a => a.Id).FirstOrDefaultAsync();

    public Task<BfAuftrag?> GetMitPositionenAsync(int id) =>
        _db.BfAuftraege.Include(a => a.Positionen).FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<BfAuftrag>> GetMehrereAsync(IReadOnlyCollection<int> ids) =>
        _db.BfAuftraege.Where(a => ids.Contains(a.Id)).ToListAsync();

    public Task<BfAuftrag?> GetDetailAsync(int id) =>
        _db.BfAuftraege.AsNoTracking().Include(a => a.Positionen).Include(a => a.OrderRecipientGroup)
            .FirstOrDefaultAsync(a => a.Id == id);

    public Task<List<BfAuftrag>> GetAlleAsync() =>
        _db.BfAuftraege.AsNoTracking().Include(a => a.Positionen).Where(a => a.Nummer != null).ToListAsync();

    public virtual async Task<int> GetMaxLaufendeNummerAsync()
    {
        var max = await _db.BfAuftraege.Where(a => a.Nummer != null).MaxAsync(a => a.Nummer);
        return max is { Length: > 2 } && int.TryParse(max[2..], NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    public async Task<List<BfArtikel>> SucheArtikelAsync(string? q, int limit)
    {
        var query = Lagerteile();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(a => a.ArticleNumber.Contains(t) || (a.Matchcode != null && a.Matchcode.Contains(t))
                                     || (a.Description != null && a.Description.Contains(t)));
        }
        return await LadenAsync(query.OrderBy(a => a.ArticleNumber).Take(Math.Clamp(limit, 1, 100)));
    }

    public async Task<BfArtikel?> GetArtikelAsync(string articleNumber) =>
        (await LadenAsync(Lagerteile().Where(a => a.ArticleNumber == articleNumber))).FirstOrDefault();

    /// <summary>Filter in SQL (Spec: nicht im Speicher nach <c>limit*5</c> wie die allgemeine Artikelsuche).</summary>
    private IQueryable<Article> Lagerteile()
    {
        const string ist = BfArtikelFelder.IstLagerartikel, typ = BfArtikelFelder.Artikeltyp;
        var arten = BfArtikelFelder.Teilearten;
        return _db.Articles.AsNoTracking()
            .Where(a => a.AttributeValues.Any(v => v.ArticleAttributeDefinition.SyncSource == Sage && v.ArticleAttributeDefinition.SyncFieldName == ist
                                                   && v.TextValue != null && v.TextValue != "" && v.TextValue != "0")
                        && a.AttributeValues.Any(v => v.ArticleAttributeDefinition.SyncSource == Sage && v.ArticleAttributeDefinition.SyncFieldName == typ
                                                      && arten.Contains(v.TextValue!)));
    }

    private static async Task<List<BfArtikel>> LadenAsync(IQueryable<Article> query)
    {
        var felder = Felder;
        var rows = await query.Select(a => new
        {
            a.ArticleNumber, a.Description, a.Matchcode, a.SagePrimaryStorageLocation,
            Lagerplatz = a.PrimaryStorageLocation != null ? a.PrimaryStorageLocation.Code : null,
            Bestellmenge = a.SageDetail != null ? a.SageDetail.Bestellmenge : null,
            Werte = a.AttributeValues.Where(v => v.ArticleAttributeDefinition.SyncSource == Sage && felder.Contains(v.ArticleAttributeDefinition.SyncFieldName!))
                .Select(v => new { v.ArticleAttributeDefinition.SyncFieldName, v.TextValue }).ToList(),
        }).ToListAsync();
        return rows.Select(r =>
        {
            var w = r.Werte.GroupBy(x => x.SyncFieldName!).ToDictionary(g => g.Key, g => g.First().TextValue?.Trim());
            string? F(string f) => w.TryGetValue(f, out var v) && !string.IsNullOrEmpty(v) ? v : null;
            // Spec „eine Stelle“: SageDetail.Bestellmenge (V-A) vor dem Merkmal (V-B, schon nach der Einheitenregel gefiltert).
            var menge = r.Bestellmenge
                        ?? (decimal.TryParse(F(BfArtikelFelder.Bestellmenge), NumberStyles.Number, CultureInfo.InvariantCulture, out var m) ? m : null);
            return new BfArtikel(r.ArticleNumber, r.Description, r.Matchcode, F(BfArtikelFelder.Artikeltyp), F(BfArtikelFelder.Material),
                F(BfArtikelFelder.Materialnummer), F(BfArtikelFelder.FertigungsInfoPN), r.Lagerplatz ?? r.SagePrimaryStorageLocation, menge,
                F(BfArtikelFelder.Arbeitsschritte));
        }).ToList();
    }

    public async Task<string?> GetBenutzerKuerzelAsync(int? userId) => userId is int id
        ? await _db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Kuerzel).FirstOrDefaultAsync()
        : null;

    public void Add(BfAuftrag auftrag) => _db.BfAuftraege.Add(auftrag);
    public void Remove(BfAuftragPosition position) => _db.BfAuftragPositionen.Remove(position);
    public void VerwerfeAenderungen() => _db.ChangeTracker.Clear();
    public void SetOriginalRowVersion(BfAuftrag auftrag, byte[] rowVersion) =>
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
