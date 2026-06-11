using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using PageSize = IdealAkeWms.Services.PageSize;

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
        bool showDone = false,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        // Datum-Filter werden in C# nach Termin-Berechnung angewendet (Komm., BG, Beschicht.).
        // Wenn aktiv: alle Text-gefilterten Rows materialisieren, Termine berechnen, dann erst paginieren.
        var dateFilters = columnFilters
            .Where(kv => LeitstandDateColumnKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var hasDateFilters = dateFilters.Count > 0;
        var sqlColumnFilters = hasDateFilters
            ? columnFilters.Where(kv => !LeitstandDateColumnKeys.Contains(kv.Key))
                           .ToDictionary(kv => kv.Key, kv => kv.Value)
            : columnFilters;

        var sqlPageSize = hasDateFilters ? int.MaxValue : effectivePageSize;
        var sqlPage = hasDateFilters ? 1 : page;

        var ordersPage = await _productionOrderRepository.GetForLeitstandAsync(
            filterOrderNumber, filterArticleNumber, filterCustomer, showDone, sqlPage, sqlPageSize, sqlColumnFilters);
        var orders = ordersPage.Rows;

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
                IsDone = o.IsDone || o.IsDonePicking,
                WorkplaceName = o.WorkplaceName,
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

        // Wenn Datum-Filter aktiv waren: Termin-basierten Filter anwenden und C#-seitig paginieren.
        int finalTotalCount = ordersPage.TotalCount;
        if (hasDateFilters)
        {
            foreach (var (key, raw) in dateFilters)
            {
                var (tokens, negate) = ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                viewItems = viewItems.Where(it => MatchLeitstandDateFilter(it, key, tokens, negate)).ToList();
            }
            finalTotalCount = viewItems.Count;
            viewItems = viewItems.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        }

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
            EnaioDmsLinks = dmsLinks,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = finalTotalCount
            }
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
            var maxPrio = await _pickingStatusRepository.GetMaxPickingPriorityAsync(excludeProductionOrderId: id);
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

        string? pickerName = null;
        if (release && assignedPickerId.HasValue)
        {
            var picker = await _userRepository.GetByIdAsync(assignedPickerId.Value);
            pickerName = picker?.Name;
        }

        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        var batch = await _pickingStatusRepository.SetReleaseBatchAsync(
            ids, release, assignedPickerId, pickerName, displayName, displayName, windowsUser);

        if (release)
            TempData["SuccessMessage"] = $"{batch.Processed} Auftrag/Aufträge freigegeben.";
        else
            TempData["SuccessMessage"] = $"{batch.Processed} Freigabe(n) zurückgenommen.";

        if (batch.SkippedNoArticle.Count > 0)
            TempData["WarningMessage"] = $"Übersprungen (keine Artikelnummer): {string.Join(", ", batch.SkippedNoArticle)}";

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

    // ---------------------------------------------------------------------
    // Date-Spalten-Filter (server-seitig, nach C#-Termin-Berechnung)
    // ---------------------------------------------------------------------
    private static readonly HashSet<string> LeitstandDateColumnKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "coating-date",   // Beschicht.
        "bg-date",        // BG-Termin (VorkommissionierTermin)
        "picking-date",   // Komm. (KommissionierTermin)
        "production-date",// Fert.-Termin (ProductionDate)
        "delivery-date"   // Liefertermin (DeliveryDate)
    };

    /// <summary>
    /// Formatiert ein Datum identisch zur View (<c>dd.MM.yyyy KWxx</c>) und lowercased,
    /// damit der Server denselben Text matched wie der bisherige clientseitige Filter.
    /// </summary>
    private static string FormatDateForFilter(DateTime? date)
    {
        if (!date.HasValue) return string.Empty;
        var d = date.Value;
        var kw = System.Globalization.ISOWeek.GetWeekOfYear(d);
        return $"{d:dd.MM.yyyy} KW{kw}".ToLowerInvariant();
    }

    private static bool MatchLeitstandDateFilter(
        PickingLeitstandItem item, string key, List<string> tokens, bool negate)
    {
        var text = key switch
        {
            "coating-date" => FormatDateForFilter(item.BeschichtungTermin),
            "bg-date" => FormatDateForFilter(item.VorkommissionierTermin),
            "picking-date" => FormatDateForFilter(item.KommissionierTermin),
            "production-date" => FormatDateForFilter(item.ProductionDate),
            "delivery-date" => FormatDateForFilter(item.DeliveryDate),
            _ => string.Empty
        };
        var hasMatch = tokens.Any(t => text.Contains(t));
        return negate ? !hasMatch : hasMatch;
    }
}
