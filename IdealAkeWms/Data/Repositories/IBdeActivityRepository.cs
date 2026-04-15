using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IBdeActivityRepository
{
    Task<List<BdeActivity>> GetAllAsync();
    Task<List<BdeActivity>> GetAllActiveAsync();
    Task<BdeActivity?> GetByIdAsync(int id);
    Task<BdeActivity?> GetByCodeAsync(string code);
    Task AddAsync(BdeActivity activity);
    Task UpdateAsync(BdeActivity activity);
}
