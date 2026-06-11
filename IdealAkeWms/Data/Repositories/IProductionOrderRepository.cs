using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public record LeitstandOrderRow(
    int Id,
    string OrderNumber,
    decimal Quantity,
    string? Customer,
    string? ArticleNumber,
    string? Description1,
    string? Description2,
    DateTime? ProductionDate,
    DateTime? DeliveryDate,
    bool IsDone,
    bool IsDonePicking,
    string? WorkplaceName);

public record LeitstandOrderPage(List<LeitstandOrderRow> Rows, int TotalCount);

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

    /// <summary>
    /// FA-/Leitstand-Liste mit Server-Side-Filterung, Projection und Pagination.
    /// Filter laufen in SQL; nur die in der View angezeigten Spalten werden
    /// materialisiert. AsNoTracking. <paramref name="page"/> ist 1-basiert.
    /// </summary>
    /// <param name="columnFilters">
    /// Optionale Spalten-Filter aus der URL (<c>colf_&lt;col-key&gt;=value</c>).
    /// Bekannte Keys: order-number, customer, article-number, description1,
    /// description2, workbench. OR-/NOT-Syntax wie clientseitig.
    /// </param>
    Task<LeitstandOrderPage> GetForLeitstandAsync(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone,
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string>? columnFilters = null);
}
