using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IStorageLocationRepository : IRepository<StorageLocation>
{
    Task<List<StorageLocation>> GetAllOrderedAsync();
    Task<List<StorageLocation>> GetAllOrderedExcludingPickingTransportAsync();
    Task<List<StorageLocation>> GetPickingTransportLocationsAsync();
    Task<StorageLocation?> GetByCodeAsync(string code);
}
