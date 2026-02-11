using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<List<User>> GetActiveUsersAsync();
}
