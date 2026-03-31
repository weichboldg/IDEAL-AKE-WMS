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
    Task<OseonPagedResult> GetPagedAsync(string? searchTerm, string? workplaceName, bool showFinished, int page, int pageSize, HashSet<string>? relevantOperationNames = null);
}

public class OseonPagedResult
{
    public List<OseonProductionOrder> Items { get; set; } = new();
    public int TotalGroupCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalGroupCount / (double)PageSize);
}
