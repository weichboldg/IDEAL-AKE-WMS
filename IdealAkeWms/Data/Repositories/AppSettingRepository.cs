using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly ApplicationDbContext _context;

    public AppSettingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppSetting>> GetAllAsync()
    {
        return await _context.AppSettings.OrderBy(s => s.Key).ToListAsync();
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        return setting?.Value;
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task SetValueAsync(string key, string? value)
    {
        // Empty form text-fields bind to null in Dictionary<string, string>; AppSetting.Value is NOT NULL.
        var safeValue = value ?? string.Empty;

        var setting = await _context.AppSettings.FindAsync(key);
        if (setting != null)
        {
            setting.Value = safeValue;
        }
        else
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = safeValue });
        }
        await _context.SaveChangesAsync();
    }
}
