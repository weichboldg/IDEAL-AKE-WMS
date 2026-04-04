using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedOseonOperationConfigRepository : IOseonOperationConfigRepository
{
    private readonly OseonOperationConfigRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DictKey = "oseonOpConfig:dict";
    private const string AllKey = "oseonOpConfig:all";

    public CachedOseonOperationConfigRepository(OseonOperationConfigRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Dictionary<string, OseonOperationConfig>> GetAllAsDictionaryAsync()
    {
        if (_cache.TryGetValue(DictKey, out Dictionary<string, OseonOperationConfig>? cached))
            return cached!;

        var dict = await _inner.GetAllAsDictionaryAsync();
        _cache.Set(DictKey, dict, CacheDuration);
        return dict;
    }

    public async Task<List<OseonOperationConfig>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllKey, out List<OseonOperationConfig>? cached))
            return cached!;

        var all = await _inner.GetAllAsync();
        _cache.Set(AllKey, all, CacheDuration);
        return all;
    }

    private void InvalidateCache()
    {
        _cache.Remove(DictKey);
        _cache.Remove(AllKey);
    }

    public Task AddAsync(OseonOperationConfig config) { InvalidateCache(); return _inner.AddAsync(config); }
    public Task UpdateAsync(OseonOperationConfig config) { InvalidateCache(); return _inner.UpdateAsync(config); }
    public Task DeleteAsync(int id) { InvalidateCache(); return _inner.DeleteAsync(id); }
    public Task<OseonOperationConfig?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<OseonOperationConfig?> GetByNameAsync(string operationName) => _inner.GetByNameAsync(operationName);
    public Task<List<string>> GetUnconfiguredOperationNamesAsync() => _inner.GetUnconfiguredOperationNamesAsync();
    public Task<bool> ExistsAsync(string operationName) => _inner.ExistsAsync(operationName);
}
