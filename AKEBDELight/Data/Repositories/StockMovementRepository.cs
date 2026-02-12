using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class StockMovementRepository : Repository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<StockOverviewItem>> GetCurrentStockAsync(
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        decimal? filterMinQuantity = null,
        decimal? filterMaxQuantity = null)
    {
        var query = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            query = query.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
        {
            query = query.Where(sm => sm.StorageLocationId == filterStorageLocationId.Value);
        }

        var grouped = await query
            .GroupBy(sm => new
            {
                sm.ArticleId,
                sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                sm.Article.Unit,
                sm.Article.ReorderLevel,
                sm.StorageLocationId,
                StorageLocationCode = sm.StorageLocation.Code,
                StorageLocationDescription = sm.StorageLocation.Description
            })
            .Select(g => new StockOverviewItem
            {
                ArticleId = g.Key.ArticleId,
                ArticleNumber = g.Key.ArticleNumber,
                ArticleDescription = g.Key.ArticleDescription,
                Unit = g.Key.Unit,
                StorageLocationId = g.Key.StorageLocationId,
                StorageLocationCode = g.Key.StorageLocationCode,
                StorageLocationDescription = g.Key.StorageLocationDescription,
                CurrentQuantity = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung ? sm.Quantity : -sm.Quantity),
                ReorderLevel = g.Key.ReorderLevel
            })
            .ToListAsync();

        if (filterMinQuantity.HasValue)
        {
            grouped = grouped.Where(g => g.CurrentQuantity >= filterMinQuantity.Value).ToList();
        }

        if (filterMaxQuantity.HasValue)
        {
            grouped = grouped.Where(g => g.CurrentQuantity <= filterMaxQuantity.Value).ToList();
        }

        return grouped.OrderBy(g => g.ArticleNumber).ThenBy(g => g.StorageLocationCode).ToList();
    }

    public async Task<List<MovementHistoryItem>> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null)
    {
        var query = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Include(sm => sm.User)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(sm => sm.Timestamp >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(sm => sm.Timestamp <= dateTo.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            query = query.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
            query = query.Where(sm => sm.StorageLocationId == filterStorageLocationId.Value);

        if (filterMovementType.HasValue)
            query = query.Where(sm => sm.MovementType == filterMovementType.Value);

        if (filterUserId.HasValue)
            query = query.Where(sm => sm.UserId == filterUserId.Value);

        if (!string.IsNullOrWhiteSpace(filterProductionOrder))
            query = query.Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(filterProductionOrder));

        return await query
            .OrderByDescending(sm => sm.Timestamp)
            .Select(sm => new MovementHistoryItem
            {
                Id = sm.Id,
                Timestamp = sm.Timestamp,
                ArticleNumber = sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                Quantity = sm.Quantity,
                StorageLocationCode = sm.StorageLocation.Code,
                MovementType = sm.MovementType,
                MovementTypeName = sm.MovementType == MovementType.Einbuchung ? "Einbuchung" : "Ausbuchung",
                UserName = sm.User != null ? sm.User.Name : sm.WindowsUser,
                ProductionOrder = sm.ProductionOrder
            })
            .ToListAsync();
    }
}
