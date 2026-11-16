using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomCacheRepository
{
    /// <summary>
    /// Reads a BOM from the cache. Returns null if no header exists.
    /// </summary>
    Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key);

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
    /// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F4): je Bauteil (Ressourcenummer) die "Geraete", deren Stueckliste es
    /// enthaelt, mit der summierten Menge aller passenden Positionen je Geraet. Flach: Geraet = CachedBomHeader.Artikelnummer;
    /// hierarchisch: SubOrderNumber der Eltern-Sub-FA. Eine Abfrage fuer alle Bauteile (kein N+1); Bauteile ohne Treffer fehlen.
    /// Ersetzt GetDeviceArticleNumbersByComponentAsync + GetComponentMengePerDeviceAsync(string) (dieselbe Schluesselmenge).
    /// </summary>
    Task<Dictionary<string, Dictionary<string, decimal>>> GetComponentMengePerDeviceAsync(IReadOnlyCollection<string> componentArticleNumbers);
}
