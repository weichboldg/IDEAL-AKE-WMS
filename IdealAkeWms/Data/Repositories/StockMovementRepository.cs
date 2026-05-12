using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

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
                StorageLocationDescription = sm.StorageLocation.Description,
                sm.StorageLocation.IsPickingTransport,
                StorageLocationIsActive = sm.StorageLocation.IsActive,
                StorageLocationIstBuchbar = sm.StorageLocation.IstBuchbar
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
                    sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                        ? -sm.Quantity :
                    0m),
                ReorderLevel = g.Key.ReorderLevel,
                IsPickingTransport = g.Key.IsPickingTransport,
                StorageLocationIsActive = g.Key.StorageLocationIsActive,
                StorageLocationIstBuchbar = g.Key.StorageLocationIstBuchbar
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
                StorageLocationDescription = sm.SourceStorageLocation!.Description,
                sm.SourceStorageLocation!.IsPickingTransport,
                StorageLocationIsActive = sm.SourceStorageLocation!.IsActive,
                StorageLocationIstBuchbar = sm.SourceStorageLocation!.IstBuchbar
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
                ReorderLevel = g.Key.ReorderLevel,
                IsPickingTransport = g.Key.IsPickingTransport,
                StorageLocationIsActive = g.Key.StorageLocationIsActive,
                StorageLocationIstBuchbar = g.Key.StorageLocationIstBuchbar
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
                    ReorderLevel = first.ReorderLevel,
                    IsPickingTransport = first.IsPickingTransport,
                    StorageLocationIsActive = first.StorageLocationIsActive,
                    StorageLocationIstBuchbar = first.StorageLocationIstBuchbar
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

    public async Task<List<StockOverviewItem>> GetStockByProductionOrderAsync(string productionOrder)
    {
        // Alle Bewegungen mit dieser FA-Nummer holen
        var movements = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(productionOrder))
            .ToListAsync();

        if (movements.Count == 0)
            return new List<StockOverviewItem>();

        // Pro Bewegung den Netto-Effekt auf Ziel-Lagerplatz berechnen
        var entries = new List<(int ArticleId, string ArticleNumber, string? ArticleDescription,
            string? Unit, int StorageLocationId, string StorageLocationCode,
            string? StorageLocationDescription, bool IsPickingTransport, bool IsActive, bool IstBuchbar, decimal Qty)>();

        foreach (var sm in movements)
        {
            var qty = sm.MovementType switch
            {
                MovementType.Einbuchung => sm.Quantity,
                MovementType.SageEinbuchung => sm.Quantity,
                MovementType.Umbuchung => sm.Quantity,
                MovementType.Ausbuchung => -sm.Quantity,
                MovementType.SageAusbuchung => -sm.Quantity,
                _ => 0m
            };
            entries.Add((sm.ArticleId, sm.Article.ArticleNumber, sm.Article.Description,
                sm.Article.Unit, sm.StorageLocationId, sm.StorageLocation.Code,
                sm.StorageLocation.Description, sm.StorageLocation.IsPickingTransport,
                sm.StorageLocation.IsActive, sm.StorageLocation.IstBuchbar, qty));
        }

        return entries
            .GroupBy(e => new { e.ArticleId, e.StorageLocationId })
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
                    IsPickingTransport = first.IsPickingTransport,
                    StorageLocationIsActive = first.IsActive,
                    StorageLocationIstBuchbar = first.IstBuchbar,
                    CurrentQuantity = g.Sum(e => e.Qty)
                };
            })
            .Where(x => x.CurrentQuantity != 0)
            .OrderBy(x => x.ArticleNumber)
            .ThenBy(x => x.StorageLocationCode)
            .ToList();
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
                                   sm.MovementType == MovementType.Ausbuchung ? "Ausbuchung" :
                                   sm.MovementType == MovementType.Umbuchung ? "Umbuchung" :
                                   sm.MovementType == MovementType.SageEinbuchung ? "Sage-Einbuchung" :
                                   sm.MovementType == MovementType.SageAusbuchung ? "Sage-Ausbuchung" :
                                   "Unbekannt",
                UserName = sm.User != null ? sm.User.Name : sm.WindowsUser,
                ProductionOrder = sm.ProductionOrder,
                Note = sm.Note
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Dictionary<string, List<StockLocationInfo>>> GetStockByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (!articleNumbers.Any())
            return new Dictionary<string, List<StockLocationInfo>>();

        // Query 1: Destination stock (Einbuchung + Umbuchung-Ziel) — ohne Kommissionierwagen
        var destItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.StorageLocation)
            .Where(sm => articleNumbers.Contains(sm.Article.ArticleNumber)
                      && !sm.StorageLocation.IsPickingTransport)
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
                    sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                        ? -sm.Quantity :
                    0m)
            })
            .ToListAsync();

        // Query 2: Source subtraction (Umbuchung-Quelle) — ohne Kommissionierwagen
        var srcItems = await _dbSet
            .Include(sm => sm.Article)
            .Include(sm => sm.SourceStorageLocation)
            .Where(sm => sm.MovementType == MovementType.Umbuchung
                      && sm.SourceStorageLocationId != null
                      && articleNumbers.Contains(sm.Article.ArticleNumber)
                      && !sm.SourceStorageLocation!.IsPickingTransport)
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
                sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                    ? sm.Quantity :
                sm.MovementType == MovementType.Umbuchung
                    ? sm.Quantity :
                sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                    ? -sm.Quantity :
                0m);

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

    public async Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync()
    {
        var movements = await _dbSet
            .AsNoTracking()
            .ToListAsync();

        var dict = new Dictionary<(int, int), decimal>();

        foreach (var sm in movements)
        {
            decimal effect = sm.MovementType switch
            {
                MovementType.Einbuchung => sm.Quantity,
                MovementType.SageEinbuchung => sm.Quantity,
                MovementType.Umbuchung => sm.Quantity,   // Ziel-Lagerplatz
                MovementType.Ausbuchung => -sm.Quantity,
                MovementType.SageAusbuchung => -sm.Quantity,
                _ => 0m
            };

            var key = (sm.ArticleId, sm.StorageLocationId);
            dict[key] = dict.GetValueOrDefault(key, 0m) + effect;

            // Umbuchung-Quell-Seite: -Quantity am SourceStorageLocationId
            if (sm.MovementType == MovementType.Umbuchung && sm.SourceStorageLocationId.HasValue)
            {
                var srcKey = (sm.ArticleId, sm.SourceStorageLocationId.Value);
                dict[srcKey] = dict.GetValueOrDefault(srcKey, 0m) - sm.Quantity;
            }
        }

        return dict;
    }
}
