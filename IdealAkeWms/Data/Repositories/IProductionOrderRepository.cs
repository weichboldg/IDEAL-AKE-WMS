using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IProductionOrderRepository : IRepository<ProductionOrder>
{
    Task<List<ProductionOrder>> GetAllOrderedAsync();
    Task<List<ProductionOrder>> GetOpenOrdersAsync();
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<List<ProductionOrder>> SearchAsync(string? query, int limit = 20);

    /// <summary>
    /// Returns the top-N open production orders with ProductionDate in the next
    /// weeksAhead weeks, ordered by ProductionDate ASC. Orders without ProductionDate are excluded.
    /// </summary>
    Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount);

    /// <summary>
    /// Returns production orders whose ArticleNumber is in the given list.
    /// </summary>
    Task<List<ProductionOrder>> GetByArticleNumbersAsync(List<string> articleNumbers);
}
