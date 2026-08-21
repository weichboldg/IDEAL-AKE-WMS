using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Oseon;

namespace IdealAkeWms.Controllers;

[RequireTrackingAccess]
public class TrackingController : Controller
{
    private readonly IWorkOperationRepository _workOperationRepository;
    private readonly IProductionWorkplaceRepository _workplaceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOseonProductionOrderRepository _oseonRepository;
    private readonly IOseonTrafficLightService _trafficLightService;
    private readonly IOseonOperationConfigRepository _operationConfigRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IOseonGroupViewModelBuilder _groupBuilder;
    private readonly IServiceSettingRepository _serviceSettingRepository;

    public TrackingController(
        IWorkOperationRepository workOperationRepository,
        IProductionWorkplaceRepository workplaceRepository,
        ICurrentUserService currentUserService,
        IOseonProductionOrderRepository oseonRepository,
        IOseonTrafficLightService trafficLightService,
        IOseonOperationConfigRepository operationConfigRepository,
        IBusinessDayService businessDayService,
        IHolidayRepository holidayRepository,
        IOseonGroupViewModelBuilder groupBuilder,
        IServiceSettingRepository serviceSettingRepository)
    {
        _workOperationRepository = workOperationRepository;
        _workplaceRepository = workplaceRepository;
        _currentUserService = currentUserService;
        _oseonRepository = oseonRepository;
        _trafficLightService = trafficLightService;
        _operationConfigRepository = operationConfigRepository;
        _businessDayService = businessDayService;
        _holidayRepository = holidayRepository;
        _groupBuilder = groupBuilder;
        _serviceSettingRepository = serviceSettingRepository;
    }

    // IDEAL FA-Liste-Hierarchie: Master-Zustand lesen (Muster wie ProductionOrdersController).
    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase));

    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        int? filterWorkplaceId,
        bool showReported = false,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // IDEAL FA-Liste-Hierarchie: nur bei Master ProduktionsauftragHierarchisch = true.
        var hierarchical = IsTrue(await _serviceSettingRepository.GetValueAsync(
            IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master));

        var allOperations = await _workOperationRepository.GetAllWithOrderAndWorkplaceAsync();

        // Filter anwenden. FA-Nummer im hierarchischen Modus Sub-zuerst-dann-Haupt.
        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            allOperations = allOperations
                .Where(wo => wo.ProductionOrder.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)
                             || (hierarchical && wo.ProductionOrder.SubOrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)))
                .ToList();

        if (filterWorkplaceId.HasValue)
            allOperations = allOperations
                .Where(wo => wo.ProductionWorkplaceId == filterWorkplaceId.Value)
                .ToList();

        if (!showReported)
            allOperations = allOperations
                .Where(wo => !wo.IsReported)
                .ToList();

        // Nach Auftrag (= ProductionOrder, im hierarchischen Modus ein Sub-FA) gruppieren.
        var allGroups = allOperations
            .GroupBy(wo => wo.ProductionOrderId)
            .Select(g =>
            {
                var po = g.First().ProductionOrder;
                var allOpsForOrder = g.ToList();
                return new TrackingOrderGroup
                {
                    ProductionOrderId = po.Id,
                    OrderNumber = po.OrderNumber,
                    ArticleNumber = po.ArticleNumber,
                    Description1 = po.Description1,
                    Customer = po.Customer,
                    ProductionDate = po.ProductionDate,
                    WorkplaceName = po.ProductionWorkplace?.Name,
                    TotalOperations = allOpsForOrder.Count,
                    ReportedOperations = allOpsForOrder.Count(o => o.IsReported),
                    Operations = allOpsForOrder.Select(MapToItem).ToList(),
                    SubOrderNumber = po.SubOrderNumber,
                    ParentSubOrderNumber = po.ParentSubOrderNumber,
                    SageMissingSince = po.SageMissingSince,
                };
            })
            .OrderBy(g => g.OrderNumber)
            .ToList();

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var colMap = hierarchical ? TrackingGroupColumnMapHierarchical : TrackingGroupColumnMap;
        var filteredGroups = IdealAkeWms.Services.ColumnFilterHelper
            .Apply(allGroups, columnFilters, colMap)
            .ToList();

        var workplaces = await _workplaceRepository.GetAllOrderedAsync();
        var canReport = await _currentUserService.CanReportOperationsAsync();

        if (hierarchical)
        {
            // Dritte Ebene: die Sub-FA-Gruppen zu HauptFA-Uebergruppen buendeln (OrderNumber),
            // Z2 innerhalb der Gruppe nach SubOrderNumber. Paginierung ueber HauptFA-Gruppen (F3);
            // Z3b: leere HauptFA-Gruppen entstehen nicht, da nach Zeilen/Sub-FAs gefiltert wird.
            var hauptGroups = filteredGroups
                .GroupBy(g => g.OrderNumber)
                .Select(hg => new TrackingHauptFaGroup
                {
                    OrderNumber = hg.Key,
                    SubGroups = hg.OrderBy(s => s.SubOrderNumber, StringComparer.OrdinalIgnoreCase).ToList(),
                })
                .ToList();

            var totalHauptCount = hauptGroups.Count;
            var totalSubCount = filteredGroups.Count;
            var pagedHaupt = hauptGroups
                .Skip((page - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();

            var hvm = new TrackingViewModel
            {
                Hierarchical = true,
                HauptGroups = pagedHaupt,
                HierarchicalRowCount = totalSubCount,
                FilterOrderNumber = filterOrderNumber,
                FilterWorkplaceId = filterWorkplaceId,
                ShowReported = showReported,
                AvailableWorkplaces = workplaces,
                CanReport = canReport,
                Pagination = new PaginationState
                {
                    CurrentPage = page,
                    PageSize = effectivePageSize,
                    PageSizeRaw = rawPageSize,
                    TotalCount = totalHauptCount, // Paginierung ueber HauptFA-Gruppen
                },
            };

            return View(hvm);
        }

        // --- Flachmodus (AKE / Master false): unveraendert zu heute (2-Ebenen, Bootstrap-collapse) ---
        var totalCount = filteredGroups.Count;
        var pagedGroups = filteredGroups
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToList();

        var vm = new TrackingViewModel
        {
            OrderGroups = pagedGroups,
            FilterOrderNumber = filterOrderNumber,
            FilterWorkplaceId = filterWorkplaceId,
            ShowReported = showReported,
            AvailableWorkplaces = workplaces,
            CanReport = canReport,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount,
            },
        };

        return View(vm);
    }

    private static readonly Dictionary<string, Func<TrackingOrderGroup, string?>> TrackingGroupColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["order-number"] = g => g.OrderNumber,
            ["article-number"] = g => g.ArticleNumber,
            ["description1"] = g => g.Description1,
            ["customer"] = g => g.Customer,
            ["production-date"] = g => g.ProductionDate.HasValue
                ? $"{g.ProductionDate.Value:dd.MM.yyyy} KW{System.Globalization.ISOWeek.GetWeekOfYear(g.ProductionDate.Value)}"
                : string.Empty,
            ["workplace"] = g => g.WorkplaceName,
            ["progress"] = g => $"{g.ReportedOperations}/{g.TotalOperations}",
        };

    // Hierarchischer Modus: FA-Nummer-Spaltenfilter matcht Sub-zuerst-dann-Haupt (OR ueber Contains).
    private static readonly Dictionary<string, Func<TrackingOrderGroup, string?>> TrackingGroupColumnMapHierarchical =
        new(TrackingGroupColumnMap, StringComparer.OrdinalIgnoreCase)
        {
            ["order-number"] = g => $"{g.SubOrderNumber} {g.OrderNumber}",
        };

    public async Task<IActionResult> ByWorkplace(int id, string? filterOrderNumber, bool showReported = false)
    {
        var workplace = await _workplaceRepository.GetByIdAsync(id);
        if (workplace == null)
            return NotFound();

        var operations = showReported
            ? await _workOperationRepository.GetByWorkplaceIdAsync(id)
            : await _workOperationRepository.GetOpenByWorkplaceIdAsync(id);

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            operations = operations
                .Where(wo => wo.ProductionOrder.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var vm = new TrackingByWorkplaceViewModel
        {
            Workplace = workplace,
            Operations = operations.Select(MapToItem).ToList(),
            CanReport = await _currentUserService.CanReportOperationsAsync(),
            FilterOrderNumber = filterOrderNumber,
            ShowReported = showReported
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, string? returnUrl)
    {
        if (!await _currentUserService.CanReportOperationsAsync())
            return RedirectToAction("AccessDenied", "Account");

        var operation = await _workOperationRepository.GetByIdAsync(id);
        if (operation == null)
            return NotFound();

        operation.IsReported = true;
        operation.ReportedAt = DateTime.UtcNow;
        operation.ReportedBy = _currentUserService.GetDisplayName();
        operation.ReportedByWindows = _currentUserService.GetWindowsUserName();
        operation.ModifiedAt = DateTime.UtcNow;
        operation.ModifiedBy = _currentUserService.GetDisplayName();
        operation.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _workOperationRepository.UpdateAsync(operation);

        TempData["SuccessMessage"] = $"Arbeitsgang '{operation.Name}' wurde rückgemeldet.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoReport(int id, string? returnUrl)
    {
        if (!await _currentUserService.CanReportOperationsAsync())
            return RedirectToAction("AccessDenied", "Account");

        var operation = await _workOperationRepository.GetByIdAsync(id);
        if (operation == null)
            return NotFound();

        operation.IsReported = false;
        operation.ReportedAt = null;
        operation.ReportedBy = null;
        operation.ReportedByWindows = null;
        operation.ModifiedAt = DateTime.UtcNow;
        operation.ModifiedBy = _currentUserService.GetDisplayName();
        operation.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _workOperationRepository.UpdateAsync(operation);

        TempData["SuccessMessage"] = $"Rückmeldung für '{operation.Name}' wurde zurückgenommen.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> OseonIndex(
        string? filterCustomerOrder, string? filterArticle, string? filterWorkplace,
        bool showFinished = false, bool useRelevanceFilter = true,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // AG-Configs und Feiertage einmalig laden
        var opConfigs = await _operationConfigRepository.GetAllAsDictionaryAsync();

        // Relevante AG-Namen fuer DB-Filter ermitteln
        HashSet<string>? relevantOpNames = null;
        if (useRelevanceFilter)
        {
            relevantOpNames = opConfigs
                .Where(kvp => kvp.Value.IsOseonRelevant)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        // Server-seitig gefiltert + paginiert laden
        var pagedResult = await _oseonRepository.GetPagedAsync(filterCustomerOrder, filterWorkplace, showFinished, page, effectivePageSize, relevantOpNames, filterArticle, columnFilters);

        // Ampelfarben + AG-Termine berechnen
        var groups = new List<OseonOrderGroupViewModel>();
        foreach (var g in pagedResult.Items.GroupBy(o => o.CustomerOrderNumber ?? o.OseonOrderNumber))
        {
            var group = await _groupBuilder.BuildAsync(g.Key, g, useRelevanceFilter, filterArticle, HttpContext.RequestAborted);
            group.IsPrefetched = !string.IsNullOrWhiteSpace(filterArticle);
            groups.Add(group);
        }

        groups = groups
            .OrderByDescending(g => g.WorstColor)
            .ThenBy(g => g.CustomerOrderNumber)
            .ToList();

        // Eindeutige Werkbanknamen fuer Filter-Dropdown
        var workplaces = await _workplaceRepository.GetAllOrderedAsync();

        var vm = new OseonTrackingViewModel
        {
            OrderGroups = groups,
            FilterCustomerOrder = filterCustomerOrder,
            FilterArticle = filterArticle,
            FilterWorkplace = filterWorkplace,
            ShowFinished = showFinished,
            UseRelevanceFilter = useRelevanceFilter,
            AvailableWorkplaces = workplaces,
            CurrentPage = pagedResult.Page,
            TotalPages = pagedResult.TotalPages,
            TotalGroupCount = pagedResult.TotalGroupCount,
            Pagination = new PaginationState
            {
                CurrentPage = pagedResult.Page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = pagedResult.TotalGroupCount
            }
        };

        return View(vm);
    }

    public async Task<IActionResult> OseonGroupDetails(
        string customerOrderNumber,
        bool useRelevanceFilter = true,
        bool showFinished = false,
        string? filterArticle = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerOrderNumber))
        {
            return BadRequest("customerOrderNumber is required.");
        }

        var opConfigs = await _operationConfigRepository.GetAllAsDictionaryAsync();
        HashSet<string>? relevantOpNames = null;
        if (useRelevanceFilter)
        {
            relevantOpNames = opConfigs
                .Where(kvp => kvp.Value.IsOseonRelevant)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }

        var subOrders = await _oseonRepository.GetSubOrdersForCustomerOrderAsync(
            customerOrderNumber, showFinished, relevantOpNames, ct);

        if (subOrders.Count == 0)
        {
            return NotFound();
        }

        var group = await _groupBuilder.BuildAsync(
            customerOrderNumber, subOrders, useRelevanceFilter, filterArticle, ct);

        return PartialView("_OseonGroupDetails", group);
    }

    private static TrackingOperationItem MapToItem(Models.WorkOperation wo)
    {
        return new TrackingOperationItem
        {
            Id = wo.Id,
            OperationNumber = wo.OperationNumber,
            Name = wo.Name,
            Sequence = wo.Sequence,
            WorkplaceName = wo.ProductionWorkplace?.Name,
            OrderNumber = wo.ProductionOrder?.OrderNumber,
            IsReportable = wo.IsReportable,
            IsExternalSystem = wo.IsExternalSystem,
            IsReported = wo.IsReported,
            ReportedAt = wo.ReportedAt,
            ReportedBy = wo.ReportedBy,
            ExternalSource = wo.ExternalSource
        };
    }
}
