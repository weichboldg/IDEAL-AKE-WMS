using System.Linq.Expressions;
using IdealAkeWms.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IdealAkeWms.Data.Repositories;

public class CachedHolidayRepository : IHolidayRepository
{
    private readonly HolidayRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string DatesKey = "holidays:dates";
    private const string AllKey = "holidays:all";
    private const string AllOrderedKey = "holidays:allOrdered";

    public CachedHolidayRepository(HolidayRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<HashSet<DateTime>> GetHolidayDatesAsync()
    {
        if (_cache.TryGetValue(DatesKey, out HashSet<DateTime>? cached))
            return cached!;

        var dates = await _inner.GetHolidayDatesAsync();
        _cache.Set(DatesKey, dates, CacheDuration);
        return dates;
    }

    public async Task<List<Holiday>> GetAllOrderedAsync()
    {
        if (_cache.TryGetValue(AllOrderedKey, out List<Holiday>? cached))
            return cached!;

        var all = await _inner.GetAllOrderedAsync();
        _cache.Set(AllOrderedKey, all, CacheDuration);
        return all;
    }

    public async Task<List<Holiday>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllKey, out List<Holiday>? cached))
            return cached!;

        var all = await _inner.GetAllAsync();
        _cache.Set(AllKey, all, CacheDuration);
        return all;
    }

    private void InvalidateCache()
    {
        _cache.Remove(DatesKey);
        _cache.Remove(AllKey);
        _cache.Remove(AllOrderedKey);
    }

    public async Task<Holiday> AddAsync(Holiday entity) { InvalidateCache(); return await _inner.AddAsync(entity); }
    public Task UpdateAsync(Holiday entity) { InvalidateCache(); return _inner.UpdateAsync(entity); }
    public Task DeleteAsync(int id) { InvalidateCache(); return _inner.DeleteAsync(id); }
    public Task<Holiday?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
    public Task<List<Holiday>> FindAsync(Expression<Func<Holiday, bool>> predicate) => _inner.FindAsync(predicate);
}
