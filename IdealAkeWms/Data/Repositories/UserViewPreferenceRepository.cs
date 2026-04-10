using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class UserViewPreferenceRepository : IUserViewPreferenceRepository
{
    private readonly ApplicationDbContext _context;

    public UserViewPreferenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserViewPreference?> GetByUserAndViewAsync(int userId, string viewKey)
    {
        return await _context.UserViewPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ViewKey == viewKey);
    }

    public async Task SaveAsync(int userId, string viewKey, string settingsJson, string modifiedBy, string modifiedByWindows)
    {
        var existing = await GetByUserAndViewAsync(userId, viewKey);
        if (existing != null)
        {
            existing.SettingsJson = settingsJson;
            existing.ModifiedAt = DateTime.UtcNow;
            existing.ModifiedBy = modifiedBy;
            existing.ModifiedByWindows = modifiedByWindows;
        }
        else
        {
            _context.UserViewPreferences.Add(new UserViewPreference
            {
                UserId = userId,
                ViewKey = viewKey,
                SettingsJson = settingsJson,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = modifiedBy,
                CreatedByWindows = modifiedByWindows
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByUserAndViewAsync(int userId, string viewKey)
    {
        var existing = await GetByUserAndViewAsync(userId, viewKey);
        if (existing != null)
        {
            _context.UserViewPreferences.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllByUserAsync(int userId)
    {
        var prefs = await _context.UserViewPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _context.UserViewPreferences.RemoveRange(prefs);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserViewPreference>> GetAllByUserAsync(int userId)
    {
        return await _context.UserViewPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ViewKey)
            .ToListAsync();
    }
}
