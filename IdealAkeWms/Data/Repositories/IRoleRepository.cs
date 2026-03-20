using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IRoleRepository
{
    Task<List<Role>> GetAllOrderedAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByKeyAsync(string key);
    Task AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(Role role);
    Task<List<Role>> GetRolesWithAdGroupAsync();
    Task<List<string>> GetRoleKeysByUserIdAsync(int userId);
    Task SetUserRolesAsync(int userId, List<int> roleIds, string createdBy, string createdByWindows);
}
