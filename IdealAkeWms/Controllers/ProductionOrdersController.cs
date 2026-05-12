using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[RequirePickingOrTrackingOrLeitstandAccess]
public class ProductionOrdersController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
    }

    // Backward-Compat-Redirects fuer Stale-Tab-Posts auf alte URLs.
    // Die eigentliche Logik wurde nach PickingLeitstandController verschoben (Phase 2 Task 2).
    // TODO: nach v1.14.0 entfernen.
    [HttpPost]
    public IActionResult ToggleRelease() => RedirectToActionPermanent("Index", "PickingLeitstand");

    [HttpPost]
    public IActionResult BulkRelease() => RedirectToActionPermanent("Index", "PickingLeitstand");

    [HttpPost]
    public IActionResult SetPriority() => RedirectToActionPermanent("Index", "PickingLeitstand");

    [HttpPost]
    public IActionResult ChangeAssignedPicker() => RedirectToActionPermanent("Index", "PickingLeitstand");

    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false)
    {
        var orders = await _productionOrderRepository.GetAllOrderedAsync();

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
        {
            orders = orders.Where(o => o.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
        {
            orders = orders.Where(o => o.ArticleNumber != null && o.ArticleNumber.Contains(filterArticleNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterCustomer))
        {
            orders = orders.Where(o => o.Customer != null && o.Customer.Contains(filterCustomer, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!showDone)
        {
            orders = orders.Where(o => !o.IsDone).ToList();
        }

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
        var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();
        var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
        var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
        ViewBag.LackierteilKategorieName = lackierteilName;

        // PickingStatus-Dict nur fuer HasCoatingParts/IsCoatingDone (Beschichtungstermin-Logik, Spec 6.1)
        var orderIds = orders.Select(o => o.Id).ToList();
        var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);

        var viewItems = orders.Select(o =>
        {
            var ps = pickingStatuses.GetValueOrDefault(o.Id);

            var item = new ProductionOrderListItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Quantity = o.Quantity,
                Customer = o.Customer,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Description2 = o.Description2,
                ProductionDate = o.ProductionDate,
                DeliveryDate = o.DeliveryDate,
                IsDone = o.IsDone,
                WorkplaceName = o.ProductionWorkplace?.Name,
                HasCoatingParts = ps?.HasCoatingParts ?? false,
                IsCoatingDone = ps?.IsCoatingDone ?? false,
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, vorkommissionierTage, holidays);
                // Backward compat: when feature is inactive (setting empty), calculate for ALL orders
                // When feature is active, only calculate if HasCoatingParts == true
                if (!coatingFeatureActive || item.HasCoatingParts)
                {
                    var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                        item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                    item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
                }
            }

            return item;
        }).ToList();

        // enaio DMS-Links laden (Bulk-Lookup fuer alle FA-Nummern)
        var orderNumbers = viewItems.Select(i => i.OrderNumber).Distinct().ToList();
        var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

        var vm = new ProductionOrderListViewModel
        {
            Items = viewItems,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
            KommissionierTage = kommissionierTage,
            VorkommissionierTage = vorkommissionierTage,
            BeschichtungTage = beschichtungTage,
            CanPick = await _currentUserService.CanPickAsync(),
            EnaioDmsLinks = dmsLinks
        };

        return View(vm);
    }

    // ToggleCoatingDone wurde ersetzt durch /api/productionorders/toggle-field
    // (siehe ProductionOrdersApiController) — gleicher Mechanismus wie HasGlass/HasExternalPurchase

    // Redirect-Stubs für verschobene Actions (Abwärtskompatibilität)
    public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
    public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
}
