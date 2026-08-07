using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Cache-Decorator (ADR 0001) fuer <see cref="FaHierarchyNodeRepository"/>. 5-Minuten-
/// <see cref="IMemoryCache"/> analog zum BOM-Cache — die Tabelle wird ohnehin nur alle paar
/// Minuten vom Sync-Service neu befuellt; der Cache reduziert wiederholte Reads bei
/// Baum-/Listen-Aufrufen.
/// </summary>
public class CachedFaHierarchyNodeRepository : IFaHierarchyNodeRepository
{
    private readonly FaHierarchyNodeRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedFaHierarchyNodeRepository(FaHierarchyNodeRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<List<FaHierarchyNode>> GetByHauptFaAsync(int hauptFa)
    {
        var cacheKey = $"fahierarchynode:hauptfa:{hauptFa}";

        if (_cache.TryGetValue(cacheKey, out List<FaHierarchyNode>? cached) && cached != null)
            return cached;

        var result = await _inner.GetByHauptFaAsync(hauptFa);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }

    public async Task<List<FaHierarchyNode>> GetAllAsync()
    {
        const string cacheKey = "fahierarchynode:all";

        if (_cache.TryGetValue(cacheKey, out List<FaHierarchyNode>? cached) && cached != null)
            return cached;

        var result = await _inner.GetAllAsync();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }
}
