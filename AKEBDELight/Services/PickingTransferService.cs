using AKEBDELight.Data;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Services;

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
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        var order = await _context.ProductionOrders.FindAsync(productionOrderId);
        var targetLocation = await _context.StorageLocations.FindAsync(targetStorageLocationId);

        // Kommissionierwaagen-Konfliktprüfung
        if (targetLocation?.IsPickingScale == true && !forceTransfer)
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
                    IsPickingScaleConflict = true,
                    ConflictStorageLocationId = targetStorageLocationId,
                    ConflictStorageLocationCode = targetLocation.Code,
                    CurrentWaNumbers = string.Join("; ", existingOrders.Where(o => !string.IsNullOrEmpty(o)).Distinct()),
                    NewWaNumber = currentWa
                };
            }
        }

        // Bei forceTransfer: WA-Nummern zusammenführen
        string? productionOrderValue = order?.OrderNumber;
        if (forceTransfer && targetLocation?.IsPickingScale == true)
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
        var pickedItems = await _context.PickingItems
            .Where(p => p.ProductionOrderId == productionOrderId
                     && p.IsPicked && !p.IsTransferred)
            .ToListAsync();

        if (!pickedItems.Any())
            throw new InvalidOperationException("Keine gepickten Artikel zum Umbuchen vorhanden.");

        var now = DateTime.Now;
        var transferredCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in pickedItems)
            {
                if (!item.SourceStorageLocationId.HasValue) continue;

                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.ArticleNumber == item.BomArticleNumber);
                if (article == null) continue;

                // Bestandsprüfung am Quell-Lagerplatz
                var destSum = await _context.StockMovements
                    .Where(sm => sm.ArticleId == article.Id && sm.StorageLocationId == item.SourceStorageLocationId.Value)
                    .SumAsync(sm =>
                        sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
                        sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                        -sm.Quantity);
                var srcSum = await _context.StockMovements
                    .Where(sm => sm.ArticleId == article.Id
                              && sm.SourceStorageLocationId == item.SourceStorageLocationId.Value
                              && sm.MovementType == MovementType.Umbuchung)
                    .SumAsync(sm => sm.Quantity);
                var currentStock = destSum - srcSum;

                var sourceLocationId = item.SourceStorageLocationId.Value;

                if (currentStock < item.Quantity)
                {
                    var negativErlaubt = (await _settingRepository.GetValueAsync("NegativeBuchungErlaubt"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
                    if (!negativErlaubt)
                    {
                        throw new InvalidOperationException(
                            $"Nicht genügend Bestand für {item.BomArticleNumber} am Quell-Lagerplatz. " +
                            $"Verfügbar: {currentStock:N3}, Benötigt: {item.Quantity:N3}");
                    }

                    var negativLagerplatzCode = await _settingRepository.GetValueAsync("NegativeBuchungLagerplatz") ?? "NAN";
                    var negativLagerplatz = await _context.StorageLocations
                        .FirstOrDefaultAsync(sl => sl.Code == negativLagerplatzCode);
                    if (negativLagerplatz != null)
                        sourceLocationId = negativLagerplatz.Id;

                    _logger.LogWarning(
                        "Bestand nicht verfügbar für {Article} (Verfügbar: {Stock}, Benötigt: {Needed}). Buche vom Lagerplatz {Location}.",
                        item.BomArticleNumber, currentStock, item.Quantity, negativLagerplatzCode);
                }

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
            await transaction.CommitAsync();

            _logger.LogInformation(
                "TransferPicked: {Count} Artikel umgebucht. ProductionOrder: {ProductionOrder}",
                transferredCount, productionOrder);

            return transferredCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "TransferPicked fehlgeschlagen für WA-Id {OrderId}. Transaktion zurückgerollt.",
                productionOrderId);
            throw;
        }
    }
}
