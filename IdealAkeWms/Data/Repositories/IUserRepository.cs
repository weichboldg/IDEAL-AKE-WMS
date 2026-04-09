using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<List<User>> GetActiveUsersAsync();
    Task<List<User>> GetActivePickersAsync();
    Task<List<User>> GetAllWithRolesAsync();
}
