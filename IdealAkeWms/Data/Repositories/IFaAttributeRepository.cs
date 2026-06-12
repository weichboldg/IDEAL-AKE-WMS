using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IFaAttributeRepository
{
    /// <summary>Alle Definitionen inkl. Options + WorkSteps, SortOrder-sortiert.</summary>
    Task<List<FaAttributeDefinition>> GetAllAsync();

    /// <summary>Aktive Definitionen via FaAttributeWorkSteps-Junction, distinct, SortOrder-sortiert. Include Options.</summary>
    Task<List<FaAttributeDefinition>> GetActiveForWorkStepsAsync(List<int> workStepIds);

    Task<FaAttributeDefinition?> GetByIdAsync(int id);
    Task AddDefinitionAsync(FaAttributeDefinition def);
    Task UpdateDefinitionAsync(FaAttributeDefinition def);

    /// <summary>false wenn Values zur Definition existieren.</summary>
    Task<bool> DeleteDefinitionAsync(int id);

    Task AddOptionAsync(FaAttributeOption option);
    Task UpdateOptionAsync(FaAttributeOption option);

    /// <summary>false wenn Values die Option referenzieren (dann nur IsActive=false setzen).</summary>
    Task<bool> DeleteOptionAsync(int id);

    /// <summary>Junction-Sync (add/remove Delta).</summary>
    Task SetWorkStepsAsync(int definitionId, List<int> workStepIds);

    /// <summary>Werte eines FA inkl. Definition + SelectedOption.</summary>
    Task<List<FaAttributeValue>> GetValuesByProductionOrderIdAsync(int productionOrderId);

    /// <summary>Legt Wert-Zeile an bzw. aktualisiert sie. selectedOptionId UND booleanValue null = "leer" -> Zeile loeschen.</summary>
    Task UpsertValueAsync(int productionOrderId, int definitionId, int? selectedOptionId, bool? booleanValue,
        string modifiedBy, string modifiedByWindows);
}
