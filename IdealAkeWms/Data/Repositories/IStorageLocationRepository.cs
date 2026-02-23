using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IStorageLocationRepository : IRepository<StorageLocation>
{
    Task<List<StorageLocation>> GetAllOrderedAsync();
    Task<StorageLocation?> GetByCodeAsync(string code);
}
