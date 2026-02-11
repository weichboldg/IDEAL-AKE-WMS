using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _dbSet.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
    }
}
