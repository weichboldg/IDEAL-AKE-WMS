using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class PickingTransferService : IPickingTransferService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PickingTransferService> _logger;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IStockMovementRepository _stockMovementRepository;

    public PickingTransferService(
        ApplicationDbContext context,
        ILogger<PickingTransferService> logger,
        IAppSettingRepository settingRepository,
        IStockMovementRepository stockMovementRepository)
    {
        _context = context;
        _logger = logger;
        _settingRepository = settingRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<PickingTransferResult> CheckAndTransferPickedItemsAsync(
        int productionOrderId,
        int targetStorageLocationId,
        bool forceTransfer,
        List<PickingSelectionItem>? selectedItems,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        // Wenn selectedItems übergeben: zuerst IsPicked + SourceStorageLocation setzen
        if (selectedItems != null && selectedItems.Count > 0)
        {
            await ApplyPickingSelectionsAsync(selectedItems, displayName, windowsUser);
        }

        var order = await _context.ProductionOrders.FindAsync(productionOrderId);
        var targetLocation = await _context.StorageLocations.FindAsync(targetStorageLocationId);

        // Kommissionierwagen-Konfliktprüfung
        if (targetLocation?.IsPickingTransport == true && !forceTransfer)
        {
            var existingOrders = await _stockMovementRepository.GetProductionOrdersAtLocationAsync(targetStorageLocationId);
            var currentWa = order?.OrderNumber;

            var conflictingOrders = existingOrders
                .Where(o => !string.IsNullOrEmpty(o) && o != currentWa)
                .ToList();

            if (conflictingOrders.Any())
            {
                return new PickingTransferResult
                {
                    Success = false,
                    IsPickingTransportConflict = true,
                    ConflictStorageLocationId = targetStorageLocationId,
                    ConflictStorageLocationCode = targetLocation.Code,
                    CurrentWaNumbers = string.Join("; ", existingOrders.Where(o => !string.IsNullOrEmpty(o)).Distinct()),
                    NewWaNumber = currentWa
                };
            }
        }

        // Bei forceTransfer: FA-Nummern zusammenführen
        string? productionOrderValue = order?.OrderNumber;
        if (forceTransfer && targetLocation?.IsPickingTransport == true)
        {
            var existingOrders = await _stockMovementRepository.GetProductionOrdersAtLocationAsync(targetStorageLocationId);
            var allOrders = existingOrders
                .Concat(new[] { order?.OrderNumber ?? "" })
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
            productionOrderValue = string.Join(";", allOrders);
        }

        var count = await DoTransferAsync(productionOrderId, targetStorageLocationId, productionOrderValue, appUserId, displayName, windowsUser);

        return new PickingTransferResult
        {
            Success = true,
            TransferredCount = count
        };
    }

    private async Task ApplyPickingSelectionsAsync(List<PickingSelectionItem> items, string userName, string windowsUser)
    {
        var now = DateTime.Now;

        // Get productionOrderId from first item (1 query)
        var firstItemId = items.First().PickingItemId;
        var productionOrderId = await _context.PickingItems
            .Where(p => p.Id == firstItemId)
            .Select(p => p.ProductionOrderId)
            .FirstOrDefaultAsync();

        if (productionOrderId == 0) return;

        // Load ALL non-transferred items for this order (1 query instead of 2)
        var allOrderItems = await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId && !p.IsTransferred)
            .ToListAsync();

        var selectionMap = items.ToDictionary(i => i.PickingItemId);
        var selectedIds = items.Select(i => i.PickingItemId).ToHashSet();

        // Reset all, then mark selected ones as picked
        foreach (var item in allOrderItems)
        {
            if (selectedIds.Contains(item.Id) && selectionMap.TryGetValue(item.Id, out var selection))
            {
                item.IsPicked = true;
                item.IsBaugruppe = selection.IsBaugruppe;
                item.PickedAt = now;
                item.PickedBy = userName;
                item.PickedByWindows = windowsUser;
                item.SourceStorageLocationId = selection.SourceStorageLocationId;
                item.ModifiedAt = now;
                item.ModifiedBy = userName;
                item.ModifiedByWindows = windowsUser;
            }
            else
            {
                item.IsPicked = false;
                item.PickedAt = null;
                item.PickedBy = null;
                item.PickedByWindows = null;
                item.SourceStorageLocationId = null;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> TransferPickedItemsAsync(
        int productionOrderId,
        int targetStorageLocationId,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        var order = await _context.ProductionOrders.FindAsync(productionOrderId);
        return await DoTransferAsync(productionOrderId, targetStorageLocationId, order?.OrderNumber, appUserId, displayName, windowsUser);
    }

    private async Task<int> DoTransferAsync(
        int productionOrderId,
        int targetStorageLocationId,
        string? productionOrder,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        // 1. Load all picked items (1 query)
        var pickedItems = await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId
                     && p.IsPicked && !p.IsTransferred)
            .ToListAsync();

        if (!pickedItems.Any())
            throw new InvalidOperationException("Keine gepickten Artikel zum Umbuchen vorhanden.");

        // 2. Batch-load all needed articles into dictionary by ArticleNumber (1 query)
        var neededArticleNumbers = pickedItems
            .Select(p => p.BomArticleNumber)
            .Distinct()
            .ToList();

        var articleLookup = await _context.Articles
            .Where(a => neededArticleNumbers.Contains(a.ArticleNumber))
            .ToDictionaryAsync(a => a.ArticleNumber);

        // 3. Collect all (articleId, sourceLocationId) pairs we need stock for
        var stockKeys = new HashSet<(int articleId, int locationId)>();
        foreach (var item in pickedItems)
        {
            if (!item.SourceStorageLocationId.HasValue) continue;
            if (!articleLookup.ContainsKey(item.BomArticleNumber)) continue;
            stockKeys.Add((articleLookup[item.BomArticleNumber].Id, item.SourceStorageLocationId.Value));
        }

        var articleIds = stockKeys.Select(k => k.articleId).Distinct().ToList();
        var locationIds = stockKeys.Select(k => k.locationId).Distinct().ToList();

        // 4. Pre-calculate stock: destSum per (articleId, locationId) — 1 query
        var destSums = await _context.StockMovements
            .Where(sm => articleIds.Contains(sm.ArticleId) && locationIds.Contains(sm.StorageLocationId))
            .GroupBy(sm => new { sm.ArticleId, sm.StorageLocationId })
            .Select(g => new
            {
                g.Key.ArticleId,
                g.Key.StorageLocationId,
                Sum = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                    -sm.Quantity)
            })
            .ToListAsync();

        // 5. Pre-calculate stock: srcSum per (articleId, sourceLocationId) — 1 query
        var srcSums = await _context.StockMovements
            .Where(sm => articleIds.Contains(sm.ArticleId)
                      && sm.SourceStorageLocationId.HasValue
                      && locationIds.Contains(sm.SourceStorageLocationId.Value)
                      && sm.MovementType == MovementType.Umbuchung)
            .GroupBy(sm => new { sm.ArticleId, SourceLocationId = sm.SourceStorageLocationId!.Value })
            .Select(g => new
            {
                g.Key.ArticleId,
                g.Key.SourceLocationId,
                Sum = g.Sum(sm => sm.Quantity)
            })
            .ToListAsync();

        // Build running stock lookup: currentStock = destSum - srcSum
        var stockLookup = new Dictionary<(int articleId, int locationId), decimal>();
        foreach (var key in stockKeys)
        {
            var dest = destSums.FirstOrDefault(d => d.ArticleId == key.articleId && d.StorageLocationId == key.locationId)?.Sum ?? 0;
            var src = srcSums.FirstOrDefault(s => s.ArticleId == key.articleId && s.SourceLocationId == key.locationId)?.Sum ?? 0;
            stockLookup[key] = dest - src;
        }

        // 6. Pre-load negative booking settings ONCE (1-2 queries via repository)
        var negativErlaubt = (await _settingRepository.GetValueAsync("NegativeBuchungErlaubt"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        string? negativLagerplatzCode = null;
        int? negativLagerplatzId = null;
        if (negativErlaubt)
        {
            negativLagerplatzCode = await _settingRepository.GetValueAsync("NegativeBuchungLagerplatz") ?? "NAN";
            var negativLagerplatz = await _context.StorageLocations
                .FirstOrDefaultAsync(sl => sl.Code == negativLagerplatzCode);
            negativLagerplatzId = negativLagerplatz?.Id;
        }

        // 7. Process items — NO DB calls in loop
        var now = DateTime.Now;
        var transferredCount = 0;

        // Use transaction if supported (InMemory provider does not support transactions)
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        try
        {
            try
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }
            catch (InvalidOperationException)
            {
                // InMemory provider — proceed without transaction
            }

            foreach (var item in pickedItems)
            {
                if (!item.SourceStorageLocationId.HasValue) continue;

                if (!articleLookup.TryGetValue(item.BomArticleNumber, out var article)) continue;

                var key = (article.Id, item.SourceStorageLocationId.Value);
                var currentStock = stockLookup.GetValueOrDefault(key, 0);
                var sourceLocationId = item.SourceStorageLocationId.Value;

                if (currentStock < item.Quantity)
                {
                    if (!negativErlaubt)
                    {
                        throw new InvalidOperationException(
                            $"Nicht genügend Bestand für {item.BomArticleNumber} am Quell-Lagerplatz. " +
                            $"Verfügbar: {currentStock:N3}, Benötigt: {item.Quantity:N3}");
                    }

                    if (negativLagerplatzId.HasValue)
                        sourceLocationId = negativLagerplatzId.Value;

                    _logger.LogWarning(
                        "Bestand nicht verfügbar für {Article} (Verfügbar: {Stock}, Benötigt: {Needed}). Buche vom Lagerplatz {Location}.",
                        item.BomArticleNumber, currentStock, item.Quantity, negativLagerplatzCode);
                }

                // CRITICAL: Decrement running stock counter for duplicate article+location handling
                stockLookup[key] = currentStock - item.Quantity;

                _context.StockMovements.Add(new StockMovement
                {
                    ArticleId = article.Id,
                    Quantity = item.Quantity,
                    StorageLocationId = targetStorageLocationId,
                    SourceStorageLocationId = sourceLocationId,
                    ProductionOrder = productionOrder,
                    MovementType = MovementType.Umbuchung,
                    Timestamp = now,
                    UserId = appUserId,
                    WindowsUser = windowsUser,
                    CreatedAt = now,
                    CreatedBy = displayName,
                    CreatedByWindows = windowsUser
                });

                item.IsTransferred = true;
                item.TransferredAt = now;
                transferredCount++;
            }

            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            _logger.LogInformation(
                "TransferPicked: {Count} Artikel umgebucht. ProductionOrder: {ProductionOrder}",
                transferredCount, productionOrder);

            return transferredCount;
        }
        catch (Exception ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            _logger.LogError(ex,
                "TransferPicked fehlgeschlagen für FA-Id {OrderId}. Transaktion zurückgerollt.",
                productionOrderId);
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }
}
