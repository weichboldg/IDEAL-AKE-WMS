using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class WorkstationRepository : Repository<Workstation>, IWorkstationRepository
{
    public WorkstationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Workstation?> GetByIdWithUsersAsync(int id)
    {
        return await _dbSet
            .Include(w => w.DefaultUser)
            .Include(w => w.WorkstationUsers)
                .ThenInclude(wu => wu.User)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<List<Workstation>> GetAllWithUsersAsync()
    {
        return await _dbSet
            .Include(w => w.DefaultUser)
            .Include(w => w.WorkstationUsers)
                .ThenInclude(wu => wu.User)
            .OrderBy(w => w.Name)
            .ToListAsync();
    }

    public async Task SetWorkstationUsersAsync(int workstationId, List<int> userIds, string createdBy, string createdByWindows)
    {
        var existing = await _context.WorkstationUsers
            .Where(wu => wu.WorkstationId == workstationId)
            .ToListAsync();

        _context.WorkstationUsers.RemoveRange(existing);

        foreach (var userId in userIds)
        {
            _context.WorkstationUsers.Add(new WorkstationUser
            {
                WorkstationId = workstationId,
                UserId = userId,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            });
        }

        await _context.SaveChangesAsync();
    }
}
