using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<List<ProductionOrder>> GetAllOrderedAsync();
    Task<List<ProductionOrder>> GetOpenOrdersAsync();
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20);
    Task<List<ProductionOrder>> GetReleasedForPickingAsync();
    Task<int> GetReleasedForPickingCountAsync();

    /// <summary>
    /// Returns the top-N open production orders with ProductionDate in the next
    /// weeksAhead weeks, ordered by ProductionDate ASC. Orders without ProductionDate are excluded.
    /// </summary>
    Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount);

    /// <summary>
    /// Bulk-updates HasCoatingParts for the given order ids. If an order flips to
    /// HasCoatingParts=false, IsCoatingDone is also reset to false (spec fallstrick #11).
    /// </summary>
    Task SetCoatingFlagsAsync(Dictionary<int, bool> orderIdToHasCoatingParts);
}
