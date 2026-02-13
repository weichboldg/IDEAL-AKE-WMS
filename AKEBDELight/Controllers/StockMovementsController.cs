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

    public StockMovementsController(
        IStockMovementRepository stockMovementRepository,
        IArticleRepository articleRepository,
        IStorageLocationRepository storageLocationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _stockMovementRepository = stockMovementRepository;
        _articleRepository = articleRepository;
        _storageLocationRepository = storageLocationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(
        DateTime? dateFrom, DateTime? dateTo,
        string? filterArticle, int? filterStorageLocationId,
        MovementType? filterMovementType, int? filterUserId,
        string? filterProductionOrder)
    {
        var items = await _stockMovementRepository.GetMovementHistoryAsync(
            dateFrom, dateTo, filterArticle, filterStorageLocationId,
            filterMovementType, filterUserId, filterProductionOrder);

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
            Users = await _userRepository.GetActiveUsersAsync()
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

        var appUserId = _currentUserService.GetCurrentAppUserId();

        var movement = new StockMovement
        {
            ArticleId = vm.ArticleId,
            Quantity = vm.Quantity,
            StorageLocationId = vm.StorageLocationId,
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

        var appUserId = _currentUserService.GetCurrentAppUserId();

        var movement = new StockMovement
        {
            ArticleId = vm.ArticleId,
            Quantity = vm.Quantity,
            StorageLocationId = vm.StorageLocationId,
            SourceStorageLocationId = vm.SourceStorageLocationId,
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
        TempData["SuccessMessage"] = "Umbuchung erfolgreich gespeichert.";
        return RedirectToAction(nameof(Transfer));
    }
}
