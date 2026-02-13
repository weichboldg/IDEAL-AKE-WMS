using AKEBDELight.Models;

namespace AKEBDELight.Data.Repositories;

public interface IStorageLocationRepository : IRepository<StorageLocation>
{
    Task<List<StorageLocation>> GetAllOrderedAsync();
    Task<StorageLocation?> GetByCodeAsync(string code);
}
