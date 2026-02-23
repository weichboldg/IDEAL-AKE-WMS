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

    public async Task<List<BomItem>> GetBomItemsAsync(string productionOrderArticleNumber)
    {
        var cacheKey = $"bom:{productionOrderArticleNumber}";

        if (_cache.TryGetValue(cacheKey, out List<BomItem>? cached) && cached != null)
            return cached;

        var items = await _inner.GetBomItemsAsync(productionOrderArticleNumber);

        _cache.Set(cacheKey, items, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        });

        return items;
    }
}
