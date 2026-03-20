using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllOrderedAsync()
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Role?> GetByKeyAsync(string key)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Key == key);
    }

    public async Task AddAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Role role)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Role role)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Role>> GetRolesWithAdGroupAsync()
    {
        return await _context.Roles
            .Where(r => r.AdGroup != null && r.AdGroup != "")
            .ToListAsync();
    }

    public async Task<List<string>> GetRoleKeysByUserIdAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Key)
            .ToListAsync();
    }

    public async Task SetUserRolesAsync(int userId, List<int> roleIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        var toRemove = existing.Where(ur => !roleIds.Contains(ur.RoleId)).ToList();
        _context.UserRoles.RemoveRange(toRemove);

        var existingRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();
        var toAdd = roleIds
            .Where(rid => !existingRoleIds.Contains(rid))
            .Select(rid => new UserRole
            {
                UserId = userId,
                RoleId = rid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });

        _context.UserRoles.AddRange(toAdd);
        await _context.SaveChangesAsync();
    }
}
