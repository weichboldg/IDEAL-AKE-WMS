using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using PageSize = IdealAkeWms.Services.PageSize;

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
    private readonly IServiceSettingRepository _serviceSettingRepository;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IServiceSettingRepository serviceSettingRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _serviceSettingRepository = serviceSettingRepository;
    }

    // IDEAL FA-Liste-Hierarchie: Master-Zustand lesen (Muster wie StandortEinstellungenController).
    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase));

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
        bool showDone = false,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        // Datum-Filter werden in C# nach Termin-Berechnung angewendet.
        var dateFilters = columnFilters
            .Where(kv => FaListDateColumnKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var hasDateFilters = dateFilters.Count > 0;
        var sqlColumnFilters = hasDateFilters
            ? columnFilters.Where(kv => !FaListDateColumnKeys.Contains(kv.Key))
                           .ToDictionary(kv => kv.Key, kv => kv.Value)
            : columnFilters;

        var sqlPageSize = hasDateFilters ? int.MaxValue : effectivePageSize;
        var sqlPage = hasDateFilters ? 1 : page;

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
        var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();
        var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
        var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
        ViewBag.LackierteilKategorieName = lackierteilName;

        // Gemeinsame Zeilen-Anreicherung (Termine, PrePicking-Override, Coating) — flach UND gruppiert.
        ProductionOrderListItem MapItem(LeitstandOrderRow o, bool hasCoatingParts, bool isCoatingDone)
        {
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
                IsDone = o.IsDone || o.IsDonePicking,
                IsCancelled = o.IsCancelled,
                WorkplaceName = o.WorkplaceName,
                Kaeltemittel = o.Kaeltemittel,
                Ventil = o.Ventil,
                AusfuehrungEZ = o.AusfuehrungEZ,
                Maschine = o.Maschine,
                SageStatus = o.SageStatus,
                HasCoatingParts = hasCoatingParts,
                IsCoatingDone = isCoatingDone,
                SubOrderNumber = o.SubOrderNumber,
                ParentSubOrderNumber = o.ParentSubOrderNumber,
                SageMissingSince = o.SageMissingSince,
            };

            var effectivePrePickingDays = PrePickingDaysResolver.Resolve(
                o.WorkplaceOverridePrePickingDays, vorkommissionierTage);
            if (PrePickingDaysResolver.IsOverrideActive(o.WorkplaceOverridePrePickingDays))
                item.PrePickingDaysOverride = effectivePrePickingDays;

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, effectivePrePickingDays, holidays);
                if (!coatingFeatureActive || item.HasCoatingParts)
                {
                    var rawBeschichtung = _businessDayService.SubtractBusinessDays(
                        item.VorkommissionierTermin.Value, beschichtungTage, holidays);
                    item.BeschichtungTermin = _businessDayService.FindPreviousPickupDay(rawBeschichtung, pickupDays);
                }
            }
            return item;
        }

        // IDEAL FA-Liste-Hierarchie: nur bei Master ProduktionsauftragHierarchisch = true.
        var hierarchical = IsTrue(await _serviceSettingRepository.GetValueAsync(
            IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master));

        ProductionOrderListViewModel vm;

        if (hierarchical)
        {
            // Paginierung ueber GRUPPEN; bei Datumsfiltern alle Gruppen laden, in C# filtern/paginieren.
            var sqlGroupPageSize = hasDateFilters ? int.MaxValue : effectivePageSize;
            var sqlGroupPage = hasDateFilters ? 1 : page;
            var grouped = await _productionOrderRepository.GetForLeitstandGroupedAsync(
                filterOrderNumber, filterArticleNumber, filterCustomer, showDone, sqlGroupPage, sqlGroupPageSize, sqlColumnFilters);

            var allRows = grouped.Groups.SelectMany(g => g.Rows).ToList();
            var psDict = await _pickingStatusRepository.GetByProductionOrderIdsAsync(allRows.Select(r => r.Id).ToList());

            var listGroups = grouped.Groups.Select(g => new ProductionOrderListGroup
            {
                OrderNumber = g.OrderNumber,
                Items = g.Rows.Select(r =>
                {
                    var ps = psDict.GetValueOrDefault(r.Id);
                    return MapItem(r, ps?.HasCoatingParts ?? false, ps?.IsCoatingDone ?? false);
                }).ToList()
            }).ToList();

            var totalGroupCount = grouped.TotalGroupCount;
            var totalRowCount = grouped.TotalRowCount;

            if (hasDateFilters)
            {
                foreach (var (key, raw) in dateFilters)
                {
                    var (tokens, negate) = ColumnFilterHelper.Parse(raw);
                    if (tokens.Count == 0) continue;
                    foreach (var g in listGroups)
                        g.Items = g.Items.Where(it => MatchFaListDateFilter(it, key, tokens, negate)).ToList();
                }
                listGroups = listGroups.Where(g => g.Items.Count > 0).ToList(); // leere Gruppen entfernen (Z3b)
                totalGroupCount = listGroups.Count;
                totalRowCount = listGroups.Sum(g => g.Items.Count);
                listGroups = listGroups.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
            }

            var groupDmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(
                listGroups.Select(g => g.OrderNumber).Distinct().ToList());

            vm = new ProductionOrderListViewModel
            {
                Hierarchical = true,
                Groups = listGroups,
                HierarchicalRowCount = totalRowCount,
                FilterOrderNumber = filterOrderNumber,
                FilterArticleNumber = filterArticleNumber,
                FilterCustomer = filterCustomer,
                ShowDone = showDone,
                KommissionierTage = kommissionierTage,
                VorkommissionierTage = vorkommissionierTage,
                BeschichtungTage = beschichtungTage,
                CanPick = await _currentUserService.CanPickAsync(),
                HasVorbauAccess = await _currentUserService.HasVorbauAccessAsync(),
                EnaioDmsLinks = groupDmsLinks,
                Pagination = new PaginationState
                {
                    CurrentPage = page,
                    PageSize = effectivePageSize,
                    PageSizeRaw = rawPageSize,
                    TotalCount = totalGroupCount // Paginierung ueber Gruppen (F3)
                }
            };
            return View(vm);
        }

        // --- Flachmodus (AKE / Master false): unveraendert zu heute ---
        var ordersPage = await _productionOrderRepository.GetForLeitstandAsync(
            filterOrderNumber, filterArticleNumber, filterCustomer, showDone, sqlPage, sqlPageSize, sqlColumnFilters);
        var orders = ordersPage.Rows;

        var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(
            orders.Select(o => o.Id).ToList());

        var viewItems = orders.Select(o =>
        {
            var ps = pickingStatuses.GetValueOrDefault(o.Id);
            return MapItem(o, ps?.HasCoatingParts ?? false, ps?.IsCoatingDone ?? false);
        }).ToList();

        int finalTotalCount = ordersPage.TotalCount;
        if (hasDateFilters)
        {
            foreach (var (key, raw) in dateFilters)
            {
                var (tokens, negate) = ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                viewItems = viewItems.Where(it => MatchFaListDateFilter(it, key, tokens, negate)).ToList();
            }
            finalTotalCount = viewItems.Count;
            viewItems = viewItems.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        }

        var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(
            viewItems.Select(i => i.OrderNumber).Distinct().ToList());

        vm = new ProductionOrderListViewModel
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
            HasVorbauAccess = await _currentUserService.HasVorbauAccessAsync(),
            EnaioDmsLinks = dmsLinks,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = finalTotalCount
            }
        };

        return View(vm);
    }

    // ---------------------------------------------------------------------
    // Date-Spalten-Filter (server-seitig, nach C#-Termin-Berechnung)
    // ---------------------------------------------------------------------
    private static readonly HashSet<string> FaListDateColumnKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "coating-date", "bg-date", "picking-date", "production-date", "delivery-date"
    };

    private static string FormatDateForFilter(DateTime? date)
    {
        if (!date.HasValue) return string.Empty;
        var d = date.Value;
        var kw = System.Globalization.ISOWeek.GetWeekOfYear(d);
        return $"{d:dd.MM.yyyy} KW{kw}".ToLowerInvariant();
    }

    private static bool MatchFaListDateFilter(
        ProductionOrderListItem item, string key, List<string> tokens, bool negate)
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

    // ToggleCoatingDone wurde ersetzt durch /api/productionorders/toggle-field
    // (siehe ProductionOrdersApiController) — gleicher Mechanismus wie HasGlass/HasExternalPurchase

    // Redirect-Stubs für verschobene Actions (Abwärtskompatibilität)
    public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
    public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
}
