using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class StockMovementsController : Controller
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;

    public StockMovementsController(
        IStockMovementRepository stockMovementRepository,
        IArticleRepository articleRepository,
        IStorageLocationRepository storageLocationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _articleRepository = articleRepository;
        _storageLocationRepository = storageLocationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
    }

    public async Task<IActionResult> Index(
        DateTime? dateFrom, DateTime? dateTo,
        string? filterArticle, int? filterStorageLocationId,
        MovementType? filterMovementType, int? filterUserId,
        string? filterProductionOrder,
        int page = 1, int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 10) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var (items, totalCount) = await _stockMovementRepository.GetMovementHistoryAsync(
            dateFrom, dateTo, filterArticle, filterStorageLocationId,
            filterMovementType, filterUserId, filterProductionOrder,
            page, pageSize);

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
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(vm);
    }

    public async Task<IActionResult> Inbound()
    {
        var vm = new StockMovementCreateViewModel
        {
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
            Users = await _userRepository.GetActiveUsersAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inbound(StockMovementCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.StorageLocations = await _storageLocationRepository.GetAllOrderedAsync();
            vm.Users = await _userRepository.GetActiveUsersAsync();
            if (vm.ArticleId > 0)
            {
                var article = await _articleRepository.GetByIdAsync(vm.ArticleId);
                if (article != null)
                    vm.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
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
        TempData["SuccessMessage"] = "Einbuchung erfolgreich gespeichert.";
        return RedirectToAction(nameof(Inbound));
    }

    public async Task<IActionResult> Outbound()
    {
        var vm = new StockMovementCreateViewModel
        {
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
            Users = await _userRepository.GetActiveUsersAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Outbound(StockMovementCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.StorageLocations = await _storageLocationRepository.GetAllOrderedAsync();
            vm.Users = await _userRepository.GetActiveUsersAsync();
            if (vm.ArticleId > 0)
            {
                var article = await _articleRepository.GetByIdAsync(vm.ArticleId);
                if (article != null)
                    vm.ArticleDisplay = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "");
            }
            return View(vm);
        }

        // Bestandsprüfung: genug Bestand am Lagerplatz?
        var currentStock = await _stockMovementRepository.GetCurrentStockAtLocationAsync(
            vm.ArticleId, vm.StorageLocationId);
        var storageLocationId = vm.StorageLocationId;
        string? warningMessage = null;

        if (currentStock < vm.Quantity)
        {
            var negativErlaubt = (await _settingRepository.GetValueAsync("NegativeBuchungErlaubt"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (!negativErlaubt)
            {
                ModelState.AddModelError("", $"Nicht genügend Bestand. Verfügbar: {currentStock:N3}");
                vm.StorageLocations = await _storageLocationRepository.GetAllOrderedAsync();
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
            var negativLagerplatzCode = await _settingRepository.GetValueAsync("NegativeBuchungLagerplatz") ?? "NAN";
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

    public async Task<IActionResult> Transfer()
    {
        var vm = new StockTransferViewModel
        {
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync()
        };
        return View(vm);
    }

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
            vm.StorageLocations = await _storageLocationRepository.GetAllOrderedAsync();
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
            var negativErlaubt = (await _settingRepository.GetValueAsync("NegativeBuchungErlaubt"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            if (!negativErlaubt)
            {
                ModelState.AddModelError("", $"Nicht genügend Bestand am Quell-Lagerplatz. Verfügbar: {currentStockTransfer:N3}");
                vm.StorageLocations = await _storageLocationRepository.GetAllOrderedAsync();
                if (vm.ArticleId > 0)
                {
                    var art = await _articleRepository.GetByIdAsync(vm.ArticleId);
                    if (art != null)
                        vm.ArticleDisplay = art.ArticleNumber + (art.Description != null ? " - " + art.Description : "");
                }
                return View(vm);
            }

            var negativLagerplatzCode = await _settingRepository.GetValueAsync("NegativeBuchungLagerplatz") ?? "NAN";
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

    public async Task<IActionResult> OutboundAll(int? storageLocationId)
    {
        var vm = new OutboundAllViewModel
        {
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
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

            // Bei Kommissionierwagen: neueste WA-Nummer automatisch ermitteln
            if (vm.IsPickingTransport && string.IsNullOrEmpty(vm.ProductionOrder))
            {
                var waNumbers = await _stockMovementRepository.GetProductionOrdersAtLocationAsync(storageLocationId.Value);
                if (waNumbers.Count > 0)
                    vm.ProductionOrder = string.Join("; ", waNumbers);
            }
        }

        return View(vm);
    }

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
}
