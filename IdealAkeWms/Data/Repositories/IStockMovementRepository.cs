using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<List<StockOverviewItem>> GetCurrentStockAsync(
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        decimal? filterMinQuantity = null,
        decimal? filterMaxQuantity = null);

    /// <summary>
    /// Zeigt Bestände für Artikel eines Fertigungsauftrags.
    /// Berechnet den Netto-Bestand aus allen Bewegungen die mit dieser FA-Nummer gebucht wurden.
    /// </summary>
    Task<List<StockOverviewItem>> GetStockByProductionOrderAsync(string productionOrder);

    Task<(List<MovementHistoryItem> Items, int TotalCount)> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null,
        int page = 1,
        int pageSize = 50);

    Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers);

    Task<decimal> GetCurrentStockAtLocationAsync(int articleId, int storageLocationId);

    Task<List<string>> GetProductionOrdersAtLocationAsync(int storageLocationId);

    /// <summary>
    /// Liefert den aggregierten WMS-Bestand pro (ArticleId, StorageLocationId).
    /// Wird vom LagerbestandSyncService genutzt, um effizient gegen Sage-Bestand zu vergleichen.
    /// Beruecksichtigt alle MovementType-Werte inklusive Umbuchung-Quell-Seite.
    /// </summary>
    Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync();
}
