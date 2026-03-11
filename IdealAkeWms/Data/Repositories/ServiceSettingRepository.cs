using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ServiceSettingRepository : IServiceSettingRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceSettingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceSetting>> GetAllAsync()
    {
        return await _context.ServiceSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<List<ServiceSetting>> GetByCategoryAsync(string category)
    {
        return await _context.ServiceSettings
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _context.ServiceSettings.FindAsync(key);
        return setting?.Value;
    }

    public async Task UpsertAsync(string key, string value, string? category = null, string? description = null)
    {
        var setting = await _context.ServiceSettings.FindAsync(key);
        if (setting != null)
        {
            setting.Value = value;
            if (category != null) setting.Category = category;
            if (description != null) setting.Description = description;
        }
        else
        {
            _context.ServiceSettings.Add(new ServiceSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string key)
    {
        var setting = await _context.ServiceSettings.FindAsync(key);
        if (setting != null)
        {
            _context.ServiceSettings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }
}
