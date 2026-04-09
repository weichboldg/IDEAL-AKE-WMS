using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomCacheRepository
{
    /// <summary>
    /// Reads a BOM from the cache. Returns null if no header exists.
    /// </summary>
    Task<BomQueryResult?> GetByArticleNumberAsync(string articleNumber);

    /// <summary>
    /// Returns the subset of <paramref name="articleNumbers"/> whose BOMs contain
    /// at least one item referencing an Article in the given category.
    /// </summary>
    Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(
        string lackierteilCategoryName,
        List<string> articleNumbers);

    /// <summary>
    /// Bulk-loads header hashes and timestamps keyed by Artikelnummer.
    /// Used by the sync service to decide whether a BOM needs refreshing.
    /// </summary>
    Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(
        List<string> articleNumbers);

    /// <summary>
    /// Inserts or replaces a cache entry. Existing items are deleted and re-inserted.
    /// </summary>
    Task UpsertBomAsync(
        string articleNumber,
        string source,
        string contentHash,
        List<BomItem> items);

    /// <summary>
    /// Deletes any header whose Artikelnummer is NOT in the given list.
    /// </summary>
    Task DeleteOrphansAsync(List<string> currentArticleNumbers);

    /// <summary>
    /// Returns device article numbers (CachedBomHeader.Artikelnummer) whose BOMs
    /// contain a component with the given Ressourcenummer.
    /// </summary>
    Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber);
}
