using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedOperationConfigRepository : IOperationConfigRepository
{
    private readonly OperationConfigRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DictKey = "opConfig:dict";
    private const string AllKey = "opConfig:all";
    private const string OffsetsKey = "opConfig:offsets";

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

    public async Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync()
    {
        if (_cache.TryGetValue(OffsetsKey, out IReadOnlyDictionary<string, int>? cached))
            return cached!;

        var offsets = await _inner.GetDueDateOffsetsAsync();
        _cache.Set(OffsetsKey, offsets, CacheDuration);
        return offsets;
    }

    private void InvalidateCache()
    {
        _cache.Remove(DictKey);
        _cache.Remove(AllKey);
        _cache.Remove(OffsetsKey);
    }

    // Nach dem Schreiben invalidieren: ein paralleler Lesezugriff zwischen Invalidieren und SaveChanges koennte sonst den
    // alten Stand fuer 10 Minuten erneut cachen.
    public async Task AddAsync(OperationConfig config) { await _inner.AddAsync(config); InvalidateCache(); }
    public async Task UpdateAsync(OperationConfig config) { await _inner.UpdateAsync(config); InvalidateCache(); }
    public async Task DeleteAsync(int id) { await _inner.DeleteAsync(id); InvalidateCache(); }
    public Task<OperationConfig?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<OperationConfig?> GetByNameAsync(string operationName) => _inner.GetByNameAsync(operationName);
    public Task<List<UnconfiguredOperation>> GetUnconfiguredOperationsAsync() => _inner.GetUnconfiguredOperationsAsync();
    public Task<bool> ExistsAsync(string operationName) => _inner.ExistsAsync(operationName);
}
