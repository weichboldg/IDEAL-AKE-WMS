using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedOperationConfigRepository : IOperationConfigRepository
{
    private readonly OperationConfigRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DictKey = "oseonOpConfig:dict";
    private const string AllKey = "oseonOpConfig:all";

    public CachedOperationConfigRepository(OperationConfigRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Dictionary<string, OperationConfig>> GetAllAsDictionaryAsync()
    {
        if (_cache.TryGetValue(DictKey, out Dictionary<string, OperationConfig>? cached))
            return cached!;

        var dict = await _inner.GetAllAsDictionaryAsync();
        _cache.Set(DictKey, dict, CacheDuration);
        return dict;
    }

    public async Task<List<OperationConfig>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllKey, out List<OperationConfig>? cached))
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

    public Task AddAsync(OperationConfig config) { InvalidateCache(); return _inner.AddAsync(config); }
    public Task UpdateAsync(OperationConfig config) { InvalidateCache(); return _inner.UpdateAsync(config); }
    public Task DeleteAsync(int id) { InvalidateCache(); return _inner.DeleteAsync(id); }
    public Task<OperationConfig?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<OperationConfig?> GetByNameAsync(string operationName) => _inner.GetByNameAsync(operationName);
    public Task<List<string>> GetUnconfiguredOperationNamesAsync() => _inner.GetUnconfiguredOperationNamesAsync();
    public Task<bool> ExistsAsync(string operationName) => _inner.ExistsAsync(operationName);
}
