using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models.ViewModels;

namespace AKEBDELight.Controllers;

public class StockOverviewController : Controller
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;

    public StockOverviewController(
        IStockMovementRepository stockMovementRepository,
        IStorageLocationRepository storageLocationRepository)
    {
        _stockMovementRepository = stockMovementRepository;
        _storageLocationRepository = storageLocationRepository;
    }

    public async Task<IActionResult> Index(
        string? filterArticle, int? filterStorageLocationId,
        decimal? filterMinQuantity, decimal? filterMaxQuantity)
    {
        var items = await _stockMovementRepository.GetCurrentStockAsync(
            filterArticle, filterStorageLocationId,
            filterMinQuantity, filterMaxQuantity);

        var vm = new StockOverviewViewModel
        {
            Items = items,
            FilterArticle = filterArticle,
            FilterStorageLocationId = filterStorageLocationId,
            FilterMinQuantity = filterMinQuantity,
            FilterMaxQuantity = filterMaxQuantity,
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync()
        };

        return View(vm);
    }
}
