using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IBdeOperatorRepository
{
    Task<List<BdeOperator>> GetAllAsync();
    Task<List<BdeOperator>> GetAllActiveAsync();
    Task<BdeOperator?> GetByIdAsync(int id);
    Task<BdeOperator?> GetByPersonnelNumberAsync(string personnelNumber);
    Task AddAsync(BdeOperator op);
    Task UpdateAsync(BdeOperator op);
}
