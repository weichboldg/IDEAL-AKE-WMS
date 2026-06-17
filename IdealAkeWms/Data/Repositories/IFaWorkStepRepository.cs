using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Aggregierte Zaehler je FA (nur aktive Zeilen, IsRemoved=0). SpecCompleteCount = IsSpecComplete.</summary>
public record FaWorkStepCounts(int ActiveCount, int SpecCompleteCount, int SpecCount);

/// <summary>Detail-Pivot-Zelle: FaWorkStep-Id + Erledigt-Status (IsCompleted) eines aktiven AGs.</summary>
public record FaWorkStepPivotCell(int FaWorkStepId, bool IsCompleted);

public interface IFaWorkStepRepository
{
    /// <summary>FA-Arbeitsgaenge inkl. WorkStep + Specs. <paramref name="includeRemoved"/> = auch IsRemoved=1.</summary>
    Task<List<FaWorkStep>> GetByProductionOrderIdAsync(int productionOrderId, bool includeRemoved = false);

    /// <summary>Alle aktiven (IsRemoved=0) FaWorkSteps eines bestimmten Arbeitsgangs (Abarbeitungsliste-Filter).</summary>
    Task<List<FaWorkStep>> GetForWorkStepAsync(int workStepId);

    /// <summary>Pivot orderId -> (WorkStep.Code -> aktiv d.h. IsRemoved=0). Chunked in 1000er-Bloecken (SQL-2100-Limit).</summary>
    Task<Dictionary<int, Dictionary<string, bool>>> GetWorkStepPivotAsync(List<int> productionOrderIds);

    /// <summary>
    /// Detail-Pivot orderId -> (WorkStep.Code -> <see cref="FaWorkStepPivotCell"/>). Nur aktive Zeilen
    /// (IsRemoved=0); Zelle traegt FaWorkStepId + IsCompleted. Chunked in 1000er-Bloecken (SQL-2100-Limit).
    /// </summary>
    Task<Dictionary<int, Dictionary<string, FaWorkStepPivotCell>>> GetWorkStepDetailPivotAsync(List<int> productionOrderIds);

    /// <summary>Zaehler je FA (aktive AGs / davon erledigt / Spec-Summe). Chunked in 1000er-Bloecken.</summary>
    Task<Dictionary<int, FaWorkStepCounts>> GetCountsByProductionOrderIdsAsync(List<int> productionOrderIds);

    /// <summary>Legt Zeile an bzw. reaktiviert (IsRemoved=0) oder setzt IsRemoved=1. Source=Manual bei User-Aktion.</summary>
    Task SetActiveAsync(int productionOrderId, int workStepId, bool active, string modifiedBy, string modifiedByWindows);

    /// <summary>Setzt IsCompleted + CompletedAt/By bzw. null.</summary>
    Task SetIsCompletedAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows);

    /// <summary>Setzt IsSpecComplete + SpecCompletedAt/By bzw. null (FA-Vervollstaendigung).</summary>
    Task SetIsSpecCompleteAsync(int faWorkStepId, bool value, string modifiedBy, string modifiedByWindows);

    Task<FaWorkStep?> GetByIdAsync(int id);

    // Spec-CRUD (1:1 Nachfolger des alten Spec-Repos):
    Task<FaWorkStepSpec?> GetSpecByIdAsync(int id);
    Task AddSpecAsync(FaWorkStepSpec spec);
    Task UpdateSpecAsync(FaWorkStepSpec spec);
    Task DeleteSpecAsync(int id);
}
