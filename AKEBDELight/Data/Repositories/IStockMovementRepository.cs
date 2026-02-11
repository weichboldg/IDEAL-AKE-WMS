using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;

namespace AKEBDELight.Data.Repositories;

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<List<StockOverviewItem>> GetCurrentStockAsync(
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        decimal? filterMinQuantity = null,
        decimal? filterMaxQuantity = null);

    Task<List<MovementHistoryItem>> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null);
}
