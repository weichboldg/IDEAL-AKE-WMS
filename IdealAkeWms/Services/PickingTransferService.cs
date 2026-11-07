using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>
/// Kommissionier-Umbuchung. Seit v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E3) auf Zeilen eines Kommissionierauftrags
/// statt auf <see cref="PickingItem"/>: gebucht wird je Zeile die Ist-Menge (&gt; 0, ≤ Soll, Server prueft) als <see cref="MovementType.Umbuchung"/>
/// Quelle → Ziel, mit Bestandspruefung/NAN-Fallback wie bisher. Die Bewegung traegt den FA-Text (Wagen-Liste bei „trotzdem buchen“) UND
/// den verknuepften FA (<see cref="StockMovement.ProductionOrderId"/>, Teil A), die Zeile die <c>StockMovementId</c>.
/// Geschrieben wird bewusst DIREKT ueber den Context (nicht ueber <see cref="IStockMovementRepository.AddAsync"/>): Kommissionier-Umbuchungen
/// gehen nie an Sage (fallstricke „Kommissionier-Umbuchung bleibt am Sage-Decorator vorbei“).
/// </summary>
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

    public async Task<PickingTransferResult> TransferOrderLinesAsync(
        int pickingOrderId,
        int targetStorageLocationId,
        bool forceTransfer,
        List<PickingSelectionItem> selectedItems,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        var auftrag = await _context.PickingOrders.Include(o => o.Zeilen).Include(o => o.ProductionOrder)
            .FirstOrDefaultAsync(o => o.Id == pickingOrderId)
            ?? throw new InvalidOperationException("Kommissionierauftrag nicht gefunden.");
        if (auftrag.Status is not (PickingOrderStatus.Offen or PickingOrderStatus.InArbeit))
            throw new InvalidOperationException($"Auftrag {auftrag.Nummer} ist {PickingOrderStatusText(auftrag.Status)} – Umbuchen nicht möglich.");
        if (selectedItems == null || selectedItems.Count == 0)
            throw new InvalidOperationException("Keine Artikel zum Umbuchen ausgewählt.");

        // Auswahl pruefen (alles vor der ersten Buchung): Zeile gehoert zum Auftrag, ist nicht umgebucht, Ist-Menge > 0 und ≤ Soll.
        var auswahl = new List<(PickingOrderLine Zeile, decimal Ist, PickingSelectionItem Item)>();
        foreach (var item in selectedItems)
        {
            var zeile = auftrag.Zeilen.FirstOrDefault(z => z.Id == item.PickingItemId)
                ?? throw new InvalidOperationException($"Zeile {item.PickingItemId} gehört nicht zu Auftrag {auftrag.Nummer}.");
            if (zeile.Status is not (PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt))
                throw new InvalidOperationException($"{zeile.ArticleNumber} (Pos. {zeile.BomPosition}) ist bereits umgebucht oder abgeschlossen.");
            decimal ist;
            if (string.IsNullOrWhiteSpace(item.IstMenge)) ist = zeile.SollMenge;
            else if (!BeschichtungsauftraegeController.TryMenge(item.IstMenge, out ist))
                throw new InvalidOperationException($"{zeile.ArticleNumber}: Ist-Menge „{item.IstMenge}“ ist keine Zahl.");
            if (ist <= 0m || ist > zeile.SollMenge)
                throw new InvalidOperationException($"{zeile.ArticleNumber}: Ist-Menge {ist:0.###} muss größer als 0 und höchstens Soll {zeile.SollMenge:0.###} sein.");
            if (!item.SourceStorageLocationId.HasValue)
                throw new InvalidOperationException($"{zeile.ArticleNumber}: Quell-Lagerplatz wählen.");
            auswahl.Add((zeile, ist, item));
        }

        var faText = auftrag.ProductionOrder?.OrderNumber;
        var targetLocation = await _context.StorageLocations.FindAsync(targetStorageLocationId)
            ?? throw new InvalidOperationException("Ziel-Lagerplatz nicht gefunden.");

        // Kommissionierwagen-Konfliktpruefung (unveraendert)
        if (targetLocation.IsPickingTransport)
        {
            var existingOrders = await _stockMovementRepository.GetProductionOrdersAtLocationAsync(targetStorageLocationId);
            if (!forceTransfer)
            {
                var conflicting = existingOrders.Where(o => !string.IsNullOrEmpty(o) && o != faText).ToList();
                if (conflicting.Any())
                {
                    return new PickingTransferResult
                    {
                        Success = false,
                        IsPickingTransportConflict = true,
                        ConflictStorageLocationId = targetStorageLocationId,
                        ConflictStorageLocationCode = targetLocation.Code,
                        CurrentWaNumbers = string.Join("; ", existingOrders.Where(o => !string.IsNullOrEmpty(o)).Distinct()),
                        NewWaNumber = faText
                    };
                }
            }
            else
            {
                // „Trotzdem buchen“: FA-Nummern am Wagen zusammenfuehren (Text); der FK bleibt der Herkunfts-FA der Zeile.
                faText = string.Join(";", existingOrders.Concat(new[] { faText ?? "" }).Where(x => !string.IsNullOrEmpty(x)).Distinct());
            }
        }

        var count = await DoTransferAsync(auftrag, auswahl, targetStorageLocationId, faText, appUserId, displayName, windowsUser);
        return new PickingTransferResult { Success = true, TransferredCount = count };
    }

    private static string PickingOrderStatusText(PickingOrderStatus s) => Models.ViewModels.PickingOrderListItem.StatusText(s).ToLowerInvariant();

    private async Task<int> DoTransferAsync(
        PickingOrder auftrag,
        List<(PickingOrderLine Zeile, decimal Ist, PickingSelectionItem Item)> auswahl,
        int targetStorageLocationId,
        string? productionOrderText,
        int? appUserId,
        string displayName,
        string windowsUser)
    {
        // 1. Artikel (1 Abfrage)
        var neededArticleNumbers = auswahl.Select(a => a.Zeile.ArticleNumber).Distinct().ToList();
        var articleLookup = await _context.Articles
            .Where(a => neededArticleNumbers.Contains(a.ArticleNumber))
            .ToDictionaryAsync(a => a.ArticleNumber);

        // 2. Bestand je (Artikel, Quelle) vorab (2 Abfragen)
        var stockKeys = new HashSet<(int articleId, int locationId)>();
        foreach (var a in auswahl)
            if (articleLookup.TryGetValue(a.Zeile.ArticleNumber, out var art))
                stockKeys.Add((art.Id, a.Item.SourceStorageLocationId!.Value));

        var articleIds = stockKeys.Select(k => k.articleId).Distinct().ToList();
        var locationIds = stockKeys.Select(k => k.locationId).Distinct().ToList();

        var destSums = await _context.StockMovements
            .Where(sm => articleIds.Contains(sm.ArticleId) && locationIds.Contains(sm.StorageLocationId))
            .GroupBy(sm => new { sm.ArticleId, sm.StorageLocationId })
            .Select(g => new
            {
                g.Key.ArticleId,
                g.Key.StorageLocationId,
                Sum = g.Sum(sm =>
                    sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Umbuchung
                        ? sm.Quantity :
                    sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
                        ? -sm.Quantity :
                    0m)
            })
            .ToListAsync();

        var srcSums = await _context.StockMovements
            .Where(sm => articleIds.Contains(sm.ArticleId)
                      && sm.SourceStorageLocationId.HasValue
                      && locationIds.Contains(sm.SourceStorageLocationId.Value)
                      && sm.MovementType == MovementType.Umbuchung)
            .GroupBy(sm => new { sm.ArticleId, SourceLocationId = sm.SourceStorageLocationId!.Value })
            .Select(g => new { g.Key.ArticleId, g.Key.SourceLocationId, Sum = g.Sum(sm => sm.Quantity) })
            .ToListAsync();

        var stockLookup = new Dictionary<(int articleId, int locationId), decimal>();
        foreach (var key in stockKeys)
        {
            var dest = destSums.FirstOrDefault(d => d.ArticleId == key.articleId && d.StorageLocationId == key.locationId)?.Sum ?? 0;
            var src = srcSums.FirstOrDefault(s => s.ArticleId == key.articleId && s.SourceLocationId == key.locationId)?.Sum ?? 0;
            stockLookup[key] = dest - src;
        }

        // 3. Minus-Buchung (einmal)
        var negativErlaubt = (await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungErlaubt))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        string? negativLagerplatzCode = null;
        int? negativLagerplatzId = null;
        if (negativErlaubt)
        {
            negativLagerplatzCode = await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungLagerplatz) ?? "NAN";
            negativLagerplatzId = (await _context.StorageLocations.FirstOrDefaultAsync(sl => sl.Code == negativLagerplatzCode))?.Id;
        }

        // 4. Buchen – keine DB-Aufrufe in der Schleife
        var now = DateTime.Now;
        var transferredCount = 0;
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        try
        {
            try { transaction = await _context.Database.BeginTransactionAsync(); }
            catch (InvalidOperationException) { /* InMemory: ohne Transaktion */ }

            foreach (var (zeile, ist, item) in auswahl)
            {
                if (!articleLookup.TryGetValue(zeile.ArticleNumber, out var article))
                    throw new InvalidOperationException($"Artikel {zeile.ArticleNumber} ist im WMS unbekannt – nicht umbuchbar.");

                var sourceLocationId = item.SourceStorageLocationId!.Value;
                var key = (article.Id, sourceLocationId);
                var currentStock = stockLookup.GetValueOrDefault(key, 0);
                if (currentStock < ist)
                {
                    if (!negativErlaubt)
                        throw new InvalidOperationException(
                            $"Nicht genügend Bestand für {zeile.ArticleNumber} am Quell-Lagerplatz. Verfügbar: {currentStock:N3}, Benötigt: {ist:N3}");
                    if (negativLagerplatzId.HasValue) sourceLocationId = negativLagerplatzId.Value;
                    _logger.LogWarning("Bestand nicht verfügbar für {Article} (Verfügbar: {Stock}, Benötigt: {Needed}). Buche vom Lagerplatz {Location}.",
                        zeile.ArticleNumber, currentStock, ist, negativLagerplatzCode);
                }
                stockLookup[key] = currentStock - ist; // doppelter Artikel am selben Platz

                var bewegung = new StockMovement
                {
                    ArticleId = article.Id,
                    Quantity = ist,
                    StorageLocationId = targetStorageLocationId,
                    SourceStorageLocationId = sourceLocationId,
                    ProductionOrder = productionOrderText,
                    ProductionOrderId = zeile.ProductionOrderId, // Teil A: Herkunfts-FA der Zeile, nicht die Wagen-Liste
                    MovementType = MovementType.Umbuchung,
                    Timestamp = now,
                    UserId = appUserId,
                    WindowsUser = windowsUser,
                    CreatedAt = now,
                    CreatedBy = displayName,
                    CreatedByWindows = windowsUser
                };
                _context.StockMovements.Add(bewegung);

                zeile.IstMenge = ist;
                zeile.IsBaugruppe = item.IsBaugruppe;
                zeile.SourceStorageLocationId = item.SourceStorageLocationId;
                zeile.Status = PickingOrderLineStatus.Umgebucht;
                zeile.PickedAt ??= now;
                zeile.PickedBy ??= displayName;
                zeile.PickedByWindows ??= windowsUser;
                zeile.TransferredAt = now;
                zeile.StockMovement = bewegung;
                zeile.ModifiedAt = now;
                zeile.ModifiedBy = displayName;
                zeile.ModifiedByWindows = windowsUser;
                transferredCount++;
            }

            if (auftrag.Status == PickingOrderStatus.Offen)
            {
                auftrag.Status = PickingOrderStatus.InArbeit;
                auftrag.ModifiedAt = now;
                auftrag.ModifiedBy = displayName;
                auftrag.ModifiedByWindows = windowsUser;
            }

            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            _logger.LogInformation("Kommissionierauftrag {Nummer}: {Count} Zeile(n) umgebucht. FA: {Fa}", auftrag.Nummer, transferredCount, productionOrderText);
            return transferredCount;
        }
        catch (Exception ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            _context.ChangeTracker.Clear(); // keine halben Zeilen-/Bewegungsstaende im Context lassen
            _logger.LogError(ex, "Umbuchung Kommissionierauftrag {Nummer} fehlgeschlagen. Transaktion zurückgerollt.", auftrag.Nummer);
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }
}
