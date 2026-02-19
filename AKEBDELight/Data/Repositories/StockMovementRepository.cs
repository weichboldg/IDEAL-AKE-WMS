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

        // Query 1: Reguläre Bewegungen + Umbuchung-Ziel (gruppiert nach StorageLocationId)
        var destinationQuery = query
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
                    sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                    -sm.Quantity),
                ReorderLevel = g.Key.ReorderLevel
            });

        var destinationItems = await destinationQuery.ToListAsync();

        // Query 2: Umbuchung-Quell-Subtraktionen (gruppiert nach SourceStorageLocationId)
        var sourceQuery = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.SourceStorageLocation)
            .Where(sm => sm.MovementType == MovementType.Umbuchung && sm.SourceStorageLocationId != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            sourceQuery = sourceQuery.Where(sm =>
                sm.Article.ArticleNumber.Contains(filterArticle) ||
                (sm.Article.Description != null && sm.Article.Description.Contains(filterArticle)));
        }

        if (filterStorageLocationId.HasValue)
        {
            sourceQuery = sourceQuery.Where(sm => sm.SourceStorageLocationId == filterStorageLocationId.Value);
        }

        var sourceItems = await sourceQuery
            .GroupBy(sm => new
            {
                sm.ArticleId,
                sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                sm.Article.Unit,
                sm.Article.ReorderLevel,
                StorageLocationId = sm.SourceStorageLocationId!.Value,
                StorageLocationCode = sm.SourceStorageLocation!.Code,
                StorageLocationDescription = sm.SourceStorageLocation!.Description
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
                CurrentQuantity = -g.Sum(sm => sm.Quantity),
                ReorderLevel = g.Key.ReorderLevel
            })
            .ToListAsync();

        // Merge: Nach (ArticleId, StorageLocationId) zusammenführen
        var merged = destinationItems
            .Concat(sourceItems)
            .GroupBy(x => new { x.ArticleId, x.StorageLocationId })
            .Select(g =>
            {
                var first = g.First();
                return new StockOverviewItem
                {
                    ArticleId = first.ArticleId,
                    ArticleNumber = first.ArticleNumber,
                    ArticleDescription = first.ArticleDescription,
                    Unit = first.Unit,
                    StorageLocationId = first.StorageLocationId,
                    StorageLocationCode = first.StorageLocationCode,
                    StorageLocationDescription = first.StorageLocationDescription,
                    CurrentQuantity = g.Sum(x => x.CurrentQuantity),
                    ReorderLevel = first.ReorderLevel
                };
            })
            .ToList();

        // Standardmäßig 0-Bestände ausblenden (nur wenn kein expliziter Mengenfilter)
        if (!filterMinQuantity.HasValue && !filterMaxQuantity.HasValue)
        {
            merged = merged.Where(r => r.CurrentQuantity != 0).ToList();
        }

        if (filterMinQuantity.HasValue)
        {
            merged = merged.Where(g => g.CurrentQuantity >= filterMinQuantity.Value).ToList();
        }

        if (filterMaxQuantity.HasValue)
        {
            merged = merged.Where(g => g.CurrentQuantity <= filterMaxQuantity.Value).ToList();
        }

        return merged.OrderBy(g => g.ArticleNumber).ThenBy(g => g.StorageLocationCode).ToList();
    }

    public async Task<(List<MovementHistoryItem> Items, int TotalCount)> GetMovementHistoryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? filterArticle = null,
        int? filterStorageLocationId = null,
        MovementType? filterMovementType = null,
        int? filterUserId = null,
        string? filterProductionOrder = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Include(sm => sm.User)
            .Include(sm => sm.SourceStorageLocation)
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
            query = query.Where(sm => sm.StorageLocationId == filterStorageLocationId.Value
                                   || sm.SourceStorageLocationId == filterStorageLocationId.Value);

        if (filterMovementType.HasValue)
            query = query.Where(sm => sm.MovementType == filterMovementType.Value);

        if (filterUserId.HasValue)
            query = query.Where(sm => sm.UserId == filterUserId.Value);

        if (!string.IsNullOrWhiteSpace(filterProductionOrder))
            query = query.Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(filterProductionOrder));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(sm => sm.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sm => new MovementHistoryItem
            {
                Id = sm.Id,
                Timestamp = sm.Timestamp,
                ArticleNumber = sm.Article.ArticleNumber,
                ArticleDescription = sm.Article.Description,
                Quantity = sm.Quantity,
                StorageLocationCode = sm.StorageLocation.Code,
                SourceStorageLocationCode = sm.SourceStorageLocation != null ? sm.SourceStorageLocation.Code : null,
                MovementType = sm.MovementType,
                MovementTypeName = sm.MovementType == MovementType.Einbuchung ? "Einbuchung" :
                                   sm.MovementType == MovementType.Umbuchung ? "Umbuchung" : "Ausbuchung",
                UserName = sm.User != null ? sm.User.Name : sm.WindowsUser,
                ProductionOrder = sm.ProductionOrder
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (!articleNumbers.Any())
            return new Dictionary<string, List<StockLocationInfo>>();

        // Query 1: Destination stock (Einbuchung + Umbuchung-Ziel)
        var destItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Where(sm => articleNumbers.Contains(sm.Article.ArticleNumber))
            .GroupBy(sm => new
            {
                sm.Article.ArticleNumber,
                sm.StorageLocationId,
                StorageLocationCode = sm.StorageLocation.Code
            })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                g.Key.StorageLocationId,
                g.Key.StorageLocationCode,
                Quantity = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                    -sm.Quantity)
            })
            .ToListAsync();

        // Query 2: Source subtraction (Umbuchung-Quelle)
        var srcItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.SourceStorageLocation)
            .Where(sm => sm.MovementType == MovementType.Umbuchung
                      && sm.SourceStorageLocationId != null
                      && articleNumbers.Contains(sm.Article.ArticleNumber))
            .GroupBy(sm => new
            {
                sm.Article.ArticleNumber,
                StorageLocationId = sm.SourceStorageLocationId!.Value,
                StorageLocationCode = sm.SourceStorageLocation!.Code
            })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                g.Key.StorageLocationId,
                g.Key.StorageLocationCode,
                Quantity = -g.Sum(sm => sm.Quantity)
            })
            .ToListAsync();

        return destItems.Concat(srcItems)
            .GroupBy(x => new { x.ArticleNumber, x.StorageLocationId, x.StorageLocationCode })
            .Select(g => new
            {
                g.Key.ArticleNumber,
                Info = new StockLocationInfo
                {
                    StorageLocationId = g.Key.StorageLocationId,
                    Code = g.Key.StorageLocationCode,
                    Quantity = g.Sum(x => x.Quantity)
                }
            })
            .Where(x => x.Info.Quantity != 0)
            .GroupBy(x => x.ArticleNumber)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Info).OrderBy(i => i.Code).ToList()
            );
    }

    public async Task<decimal> GetCurrentStockAtLocationAsync(int articleId, int storageLocationId)
    {
        var destSum = await _dbSet
            .Where(sm => sm.ArticleId == articleId && sm.StorageLocationId == storageLocationId)
            .SumAsync(sm =>
                sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                -sm.Quantity);

        var srcSum = await _dbSet
            .Where(sm => sm.ArticleId == articleId
                      && sm.SourceStorageLocationId == storageLocationId
                      && sm.MovementType == MovementType.Umbuchung)
            .SumAsync(sm => sm.Quantity);

        return destSum - srcSum;
    }

    public async Task<List<string>> GetProductionOrdersAtLocationAsync(int storageLocationId)
    {
        // Distinct ProductionOrder-Werte von Umbuchungen auf diesen Lagerplatz
        // (nur für Artikel die dort aktuell positiven Bestand haben)
        var currentStock = await GetCurrentStockAsync(filterStorageLocationId: storageLocationId);
        var articleIdsWithStock = currentStock
            .Where(s => s.CurrentQuantity > 0)
            .Select(s => s.ArticleId)
            .ToHashSet();

        if (articleIdsWithStock.Count == 0)
            return new List<string>();

        var orders = await _dbSet
            .Where(sm => sm.StorageLocationId == storageLocationId
                      && sm.ProductionOrder != null
                      && articleIdsWithStock.Contains(sm.ArticleId))
            .Select(sm => sm.ProductionOrder!)
            .Distinct()
            .ToListAsync();

        return orders;
    }
}
