using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

public class StockMovementsController : Controller
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IPartRequisitionRepository _partRequisitionRepository;

    public StockMovementsController(
        IStockMovementRepository stockMovementRepository,
        IArticleRepository articleRepository,
        IStorageLocationRepository storageLocationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IPartRequisitionRepository partRequisitionRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _articleRepository = articleRepository;
        _storageLocationRepository = storageLocationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _partRequisitionRepository = partRequisitionRepository;
    }

    [RequireStockReadAccess]
    public async Task<IActionResult> Index(
        DateTime? dateFrom, DateTime? dateTo,
        string? filterArticle, int? filterStorageLocationId,
        MovementType? filterMovementType, int? filterUserId,
        string? filterProductionOrder,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var (items, totalCount) = await _stockMovementRepository.GetMovementHistoryAsync(
            dateFrom, dateTo, filterArticle, filterStorageLocationId,
            filterMovementType, filterUserId, filterProductionOrder,
            page, effectivePageSize, columnFilters);

        var vm = new MovementHistoryViewModel
        {
            Items = items,
            FilterDateFrom = dateFrom,
            FilterDateTo = dateTo,
            FilterArticle = filterArticle,
            FilterStorageLocationId = filterStorageLocationId,
            FilterMovementType = filterMovementType,
            FilterUserId = filterUserId,
            FilterProductionOrder = filterProductionOrder,
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
            Users = await _userRepository.GetActiveUsersAsync(),
            Page = page,
            PageSize = effectivePageSize,
            TotalCount = totalCount,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            }
        };

        return View(vm);
    }

    [RequireStockAccess]
    public async Task<IActionResult> Inbound()
    {
        var vm = new StockMovementCreateViewModel
        {
            // Standardmenge 1 (Teil-5): bewusst NUR hier im Inbound-GET, nicht am geteilten
            // StockMovementCreateViewModel.Quantity-Property (das nutzt auch die Ausbuchung).
            Quantity = 1,
            StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
            Users = await _userRepository.GetActiveUsersAsync()
        };
        ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var bestellungenAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.BestellungenAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.BestellungenAktiv = bestellungenAktiv;
        return View(vm);
    }

    [RequireStockAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inbound(StockMovementCreateViewModel vm, List<int>? fulfilledRequisitionIds)
    {
        if (!ModelState.IsValid)
        {
            vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
            vm.Users = await _userRepository.GetActiveUsersAsync();
            if (vm.ArticleId > 0)
            {
                var article = await _articleRepository.GetByIdAsync(vm.ArticleId);
                if (article != null)
                    vm.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
            ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            ViewBag.BestellungenAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.BestellungenAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            return View(vm);
        }

        var appUserId = _currentUserService.GetCurrentAppUserId();

        var movement = new StockMovement
        {
            ArticleId = vm.ArticleId,
            Quantity = vm.Quantity,
            StorageLocationId = vm.StorageLocationId,
            ProductionOrder = vm.ProductionOrder,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now,
            UserId = appUserId,
            WindowsUser = _currentUserService.GetWindowsUserName(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _stockMovementRepository.AddAsync(movement);

        // Bedarfsmeldungen erfüllen
        if (fulfilledRequisitionIds != null && fulfilledRequisitionIds.Count > 0)
        {
            foreach (var reqId in fulfilledRequisitionIds)
            {
                await _partRequisitionRepository.FulfillAsync(
                    reqId, movement.Id,
                    _currentUserService.GetDisplayName(),
                    _currentUserService.GetWindowsUserName());
            }
        }

        TempData["SuccessMessage"] = "Einbuchung erfolgreich gespeichert.";
        return RedirectToAction(nameof(Inbound));
    }

    // ========== Mehrfachartikel-Einbuchung (ein Lagerplatz + eine FA, viele Zeilen) ==========

    [RequireStockAccess]
    public async Task<IActionResult> InboundBulk()
    {
        var vm = new StockMovementBulkInboundViewModel
        {
            StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
            Lines = new List<StockMovementBulkInboundLine> { new() { Quantity = 1m } }
        };
        ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        return View(vm);
    }

    [RequireStockAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    // S3: bewusst keine kuenstliche Zeilen-Obergrenze. Wir heben aber die ASP.NET-Core-
    // Standardgrenze (ValueCountLimit ~1024 Form-Felder) deutlich an, damit auch grosse
    // Sammel-Wareneingaenge sauber binden statt still mit HTTP-400 zu scheitern. Eine sprechende
    // Client-Warnung greift lange vorher (siehe InboundBulk.cshtml, MAX_LINES_SOFT).
    [RequestFormLimits(ValueCountLimit = 16384)]
    public async Task<IActionResult> InboundBulk(StockMovementBulkInboundViewModel vm)
    {
        var lines = vm.Lines ?? new List<StockMovementBulkInboundLine>();

        // S2: ALLE Zeilen VOR dem ersten AddAsync validieren (keine Teilbuchung).
        var anyLineError = false;
        foreach (var line in lines)
        {
            if (line.ArticleId <= 0)
            {
                line.Error = "Artikel ist erforderlich.";
                anyLineError = true;
            }
            else if (line.Quantity <= 0)
            {
                line.Error = "Menge muss größer als 0 sein.";
                anyLineError = true;
            }
        }

        if (lines.Count == 0)
            ModelState.AddModelError("", "Mindestens eine Artikel-Zeile ist erforderlich.");

        // [Required] weist bei non-nullable int den Wert 0 NICHT ab — expliziter Guard gegen
        // einen manipulierten POST (StorageLocationId=0), der sonst eine FK-Exception mitten in
        // der Buchungsschleife auslösen würde.
        if (vm.StorageLocationId <= 0)
        {
            var slEntry = ModelState[nameof(vm.StorageLocationId)];
            if (slEntry == null || slEntry.Errors.Count == 0)
                ModelState.AddModelError(nameof(vm.StorageLocationId), "Lagerplatz ist erforderlich");
        }

        if (!ModelState.IsValid || anyLineError)
        {
            if (anyLineError)
                ModelState.AddModelError("", "Bitte korrigieren Sie die markierten Zeilen — es wurde nichts gebucht.");
            await PopulateBulkInboundAsync(vm);
            return View(vm);
        }

        // Alle Zeilen gueltig → je Zeile eine Einbuchung mit gemeinsamem Timestamp.
        // S1: Buchung ausschliesslich ueber IStockMovementRepository.AddAsync (identischer Pfad
        // wie die Einzel-Einbuchung inkl. Audit UND Sage-Enqueue-Decorator, v1.28.0) — KEIN
        // Direkt-DbContext-Bypass, sonst erreichten die Bulk-Einbuchungen Sage nie.
        var appUserId = _currentUserService.GetCurrentAppUserId();
        var windowsUser = _currentUserService.GetWindowsUserName();
        var displayName = _currentUserService.GetDisplayName();
        var now = DateTime.Now;
        var count = 0;

        foreach (var line in lines)
        {
            var movement = new StockMovement
            {
                ArticleId = line.ArticleId,
                Quantity = line.Quantity,
                StorageLocationId = vm.StorageLocationId,
                ProductionOrder = vm.ProductionOrder,
                MovementType = MovementType.Einbuchung,
                Timestamp = now,
                UserId = appUserId,
                WindowsUser = windowsUser,
                CreatedAt = now,
                CreatedBy = displayName,
                CreatedByWindows = windowsUser
            };
            await _stockMovementRepository.AddAsync(movement);
            count++;
        }

        TempData["SuccessMessage"] = $"{count} Artikel erfolgreich eingebucht.";
        return RedirectToAction(nameof(InboundBulk));
    }

    private async Task PopulateBulkInboundAsync(StockMovementBulkInboundViewModel vm)
    {
        vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
        ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        // Anzeigetexte fuer bereits gewaehlte Artikel wiederherstellen (Select2-Re-Render).
        foreach (var line in vm.Lines)
        {
            if (line.ArticleId > 0 && string.IsNullOrEmpty(line.ArticleDisplay))
            {
                var article = await _articleRepository.GetByIdAsync(line.ArticleId);
                if (article != null)
                    line.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
        }
    }

    [RequireStockAccess]
    public async Task<IActionResult> Outbound()
    {
        var vm = new StockMovementCreateViewModel
        {
            StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
            Users = await _userRepository.GetActiveUsersAsync()
        };
        ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        return View(vm);
    }

    [RequireStockAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Outbound(StockMovementCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
            vm.Users = await _userRepository.GetActiveUsersAsync();
            if (vm.ArticleId > 0)
            {
                var article = await _articleRepository.GetByIdAsync(vm.ArticleId);
                if (article != null)
                    vm.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
            ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            return View(vm);
        }

        // Bestandsprüfung: genug Bestand am Lagerplatz?
        var currentStock = await _stockMovementRepository.GetCurrentStockAtLocationAsync(
            vm.ArticleId, vm.StorageLocationId);
        var storageLocationId = vm.StorageLocationId;
        string? warningMessage = null;

        if (currentStock < vm.Quantity)
        {
            var negativErlaubt = (await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungErlaubt))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (!negativErlaubt)
            {
                ModelState.AddModelError("", $"Nicht genügend Bestand. Verfügbar: {currentStock:N3}");
                vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
                vm.Users = await _userRepository.GetActiveUsersAsync();
                if (vm.ArticleId > 0)
                {
                    var art = await _articleRepository.GetByIdAsync(vm.ArticleId);
                    if (art != null)
                        vm.ArticleDisplay = art.ArticleNumber + (art.Description != null ? " - " + art.Description : "");
                }
                return View(vm);
            }

            // Negative Buchung erlaubt: vom Default-Lagerplatz buchen
            var negativLagerplatzCode = await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungLagerplatz) ?? "NAN";
            var negativLagerplatz = await _storageLocationRepository.GetByCodeAsync(negativLagerplatzCode);
            if (negativLagerplatz != null)
            {
                storageLocationId = negativLagerplatz.Id;
                warningMessage = $"Lagerstand nicht verfügbar (Bestand: {currentStock:N3}), buche vom Lagerplatz {negativLagerplatzCode} ab.";
            }
        }

        var appUserId = _currentUserService.GetCurrentAppUserId();

        var movement = new StockMovement
        {
            ArticleId = vm.ArticleId,
            Quantity = vm.Quantity,
            StorageLocationId = storageLocationId,
            ProductionOrder = vm.ProductionOrder,
            MovementType = MovementType.Ausbuchung,
            Timestamp = DateTime.Now,
            UserId = appUserId,
            WindowsUser = _currentUserService.GetWindowsUserName(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _stockMovementRepository.AddAsync(movement);
        if (warningMessage != null)
            TempData["WarningMessage"] = warningMessage;
        TempData["SuccessMessage"] = "Ausbuchung erfolgreich gespeichert.";
        return RedirectToAction(nameof(Outbound));
    }

    [RequireStockAccess]
    public async Task<IActionResult> Transfer()
    {
        var vm = new StockTransferViewModel
        {
            StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync()
        };
        ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        return View(vm);
    }

    [RequireStockAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(StockTransferViewModel vm)
    {
        if (vm.SourceStorageLocationId == vm.StorageLocationId && vm.SourceStorageLocationId > 0)
        {
            ModelState.AddModelError("", "Quell- und Ziel-Lagerplatz dürfen nicht identisch sein.");
        }

        if (!ModelState.IsValid)
        {
            vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
            ViewBag.QrMitFaNummer = (await _settingRepository.GetValueAsync(AppSettingKeys.QrMitFaNummer))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (vm.ArticleId > 0)
            {
                var article = await _articleRepository.GetByIdAsync(vm.ArticleId);
                if (article != null)
                    vm.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
            return View(vm);
        }

        // Bestandsprüfung: genug Bestand am Quell-Lagerplatz?
        var currentStockTransfer = await _stockMovementRepository.GetCurrentStockAtLocationAsync(
            vm.ArticleId, vm.SourceStorageLocationId);
        var sourceLocationId = vm.SourceStorageLocationId;
        string? transferWarning = null;

        if (currentStockTransfer < vm.Quantity)
        {
            var negativErlaubt = (await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungErlaubt))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (!negativErlaubt)
            {
                ModelState.AddModelError("", $"Nicht genügend Bestand am Quell-Lagerplatz. Verfügbar: {currentStockTransfer:N3}");
                vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
                if (vm.ArticleId > 0)
                {
                    var art = await _articleRepository.GetByIdAsync(vm.ArticleId);
                    if (art != null)
                        vm.ArticleDisplay = art.ArticleNumber + (art.Description != null ? " - " + art.Description : "");
                }
                return View(vm);
            }

            var negativLagerplatzCode = await _settingRepository.GetValueAsync(AppSettingKeys.NegativeBuchungLagerplatz) ?? "NAN";
            var negativLagerplatz = await _storageLocationRepository.GetByCodeAsync(negativLagerplatzCode);
            if (negativLagerplatz != null)
            {
                sourceLocationId = negativLagerplatz.Id;
                transferWarning = $"Lagerstand nicht verfügbar (Bestand: {currentStockTransfer:N3}), buche vom Lagerplatz {negativLagerplatzCode} um.";
            }
        }

        var appUserId = _currentUserService.GetCurrentAppUserId();

        var movement = new StockMovement
        {
            ArticleId = vm.ArticleId,
            Quantity = vm.Quantity,
            StorageLocationId = vm.StorageLocationId,
            SourceStorageLocationId = sourceLocationId,
            ProductionOrder = vm.ProductionOrder,
            MovementType = MovementType.Umbuchung,
            Timestamp = DateTime.Now,
            UserId = appUserId,
            WindowsUser = _currentUserService.GetWindowsUserName(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _stockMovementRepository.AddAsync(movement);
        if (transferWarning != null)
            TempData["WarningMessage"] = transferWarning;
        TempData["SuccessMessage"] = "Umbuchung erfolgreich gespeichert.";
        return RedirectToAction(nameof(Transfer));
    }

    [RequireStockKeyUserAccess]
    public async Task<IActionResult> OutboundAll(int? storageLocationId)
    {
        var vm = new OutboundAllViewModel
        {
            StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
            StorageLocationId = storageLocationId
        };

        if (storageLocationId.HasValue)
        {
            var location = await _storageLocationRepository.GetByIdAsync(storageLocationId.Value);
            vm.StorageLocationCode = location?.Code;
            vm.IsPickingTransport = location?.IsPickingTransport ?? false;

            var allStock = await _stockMovementRepository.GetCurrentStockAsync(
                filterStorageLocationId: storageLocationId.Value);
            vm.Items = allStock.Where(s => s.CurrentQuantity > 0).ToList();

            // Bei Kommissionierwagen: neueste FA-Nummer automatisch ermitteln
            if (vm.IsPickingTransport && string.IsNullOrEmpty(vm.ProductionOrder))
            {
                var waNumbers = await _stockMovementRepository.GetProductionOrdersAtLocationAsync(storageLocationId.Value);
                if (waNumbers.Count > 0)
                    vm.ProductionOrder = string.Join("; ", waNumbers);
            }
        }

        return View(vm);
    }

    [RequireStockKeyUserAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OutboundAllConfirm(int storageLocationId, string? productionOrder)
    {
        var allStock = await _stockMovementRepository.GetCurrentStockAsync(
            filterStorageLocationId: storageLocationId);
        var itemsToOutbound = allStock.Where(s => s.CurrentQuantity > 0).ToList();

        if (!itemsToOutbound.Any())
        {
            TempData["WarningMessage"] = "Keine Artikel mit positivem Bestand auf diesem Lagerplatz.";
            return RedirectToAction(nameof(OutboundAll), new { storageLocationId });
        }

        var appUserId = _currentUserService.GetCurrentAppUserId();
        var now = DateTime.Now;
        var count = 0;

        foreach (var item in itemsToOutbound)
        {
            var movement = new StockMovement
            {
                ArticleId = item.ArticleId,
                Quantity = item.CurrentQuantity,
                StorageLocationId = storageLocationId,
                ProductionOrder = productionOrder,
                MovementType = MovementType.Ausbuchung,
                Timestamp = now,
                UserId = appUserId,
                WindowsUser = _currentUserService.GetWindowsUserName(),
                CreatedAt = now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };
            await _stockMovementRepository.AddAsync(movement);
            count++;
        }

        TempData["SuccessMessage"] = $"{count} Artikel erfolgreich ausgebucht.";
        return RedirectToAction(nameof(OutboundAll));
    }

    // ========== Lagerplatz-Umbuchung ==========

    [RequireStockKeyUserAccess]
    public async Task<IActionResult> LocationTransfer(int? sourceStorageLocationId)
    {
        var allLocations = await _storageLocationRepository.GetAllOrderedAsync();
        var vm = new LocationTransferViewModel
        {
            SourceStorageLocationId = sourceStorageLocationId,
            AllStorageLocations = allLocations
        };

        if (sourceStorageLocationId.HasValue)
        {
            var source = allLocations.FirstOrDefault(l => l.Id == sourceStorageLocationId.Value);
            vm.SourceStorageLocationCode = source?.Code;

            var stock = await _stockMovementRepository.GetCurrentStockAsync(
                filterStorageLocationId: sourceStorageLocationId.Value);
            vm.SourceItems = stock.Where(s => s.CurrentQuantity > 0).ToList();
        }

        return View(vm);
    }

    [RequireStockKeyUserAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationTransferConfirm(int sourceStorageLocationId, int targetStorageLocationId)
    {
        if (sourceStorageLocationId == targetStorageLocationId)
        {
            TempData["WarningMessage"] = "Quell- und Ziel-Lagerplatz dürfen nicht identisch sein.";
            return RedirectToAction(nameof(LocationTransfer), new { sourceStorageLocationId });
        }

        var sourceLocation = await _storageLocationRepository.GetByIdAsync(sourceStorageLocationId);
        var targetLocation = await _storageLocationRepository.GetByIdAsync(targetStorageLocationId);
        if (sourceLocation == null || targetLocation == null)
        {
            TempData["WarningMessage"] = "Ungültiger Lagerplatz.";
            return RedirectToAction(nameof(LocationTransfer));
        }

        var stock = await _stockMovementRepository.GetCurrentStockAsync(
            filterStorageLocationId: sourceStorageLocationId);
        var itemsToTransfer = stock.Where(s => s.CurrentQuantity > 0).ToList();

        if (!itemsToTransfer.Any())
        {
            TempData["WarningMessage"] = $"Keine Artikel mit positivem Bestand auf Lagerplatz {sourceLocation.Code}.";
            return RedirectToAction(nameof(LocationTransfer), new { sourceStorageLocationId });
        }

        var appUserId = _currentUserService.GetCurrentAppUserId();
        var now = DateTime.Now;

        foreach (var item in itemsToTransfer)
        {
            var movement = new StockMovement
            {
                ArticleId = item.ArticleId,
                Quantity = item.CurrentQuantity,
                StorageLocationId = targetStorageLocationId,
                SourceStorageLocationId = sourceStorageLocationId,
                MovementType = MovementType.Umbuchung,
                Timestamp = now,
                UserId = appUserId,
                WindowsUser = _currentUserService.GetWindowsUserName(),
                CreatedAt = now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };
            await _stockMovementRepository.AddAsync(movement);
        }

        TempData["SuccessMessage"] = $"{itemsToTransfer.Count} Artikel von {sourceLocation.Code} nach {targetLocation.Code} umgebucht.";
        return RedirectToAction(nameof(LocationTransfer));
    }
}
