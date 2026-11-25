using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public class BulkReleaseResult
{
    public int Processed { get; set; }
    /// <summary>v1.88.0: Ids der verarbeiteten FAs (Kommissionierauftraege nach der Freigabe erzeugen).</summary>
    public List<int> ProcessedIds { get; set; } = new();
    public List<string> SkippedNoArticle { get; set; } = new();
    public List<string> SkippedSubFa { get; set; } = new();
}

public interface IProductionOrderPickingStatusRepository
{
    Task<ProductionOrderPickingStatus?> GetByProductionOrderIdAsync(int productionOrderId);
    Task<Dictionary<int, ProductionOrderPickingStatus>> GetByProductionOrderIdsAsync(IEnumerable<int> productionOrderIds);
    Task SetFieldAsync(int productionOrderId, string field, bool value, string modifiedBy, string modifiedByWindows);
    /// <summary>v1.99.23 (F1, ADR 0026): false = der FA stand schon im Zielzustand (paralleler Klick hat gewonnen) – nichts geschrieben.</summary>
    Task<bool> SetReleaseAsync(int productionOrderId, bool released, int? priority, string? releasedBy, string modifiedBy, string modifiedByWindows);
    Task SetAssignedPickerAsync(int productionOrderId, int? pickerId, string? pickerName, string modifiedBy, string modifiedByWindows);
    Task SetPickingStatusTextAsync(int productionOrderId, string? statusText, string modifiedBy, string modifiedByWindows);
    /// <summary>v1.99.70 (F8): Marker „Stückliste kleiner“ setzen/loeschen (mit Audit-Feldern); nur aus PickingOrderWorkflow.</summary>
    Task SetStuecklisteKleinerAsync(int productionOrderId, string? text, string modifiedBy, string modifiedByWindows);
    Task SetIsDonePickingAsync(int productionOrderId, bool value, string modifiedBy, string modifiedByWindows);
    Task SetPriorityAsync(int productionOrderId, int? priority, string modifiedBy, string modifiedByWindows);
    /// <summary>Setzt HasCoatingParts je Auftrag (nur bestehende Zeilen); bei Wechsel auf false auch IsCoatingDone zurueck.
    /// v1.99.25: Audit-Felder bei jeder Aenderung (ADR 0003) — Service-Name und Dienstkonto.</summary>
    Task SetCoatingPartsAsync(Dictionary<int, bool> orderIdToHasCoatingParts, string modifiedBy, string modifiedByWindows);
    Task<List<ProductionOrder>> GetReleasedForPickingAsync();
    Task<List<ProductionOrder>> GetReleasedForPickingByPickerAsync(int pickerId);
    Task<int> GetReleasedForPickingCountAsync();

    /// <summary>v1.99.43 (A5): OrderNumbers aller HauptFA (<see cref="ProductionOrder.IsHauptFa"/>) mit <c>IsReleasedForPicking</c>,
    /// distinct, EINE Abfrage — Gate der IDEAL-Kommissionierliste. Bewusst ohne IsDone/IsCancelled/IsDonePicking (Freigabe-Antwort 1).</summary>
    Task<List<string>> GetReleasedHauptFaOrderNumbersAsync();

    /// <summary>
    /// Returns the current MAX(PickingPriority) over all open (not done, not cancelled) HauptFAs with a priority,
    /// released or revoked (a revoked order keeps its priority), or 0 if none exist. Optionally excludes one ProductionOrderId from the calculation.
    /// </summary>
    Task<int> GetMaxPickingPriorityAsync(int? excludeProductionOrderId = null);

    /// <summary>
    /// Bulk release/unrelease for a set of production orders in a single SaveChanges roundtrip.
    /// When releasing: skips orders without ArticleNumber and reports them in SkippedNoArticle.
    /// Assigns priorities continuing from <see cref="GetMaxPickingPriorityAsync"/> for orders that don't have one.
    /// When releasing and assignedPickerId is provided, sets picker on every processed row.
    /// </summary>
    Task<BulkReleaseResult> SetReleaseBatchAsync(
        IEnumerable<int> productionOrderIds,
        bool release,
        int? assignedPickerId,
        string? assignedPickerName,
        string? releasedBy,
        string modifiedBy,
        string modifiedByWindows);
}
