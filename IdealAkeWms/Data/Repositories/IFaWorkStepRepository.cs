using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Aggregierte Zaehler je FA (nur aktive Zeilen, IsRemoved=0).</summary>
public record FaWorkStepCounts(int ActiveCount, int CompletedCount, int SpecCount);

public interface IFaWorkStepRepository
{
    /// <summary>FA-Arbeitsgaenge inkl. WorkStep + Specs. <paramref name="includeRemoved"/> = auch IsRemoved=1.</summary>
    Task<List<FaWorkStep>> GetByProductionOrderIdAsync(int productionOrderId, bool includeRemoved = false);

    /// <summary>Pivot orderId -> (WorkStep.Code -> aktiv d.h. IsRemoved=0). Chunked in 1000er-Bloecken (SQL-2100-Limit).</summary>
    Task<Dictionary<int, Dictionary<string, bool>>> GetWorkStepPivotAsync(List<int> productionOrderIds);

    /// <summary>Zaehler je FA (aktive AGs / davon erledigt / Spec-Summe). Chunked in 1000er-Bloecken.</summary>
    Task<Dictionary<int, FaWorkStepCounts>> GetCountsByProductionOrderIdsAsync(List<int> productionOrderIds);

    /// <summary>Legt Zeile an bzw. reaktiviert (IsRemoved=0) oder setzt IsRemoved=1. Source=Manual bei User-Aktion.</summary>
    Task SetActiveAsync(int productionOrderId, int workStepId, bool active, string modifiedBy, string modifiedByWindows);

    /// <summary>Setzt IsCompleted + CompletedAt/By bzw. null.</summary>
    Task SetIsCompletedAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows);

    Task<FaWorkStep?> GetByIdAsync(int id);

    // Spec-CRUD (1:1 Nachfolger des alten Spec-Repos):
    Task<FaWorkStepSpec?> GetSpecByIdAsync(int id);
    Task AddSpecAsync(FaWorkStepSpec spec);
    Task UpdateSpecAsync(FaWorkStepSpec spec);
    Task DeleteSpecAsync(int id);
}
