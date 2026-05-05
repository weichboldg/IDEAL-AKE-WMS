using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _dbSet.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<List<User>> GetActivePickersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && u.IsPicker)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllWithRolesAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();
    }

    public async Task<User?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = name.Trim();
        return await _dbSet.FirstOrDefaultAsync(u => u.Name == trimmed);
    }
}
