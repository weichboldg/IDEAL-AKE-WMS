using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

public class ProductionOrdersController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IUserRepository _userRepository;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IUserRepository userRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _userRepository = userRepository;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> ToggleRelease(int id, int? assignedPickerId, string? returnUrl)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (!order.IsReleasedForPicking && string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["WarningMessage"] = $"FA {order.OrderNumber} kann nicht freigegeben werden — keine Artikelnummer vorhanden.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!order.IsReleasedForPicking && pickerAssignmentEnabled && !assignedPickerId.HasValue)
        {
            TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        order.IsReleasedForPicking = !order.IsReleasedForPicking;
        if (order.IsReleasedForPicking)
        {
            order.ReleasedAt = DateTime.UtcNow;
            order.ReleasedBy = _currentUserService.GetDisplayName();

            if (!order.PickingPriority.HasValue)
            {
                var maxPrio = (await _productionOrderRepository.GetReleasedForPickingAsync())
                    .Where(o => o.PickingPriority.HasValue && o.Id != order.Id)
                    .Select(o => o.PickingPriority!.Value)
                    .DefaultIfEmpty(0)
                    .Max();
                order.PickingPriority = maxPrio + 1;
            }

            if (assignedPickerId.HasValue)
            {
                var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
                order.AssignedPickerId = assignedPickerId;
                order.AssignedPickerName = picker?.Name;
            }
        }
        else
        {
            order.AssignedPickerId = null;
            order.AssignedPickerName = null;
        }

        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
        await _productionOrderRepository.UpdateAsync(order);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> BulkRelease(List<int> ids, bool release, int? assignedPickerId, string? returnUrl)
    {
        if (ids == null || ids.Count == 0)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (release && pickerAssignmentEnabled && !assignedPickerId.HasValue)
        {
            TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var maxPrio = 0;
        if (release)
        {
            var existing = await _productionOrderRepository.GetReleasedForPickingAsync();
            maxPrio = existing
                .Where(o => o.PickingPriority.HasValue)
                .Select(o => o.PickingPriority!.Value)
                .DefaultIfEmpty(0)
                .Max();
        }

        string? pickerName = null;
        if (release && assignedPickerId.HasValue)
        {
            var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
            pickerName = picker?.Name;
        }

        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();
        var skipped = new List<string>();
        var processed = 0;

        foreach (var id in ids)
        {
            var order = await _productionOrderRepository.GetByIdAsync(id);
            if (order == null) continue;

            if (release && string.IsNullOrEmpty(order.ArticleNumber))
            {
                skipped.Add(order.OrderNumber);
                continue;
            }

            order.IsReleasedForPicking = release;
            if (release)
            {
                order.ReleasedAt = DateTime.UtcNow;
                order.ReleasedBy = displayName;
                if (!order.PickingPriority.HasValue)
                    order.PickingPriority = ++maxPrio;
                order.AssignedPickerId = assignedPickerId;
                order.AssignedPickerName = pickerName;
            }
            else
            {
                order.AssignedPickerId = null;
                order.AssignedPickerName = null;
            }

            order.ModifiedAt = DateTime.UtcNow;
            order.ModifiedBy = displayName;
            order.ModifiedByWindows = windowsUser;
            await _productionOrderRepository.UpdateAsync(order);
            processed++;
        }

        var count = processed;
        if (release)
            TempData["SuccessMessage"] = $"{count} Auftrag/Aufträge freigegeben.";
        else
            TempData["SuccessMessage"] = $"{count} Freigabe(n) zurückgenommen.";

        if (skipped.Count > 0)
            TempData["WarningMessage"] = $"Übersprungen (keine Artikelnummer): {string.Join(", ", skipped)}";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> SetPriority(int id, int? priority)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        order.PickingPriority = priority;
        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
        await _productionOrderRepository.UpdateAsync(order);

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> ChangeAssignedPicker(int id, int assignedPickerId)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        var picker = await _userRepository.GetByIdAsync(assignedPickerId);
        if (picker == null)
            return BadRequest("Kommissionierer nicht gefunden.");

        order.AssignedPickerId = assignedPickerId;
        order.AssignedPickerName = picker.Name;
        order.ModifiedAt = DateTime.UtcNow;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
        await _productionOrderRepository.UpdateAsync(order);

        return Ok();
    }

    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false)
    {
        // Zugriff: Picking, Tracking oder Leitstand
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanViewTrackingAsync()
            && !await _currentUserService.CanManagePickingReleaseAsync())
        {
            return RedirectToAction("AccessDenied", "Account");
        }

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

        var viewItems = orders.Select(o =>
        {
            var item = new ProductionOrderViewItem
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
                PickingStatus = o.PickingStatus,
                HasGlass = o.HasGlass,
                HasExternalPurchase = o.HasExternalPurchase,
                HasCooling = o.HasCooling,
                HasFan = o.HasFan,
                HasElectric = o.HasElectric,
                HasDoors = o.HasDoors,
                HasSuperstructure = o.HasSuperstructure,
                HasCoatingParts = o.HasCoatingParts,
                IsCoatingDone = o.IsCoatingDone,
                WorkplaceName = o.ProductionWorkplace?.Name,
                IsReleasedForPicking = o.IsReleasedForPicking,
                PickingPriority = o.PickingPriority,
                ReleasedAt = o.ReleasedAt,
                ReleasedBy = o.ReleasedBy,
                AssignedPickerId = o.AssignedPickerId,
                AssignedPickerName = o.AssignedPickerName
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, vorkommissionierTage, holidays);
                // Backward compat: when feature is inactive (setting empty), calculate for ALL orders
                // When feature is active, only calculate if HasCoatingParts == true
                if (!coatingFeatureActive || o.HasCoatingParts)
                {
                    // Beschichtungstermin: Baugruppentermin - BeschichtungTage, dann auf vorherigen Abholtag
                    var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                        item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                    item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
                }
                // else: leave BeschichtungTermin null
            }

            return item;
        }).ToList();

        // enaio DMS-Links laden (Bulk-Lookup fuer alle FA-Nummern)
        var orderNumbers = viewItems.Select(i => i.OrderNumber).Distinct().ToList();
        var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var pickerAssignmentEnabled = leitstandAktiv && (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        var vm = new ProductionOrderViewModel
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
            CanManagePickingRelease = await _currentUserService.CanManagePickingReleaseAsync(),
            LeitstandAktiv = leitstandAktiv,
            PickerAssignmentEnabled = pickerAssignmentEnabled,
            EnaioDmsLinks = dmsLinks
        };

        if (pickerAssignmentEnabled)
        {
            ViewBag.ActivePickers = await _userRepository.GetActivePickersAsync();
        }

        return View(vm);
    }

    // ToggleCoatingDone wurde ersetzt durch /api/productionorders/toggle-field
    // (siehe ProductionOrdersApiController) — gleicher Mechanismus wie HasGlass/HasExternalPurchase

    // Redirect-Stubs für verschobene Actions (Abwärtskompatibilität)
    public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
    public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
}
