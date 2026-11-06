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

    /// <summary>E6b: Cache der Sage-PPS-Mitarbeiter (vom Dienst geschrieben), sortiert nach Nummer.</summary>
    Task<List<SagePpsMitarbeiter>> GetSagePpsMitarbeiterAsync();

    /// <summary>E6b/G14: App-Layer-Guard fuer den gefilterten UNIQUE-Index (InMemory erzwingt ihn nicht).</summary>
    Task<BdeOperator?> GetBySageMitarbeiternummerAsync(string sageMitarbeiternummer);
}
