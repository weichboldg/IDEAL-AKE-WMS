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
            .Select(p => new { p.HauptFA, p.VaterFA, p.SubFA, p.Position, p.Artnr, p.PositionText, p.BeschichtungsauftragId, p.Beschichtungsauftrag.Nummer })
            .ToListAsync();
        return rows.Select(r => new Vergabe(PositionsSchluessel.Bilden(r.VaterFA, r.SubFA, r.Position, r.Artnr, r.PositionText),
                r.BeschichtungsauftragId, r.Nummer, r.HauptFA))
            .ToList();
    }

    public async Task<FlacheLackierteilDaten> GetFlacheLackierteileAsync(string? lackierteilKategorie, IReadOnlyCollection<string>? orderNumbers = null)
    {
        var q = _db.ProductionOrders.AsNoTracking()
            .Include(o => o.ProductionWorkplace)
            .Include(o => o.PickingStatus)
            .Where(ProductionOrder.IsHauptFa)
            .Where(o => !o.IsDone && !o.IsCancelled && o.PickingStatus != null && o.PickingStatus.HasCoatingParts);
        if (orderNumbers != null) q = q.Where(o => orderNumbers.Contains(o.OrderNumber));
        var orders = await q.OrderBy(o => o.OrderNumber).ToListAsync();

        var artikel = orders.Select(o => o.ArticleNumber).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
        var items = string.IsNullOrWhiteSpace(lackierteilKategorie) || artikel.Count == 0
            ? new List<FlachRoh>()
            : await (
                from h in _db.CachedBomHeaders
                join i in _db.CachedBomItems on h.Id equals i.CachedBomHeaderId
                join a in _db.Articles on i.Ressourcenummer equals a.ArticleNumber
                join c in _db.ArticleCategories on a.ArticleCategoryId equals c.Id
                join d in _db.ArticleSageDetails on a.Id equals d.ArticleId into dj
                from d in dj.DefaultIfEmpty()
                where artikel.Contains(h.Artikelnummer) && c.Name == lackierteilKategorie
                orderby i.SortOrder
                select new FlachRoh(h.Artikelnummer, h.Source, i, d == null ? null : d.Breite, d == null ? null : d.Hoehe, d == null ? null : d.Laenge)
            ).AsNoTracking().ToListAsync();
        // SQL Server vergleicht ohne Gross/Klein — die Zuordnung im Speicher ebenso.
        var jeArtikel = items.ToLookup(x => x.Artikelnummer, StringComparer.OrdinalIgnoreCase);

        var zeilen = orders.SelectMany(o => jeArtikel[o.ArticleNumber ?? ""]
            .Select(x => new FlacheLackierteilZeile(o, x.Source, x.Item, x.Breite, x.Hoehe, x.Laenge))).ToList();
        var ohne = orders.Where(o => !jeArtikel[o.ArticleNumber ?? ""].Any()).ToLookup(o => o.PickingStatus?.IsDonePicking == true);
        return new FlacheLackierteilDaten(zeilen, ohne[false].Select(o => o.OrderNumber).Distinct().ToList(),
            ohne[true].Select(o => o.OrderNumber).Distinct().Count());
    }

    private sealed record FlachRoh(string Artikelnummer, string Source, CachedBomItem Item, decimal? Breite, decimal? Hoehe, decimal? Laenge);

    public Task<Beschichtungsauftrag?> GetDetailAsync(int id) =>
        _db.Beschichtungsauftraege
            .AsNoTracking()
            .Include(a => a.Positionen)
            .Include(a => a.Ursprungsauftrag)
            .Include(a => a.Folgeauftraege)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

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
