using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BomCacheRepository : IBomCacheRepository
{
    private readonly ApplicationDbContext _db;

    public BomCacheRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<BomQueryResult?> GetByArticleNumberAsync(string articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber)) return null;

        var header = await _db.CachedBomHeaders
            .AsNoTracking()
            .Include(h => h.Items)
            .FirstOrDefaultAsync(h => h.Artikelnummer == articleNumber);

        if (header == null) return null;

        var items = header.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => new BomItem
            {
                Position = i.Position ?? string.Empty,
                Baugruppe = i.Baugruppe ?? string.Empty,
                Ressourcenummer = i.Ressourcenummer ?? string.Empty,
                Bezeichnung1 = i.Bezeichnung1 ?? string.Empty,
                Bezeichnung2 = i.Bezeichnung2 ?? string.Empty,
                Menge = i.Menge,
                Beschaffungsartikel = i.Beschaffungsartikel ?? string.Empty,
                Artikelgruppe = i.Artikelgruppe ?? string.Empty
            })
            .ToList();

        return new BomQueryResult(items, header.Source);
    }

    public async Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(
        string lackierteilCategoryName,
        List<string> articleNumbers)
    {
        if (string.IsNullOrWhiteSpace(lackierteilCategoryName) || articleNumbers == null || articleNumbers.Count == 0)
            return new HashSet<string>();

        // DB-internal JOIN: CachedBomItems -> Articles -> ArticleCategories
        var matches = await (
            from h in _db.CachedBomHeaders
            join i in _db.CachedBomItems on h.Id equals i.CachedBomHeaderId
            join a in _db.Articles on i.Ressourcenummer equals a.ArticleNumber
            join c in _db.ArticleCategories on a.ArticleCategoryId equals c.Id
            where articleNumbers.Contains(h.Artikelnummer)
                  && c.Name == lackierteilCategoryName
            select h.Artikelnummer
        ).Distinct().ToListAsync();

        return new HashSet<string>(matches, StringComparer.Ordinal);
    }

    public async Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(
        List<string> articleNumbers)
    {
        if (articleNumbers == null || articleNumbers.Count == 0)
            return new Dictionary<string, (string, DateTime)>();

        var rows = await _db.CachedBomHeaders
            .AsNoTracking()
            .Where(h => articleNumbers.Contains(h.Artikelnummer))
            .Select(h => new { h.Artikelnummer, h.ContentHash, h.CachedAt })
            .ToListAsync();

        return rows.ToDictionary(
            r => r.Artikelnummer,
            r => (r.ContentHash, r.CachedAt));
    }

    public async Task UpsertBomAsync(
        string articleNumber,
        string source,
        string contentHash,
        List<BomItem> items)
    {
        if (string.IsNullOrWhiteSpace(articleNumber))
            throw new ArgumentException("Artikelnummer required", nameof(articleNumber));

        var header = await _db.CachedBomHeaders
            .Include(h => h.Items)
            .FirstOrDefaultAsync(h => h.Artikelnummer == articleNumber);

        if (header == null)
        {
            header = new CachedBomHeader
            {
                Artikelnummer = articleNumber,
                Source = source,
                ItemCount = items.Count,
                ContentHash = contentHash,
                CachedAt = DateTime.Now
            };
            _db.CachedBomHeaders.Add(header);
        }
        else
        {
            // Replace semantics: delete old items, update header
            _db.CachedBomItems.RemoveRange(header.Items);
            header.Source = source;
            header.ItemCount = items.Count;
            header.ContentHash = contentHash;
            header.CachedAt = DateTime.Now;
        }

        // Save so header has its Id before we insert items (if it was new)
        await _db.SaveChangesAsync();

        var sortOrder = 0;
        var newItems = items.Select(bi => new CachedBomItem
        {
            CachedBomHeaderId = header.Id,
            Position = bi.Position,
            Baugruppe = bi.Baugruppe,
            Ressourcenummer = bi.Ressourcenummer,
            Bezeichnung1 = bi.Bezeichnung1,
            Bezeichnung2 = bi.Bezeichnung2,
            Menge = bi.Menge,
            Beschaffungsartikel = bi.Beschaffungsartikel,
            Artikelgruppe = bi.Artikelgruppe,
            SortOrder = sortOrder++
        }).ToList();

        _db.CachedBomItems.AddRange(newItems);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber))
            return new List<string>();

        return await (
            from i in _db.CachedBomItems
            join h in _db.CachedBomHeaders on i.CachedBomHeaderId equals h.Id
            where i.Ressourcenummer == componentArticleNumber
            select h.Artikelnummer
        ).Distinct().ToListAsync();
    }

    public async Task<Dictionary<string, decimal>> GetComponentMengePerDeviceAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber))
            return new Dictionary<string, decimal>();

        var rows = await (
            from i in _db.CachedBomItems
            join h in _db.CachedBomHeaders on i.CachedBomHeaderId equals h.Id
            where i.Ressourcenummer == componentArticleNumber
            group i.Menge by h.Artikelnummer into g
            select new { Artikelnummer = g.Key, Menge = g.Sum() }
        ).ToListAsync();

        return rows.ToDictionary(r => r.Artikelnummer, r => r.Menge);
    }

    public async Task DeleteOrphansAsync(List<string> currentArticleNumbers)
    {
        var current = new HashSet<string>(currentArticleNumbers ?? new List<string>(), StringComparer.Ordinal);

        var orphans = await _db.CachedBomHeaders
            .Where(h => !current.Contains(h.Artikelnummer))
            .ToListAsync();

        if (orphans.Count == 0) return;

        _db.CachedBomHeaders.RemoveRange(orphans); // Cascade deletes items
        await _db.SaveChangesAsync();
    }
}
