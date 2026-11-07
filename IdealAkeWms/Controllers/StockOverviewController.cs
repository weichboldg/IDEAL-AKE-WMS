using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[RequireStockReadAccess]
public class StockOverviewController : Controller
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IAppSettingRepository _settingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductionOrderRepository _productionOrderRepository;

    // Mapping data-col-key -> Property fuer Server-Side-Column-Filter
    private static readonly Dictionary<string, Func<StockOverviewItem, string?>> _stockOverviewColMap = new()
    {
        ["article-number"] = i => i.ArticleNumber,
        ["description"] = i => i.ArticleDescription,
        ["storage-location"] = i => i.StorageLocationCode,
        ["unit"] = i => i.Unit,
        ["reorder-level"] = i => i.ReorderLevel?.ToString("N3"), // wie die Zelle, Regel 6
        ["current-stock"] = i => i.CurrentQuantity.ToString("N3") // wie die Zelle, Regel 6
    };

    public StockOverviewController(
        IStockMovementRepository stockMovementRepository,
        IStorageLocationRepository storageLocationRepository,
        IAppSettingRepository settingRepository,
        ICurrentUserService currentUserService,
        IProductionOrderRepository productionOrderRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _stockMovementRepository = stockMovementRepository;
        _storageLocationRepository = storageLocationRepository;
        _settingRepository = settingRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(
        string? filterArticle, int? filterStorageLocationId,
        decimal? filterMinQuantity, decimal? filterMaxQuantity,
        string? filterProductionOrder,
        int page = 1, int? pageSize = null)
    {
        List<StockOverviewItem> items;
        if (!string.IsNullOrWhiteSpace(filterProductionOrder))
        {
            // Dedizierte FA-Abfrage, historischer Pfad (onlyActualStock: false): FA-getaggte
            // Netto-Summe pro Artikel+Lagerplatz — bewusst unverändert („wo wurde je unter dieser
            // FA gebucht"), inkl. bekannter Phantom-Menge bei komplett ausgebuchten FAs.
            items = await _stockMovementRepository.GetStockByProductionOrderAsync(filterProductionOrder, onlyActualStock: false);
        }
        else
        {
            items = await _stockMovementRepository.GetCurrentStockAsync(
                filterArticle, filterStorageLocationId,
                filterMinQuantity, filterMaxQuantity);
        }

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);
        if (page < 1) page = 1;

        // Server-Side Column Filter (in-memory ueber das schon geladene Set)
        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        items = IdealAkeWms.Services.ColumnFilterHelper.Apply(items, columnFilters, _stockOverviewColMap, ColumnFilterTypes.For("StockOverview/Index")).ToList();

        var totalCount = items.Count;
        var pagedItems = items.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

        var vm = new StockOverviewViewModel
        {
            Items = pagedItems,
            FilterArticle = filterArticle,
            FilterStorageLocationId = filterStorageLocationId,
            FilterMinQuantity = filterMinQuantity,
            FilterMaxQuantity = filterMaxQuantity,
            FilterProductionOrder = filterProductionOrder,
            StorageLocations = await _storageLocationRepository.GetAllOrderedAsync(),
            WarningThresholdPercent = await _settingRepository.GetIntValueAsync("WarningThresholdPercent", 150),
            CriticalThresholdPercent = await _settingRepository.GetIntValueAsync("CriticalThresholdPercent", 100),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            }
        };

        // v1.87.0 (Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung, Antwort 2): der Artikel-QR-Knopf uebernimmt NIE einen FA
        // (View: data-qr-fa-enabled="false"); ersetzt die QrMitFaNummer-Bindung aus v1.76.0. FA-Kopf bei gesetztem FA-Filter.
        if (!string.IsNullOrWhiteSpace(filterProductionOrder))
            vm.FaKopf = await _productionOrderRepository.FindForBookingAsync(filterProductionOrder);
        return View(vm);
    }
}
