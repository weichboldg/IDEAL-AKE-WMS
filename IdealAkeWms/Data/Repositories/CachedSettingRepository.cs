using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedSettingRepository : IAppSettingRepository
{
    private readonly AppSettingRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
    private const string CachePrefix = "setting:";

    public CachedSettingRepository(AppSettingRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var cacheKey = CachePrefix + key;
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var value = await _inner.GetValueAsync(key);
        _cache.Set(cacheKey, value, CacheDuration);
        return value;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task SetValueAsync(string key, string value)
    {
        await _inner.SetValueAsync(key, value);
        _cache.Remove(CachePrefix + key);
    }

    public Task<List<AppSetting>> GetAllAsync() => _inner.GetAllAsync();
}
