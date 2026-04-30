using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ProductionWorkplaceRepository : Repository<ProductionWorkplace>, IProductionWorkplaceRepository
{
    public ProductionWorkplaceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ProductionWorkplace>> GetAllOrderedAsync()
    {
        return await _dbSet
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<ProductionWorkplace?> GetByIdWithUsersAsync(int id)
    {
        return await _dbSet
            .Include(w => w.ProductionWorkplaceUsers)
                .ThenInclude(wu => wu.User)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<List<ProductionWorkplace>> GetAllWithUsersOrderedAsync()
    {
        return await _dbSet
            .Include(w => w.ProductionWorkplaceUsers)
                .ThenInclude(wu => wu.User)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<ProductionWorkplace>> GetBdeActiveAsync()
    {
        return await _dbSet
            .Where(w => w.BdeAktiv)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId)
    {
        return await _context.ProductionWorkplaceUsers
            .AsNoTracking()
            .Where(wu => wu.UserId == userId)
            .Select(wu => wu.ProductionWorkplace)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task SetProductionWorkplaceUsersAsync(int workplaceId, List<int> userIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.ProductionWorkplaceUsers
            .Where(wu => wu.ProductionWorkplaceId == workplaceId)
            .ToListAsync();

        _context.ProductionWorkplaceUsers.RemoveRange(existing);

        foreach (var userId in userIds)
        {
            _context.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
            {
                ProductionWorkplaceId = workplaceId,
                UserId = userId,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }
}
