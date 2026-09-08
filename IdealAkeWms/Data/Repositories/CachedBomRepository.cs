using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedBomRepository : IBomRepository
{
    private readonly BomRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedBomRepository(BomRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
    {
        var cacheKey = $"bom:{key.ArticleNumber}";

        if (_cache.TryGetValue(cacheKey, out BomQueryResult? cached) && cached != null)
            return cached;

        var result = await _inner.GetBomItemsAsync(key, scope);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return result;
    }
}
