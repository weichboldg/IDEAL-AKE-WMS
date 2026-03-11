namespace IDEALAKEWMSService.Services;

public record StockBelowReorderItem(
    string ArticleNumber,
    string? Description,
    string? Unit,
    decimal ReorderLevel,
    decimal CurrentStock,
    List<string> StorageLocations
);

public interface IStockCheckService
{
    Task<List<StockBelowReorderItem>> GetArticlesBelowReorderLevelAsync(CancellationToken ct = default);
    Task<List<string>> GetNotificationRecipientsAsync(CancellationToken ct = default);
}
