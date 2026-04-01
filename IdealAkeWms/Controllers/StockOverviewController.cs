using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class StockOverviewController : Controller
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IAppSettingRepository _settingRepository;

    public StockOverviewController(
        IStockMovementRepository stockMovementRepository,
        IStorageLocationRepository storageLocationRepository,
        IAppSettingRepository settingRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _storageLocationRepository = storageLocationRepository;
        _settingRepository = settingRepository;
    }

    public async Task<IActionResult> Index(
        string? filterArticle, int? filterStorageLocationId,
        decimal? filterMinQuantity, decimal? filterMaxQuantity,
        string? filterProductionOrder)
    {
        var items = await _stockMovementRepository.GetCurrentStockAsync(
            filterArticle, filterStorageLocationId,
            filterMinQuantity, filterMaxQuantity,
            filterProductionOrder);

        var vm = new StockOverviewViewModel
        {
            Items = items,
            FilterArticle = filterArticle,
            FilterStorageLocationId = filterStorageLocationId,
            FilterMinQuantity = filterMinQuantity,
            FilterMaxQuantity = filterMaxQuantity,
            FilterProductionOrder = filterProductionOrder,
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
            WarningThresholdPercent = await _settingRepository.GetIntValueAsync("WarningThresholdPercent", 150),
            CriticalThresholdPercent = await _settingRepository.GetIntValueAsync("CriticalThresholdPercent", 100)
        };

        return View(vm);
    }
}
