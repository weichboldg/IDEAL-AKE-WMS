using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequirePickingOrLeitstandAccess]
public class PickingLeitstandController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
    private readonly IProductionOrderAssemblyGroupRepository _assemblyGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IUserRepository _userRepository;

    public PickingLeitstandController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        IProductionOrderAssemblyGroupRepository assemblyGroupRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IUserRepository userRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _assemblyGroupRepository = assemblyGroupRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _userRepository = userRepository;
    }

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

        // Bulk-Lookups fuer pivot-basiertes Mapping (Spec 7.3)
        var orderIds = orders.Select(o => o.Id).ToList();
        var groupPivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
        var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);

        var viewItems = orders.Select(o =>
        {
            var ps = pickingStatuses.GetValueOrDefault(o.Id);
            var grp = groupPivot.GetValueOrDefault(o.Id) ?? new Dictionary<string, bool>();

            var item = new PickingLeitstandItem
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
                PickingStatus = ps?.PickingStatus,
                HasGlass = ps?.HasGlass ?? false,
                HasExternalPurchase = ps?.HasExternalPurchase ?? false,
                HasCoatingParts = ps?.HasCoatingParts ?? false,
                IsCoatingDone = ps?.IsCoatingDone ?? false,
                IsReleasedForPicking = ps?.IsReleasedForPicking ?? false,
                PickingPriority = ps?.PickingPriority,
                ReleasedAt = ps?.ReleasedAt,
                ReleasedBy = ps?.ReleasedBy,
                AssignedPickerId = ps?.AssignedPickerId,
                AssignedPickerName = ps?.AssignedPickerName,
                HasCooling = grp.GetValueOrDefault("VK"),
                HasFan = grp.GetValueOrDefault("VL"),
                HasElectric = grp.GetValueOrDefault("VE"),
                HasDoors = grp.GetValueOrDefault("VT"),
                HasSuperstructure = grp.GetValueOrDefault("VA"),
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, vorkommissionierTage, holidays);
                // Backward compat: when feature is inactive (setting empty), calculate for ALL orders
                // When feature is active, only calculate if HasCoatingParts == true
                if (!coatingFeatureActive || (ps?.HasCoatingParts ?? false))
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

        var vm = new PickingLeitstandViewModel
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> ToggleRelease(int id, int? assignedPickerId, string? returnUrl)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
        if (ps == null)
            return NotFound("PickingStatus-Zeile fehlt.");

        if (!ps.IsReleasedForPicking && string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["WarningMessage"] = $"FA {order.OrderNumber} kann nicht freigegeben werden — keine Artikelnummer vorhanden.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!ps.IsReleasedForPicking && pickerAssignmentEnabled && !assignedPickerId.HasValue)
        {
            TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var newReleased = !ps.IsReleasedForPicking;
        int? newPriority = ps.PickingPriority;
        if (newReleased && !newPriority.HasValue)
        {
            var existing = await _pickingStatusRepository.GetReleasedForPickingAsync();
            var maxPrio = existing
                .Where(o => o.Id != id)
                .Select(o => o.PickingStatus?.PickingPriority ?? 0)
                .DefaultIfEmpty(0)
                .Max();
            newPriority = maxPrio + 1;
        }

        await _pickingStatusRepository.SetReleaseAsync(
            id, newReleased, newPriority, _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        if (newReleased && assignedPickerId.HasValue)
        {
            var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
            await _pickingStatusRepository.SetAssignedPickerAsync(
                id, assignedPickerId, picker?.Name,
                _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        }

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
            var existing = await _pickingStatusRepository.GetReleasedForPickingAsync();
            maxPrio = existing
                .Where(o => o.PickingStatus?.PickingPriority != null)
                .Select(o => o.PickingStatus!.PickingPriority!.Value)
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

            var ps = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
            if (ps == null) continue;

            if (release && string.IsNullOrEmpty(order.ArticleNumber))
            {
                skipped.Add(order.OrderNumber);
                continue;
            }

            int? newPriority = release ? (ps.PickingPriority ?? (++maxPrio)) : ps.PickingPriority;
            await _pickingStatusRepository.SetReleaseAsync(
                id, release, newPriority, displayName, displayName, windowsUser);

            if (release && assignedPickerId.HasValue)
            {
                await _pickingStatusRepository.SetAssignedPickerAsync(
                    id, assignedPickerId, pickerName, displayName, windowsUser);
            }
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

        await _pickingStatusRepository.SetPriorityAsync(
            id, priority,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
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

        await _pickingStatusRepository.SetAssignedPickerAsync(
            id, assignedPickerId, picker.Name,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        return Ok();
    }
}
