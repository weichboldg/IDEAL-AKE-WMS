using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IWorkStepRepository
{
    /// <summary>Alle Arbeitsgaenge inkl. inaktive (Stammdaten-Liste).</summary>
    Task<List<WorkStep>> GetAllAsync();

    /// <summary>Nur aktive, SortOrder-sortiert.</summary>
    Task<List<WorkStep>> GetActiveAsync();

    Task<WorkStep?> GetByIdAsync(int id);
    Task<WorkStep?> GetByCodeAsync(string code);
    Task AddAsync(WorkStep step);
    Task UpdateAsync(WorkStep step);

    /// <summary>True wenn FaWorkSteps/Mappings den Arbeitsgang referenzieren.</summary>
    Task<bool> IsInUseAsync(int id);

    /// <summary>False wenn IsInUse (App-Guard) — dann wird nicht geloescht.</summary>
    Task<bool> DeleteAsync(int id);
}
