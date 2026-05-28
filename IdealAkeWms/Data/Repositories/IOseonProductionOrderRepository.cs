using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IOseonProductionOrderRepository : IRepository<OseonProductionOrder>
{
    Task<List<OseonProductionOrder>> GetAllWithOperationsAsync();
    Task<List<OseonProductionOrder>> GetByCustomerOrderNumberAsync(string customerOrderNumber);
    Task<OseonProductionOrder?> GetByOseonIdAsync(long oseonId);

    /// <summary>
    /// Server-side filtered + paginated query. Filters and pagination are pushed to SQL.
    /// When relevantOperationNames is set, "finished" is determined by whether all relevant operations are done.
    /// </summary>
    Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize, HashSet<string>? relevantOperationNames = null, string? articleNumber = null, IReadOnlyDictionary<string, string>? columnFilters = null);

    /// <summary>
    /// Laedt alle Sub-Orders + WorkOperations einer einzelnen Kundenauftrag-Gruppe.
    /// Wird vom OseonGroupDetails-AJAX-Endpoint genutzt fuer Lazy-Load.
    /// </summary>
    Task<List<OseonProductionOrder>> GetSubOrdersForCustomerOrderAsync(
        string customerOrderNumber,
        bool showFinished,
        HashSet<string>? relevantOperationNames,
        CancellationToken ct = default);

    /// <summary>
    /// Liefert relevante Arbeitsgaenge fuer das OSEON-Reporting.
    /// Filter: Aktive Auftragsstatus (20/30/60/90), DueDate NOT NULL, AGs nicht storniert (95),
    /// nur AGs mit Config und IsOseonRelevant=true. AGs ohne Config werden gezaehlt aber nicht zurueckgegeben.
    /// </summary>
    Task<OseonReportingQueryResult> GetRelevantOperationsForReportingAsync(
        int? workplaceId,
        IReadOnlyCollection<string>? operationNames,
        string? customerOrderNumber,
        string? faNumberPrefix,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);
}

public class OseonPagedResult
{
    public List<OseonProductionOrder> Items { get; set; } = new();
    public int TotalGroupCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalGroupCount / (double)PageSize);
}
