using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

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

    public TrackingController(
        IWorkOperationRepository workOperationRepository,
        IProductionWorkplaceRepository workplaceRepository,
        ICurrentUserService currentUserService,
        IOseonProductionOrderRepository oseonRepository,
        IOseonTrafficLightService trafficLightService,
        IOseonOperationConfigRepository operationConfigRepository,
        IBusinessDayService businessDayService,
        IHolidayRepository holidayRepository)
    {
        _workOperationRepository = workOperationRepository;
        _workplaceRepository = workplaceRepository;
        _currentUserService = currentUserService;
        _oseonRepository = oseonRepository;
        _trafficLightService = trafficLightService;
        _operationConfigRepository = operationConfigRepository;
        _businessDayService = businessDayService;
        _holidayRepository = holidayRepository;
    }

    public async Task<IActionResult> Index(string? filterOrderNumber, int? filterWorkplaceId, bool showReported = false)
    {
        var allOperations = await _workOperationRepository.GetAllWithOrderAndWorkplaceAsync();

        // Filter anwenden
        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            allOperations = allOperations
                .Where(wo => wo.ProductionOrder.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (filterWorkplaceId.HasValue)
            allOperations = allOperations
                .Where(wo => wo.ProductionWorkplaceId == filterWorkplaceId.Value)
                .ToList();

        if (!showReported)
            allOperations = allOperations
                .Where(wo => !wo.IsReported)
                .ToList();

        // Nach Auftrag gruppieren
        var groups = allOperations
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
                    Operations = allOpsForOrder.Select(MapToItem).ToList()
                };
            })
            .OrderBy(g => g.OrderNumber)
            .ToList();

        var vm = new TrackingViewModel
        {
            OrderGroups = groups,
            FilterOrderNumber = filterOrderNumber,
            FilterWorkplaceId = filterWorkplaceId,
            ShowReported = showReported,
            AvailableWorkplaces = await _workplaceRepository.GetAllOrderedAsync(),
            CanReport = await _currentUserService.CanReportOperationsAsync()
        };

        return View(vm);
    }

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

    public async Task<IActionResult> OseonIndex(string? filterCustomerOrder, string? filterWorkplace, bool showFinished = false, bool useRelevanceFilter = true, int page = 1)
    {
        const int pageSize = 25;
        if (page < 1) page = 1;

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

        // Server-seitig gefiltert + paginiert laden
        var pagedResult = await _oseonRepository.GetPagedAsync(filterCustomerOrder, filterWorkplace, showFinished, page, pageSize, relevantOpNames);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        // Ampelfarben + AG-Termine berechnen
        var groups = new List<OseonOrderGroupViewModel>();
        foreach (var g in pagedResult.Items.GroupBy(o => o.CustomerOrderNumber ?? o.OseonOrderNumber))
        {
            var subOrders = new List<OseonSubOrderViewModel>();
            foreach (var o in g.OrderBy(o => o.OseonOrderNumber))
            {
                var operations = new List<OseonOperationViewModel>();
                foreach (var op in o.WorkOperations.OrderBy(op => op.PositionNumber))
                {
                    var hasConfig = opConfigs.TryGetValue(op.Name, out var opConfig);
                    var isRelevant = !hasConfig || opConfig!.IsOseonRelevant;

                    // AG-spezifischen Soll-Termin berechnen
                    DateTime? calculatedDueDate = null;
                    if (o.DueDate.HasValue && hasConfig)
                    {
                        calculatedDueDate = opConfig!.DueDateOffsetDays == 0
                            ? o.DueDate.Value.Date
                            : _businessDayService.AddBusinessDays(o.DueDate.Value, opConfig.DueDateOffsetDays, holidays);
                    }
                    else if (o.DueDate.HasValue)
                    {
                        calculatedDueDate = o.DueDate.Value.Date;
                    }

                    var opColor = await _trafficLightService.GetColorForOperationAsync(op.OseonStatus, calculatedDueDate);

                    operations.Add(new OseonOperationViewModel
                    {
                        PositionNumber = op.PositionNumber,
                        Name = op.Name,
                        Description = op.Description,
                        OseonStatus = op.OseonStatus,
                        StatusText = OseonStatusHelper.GetStatusText(op.OseonStatus),
                        StatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(op.OseonStatus),
                        IsFirstOperation = op.IsFirstOperation,
                        IsLastOperation = op.IsLastOperation,
                        Color = opColor,
                        CalculatedDueDate = calculatedDueDate,
                        IsOseonRelevant = isRelevant
                    });
                }

                // Relevanz-Logik: nur wenn Filter aktiv
                int effectiveStatus;
                TrafficLightColor orderColor;
                if (useRelevanceFilter)
                {
                    var relevantOps = operations.Where(op => op.IsOseonRelevant).ToList();
                    // Keine relevanten AGs = Auftrag ist fertig (z.B. nur ZB + A-BT)
                    var noRelevantOps = relevantOps.Count == 0 && operations.Count > 0;
                    var allRelevantFinished = noRelevantOps || (relevantOps.Count > 0 && relevantOps.All(op => op.OseonStatus is 90 or 95));

                    orderColor = allRelevantFinished
                        ? TrafficLightColor.Green
                        : (relevantOps.Count > 0 ? relevantOps.Max(op => op.Color) : TrafficLightColor.Gray);
                    effectiveStatus = allRelevantFinished ? 90 : o.OseonStatus;
                }
                else
                {
                    // Ohne Relevanz-Filter: Original-Logik (alle AGs zaehlen)
                    orderColor = await _trafficLightService.GetColorAsync(o.OseonStatus, o.DueDate);
                    effectiveStatus = o.OseonStatus;
                }

                // Auftrags-Endtermin = Max der berechneten AG-Soll-Termine (wenn vorhanden)
                var maxCalculatedDueDate = operations
                    .Where(op => op.CalculatedDueDate.HasValue)
                    .Select(op => op.CalculatedDueDate!.Value)
                    .DefaultIfEmpty()
                    .Max();
                var displayDueDate = maxCalculatedDueDate != default ? maxCalculatedDueDate : o.DueDate;

                subOrders.Add(new OseonSubOrderViewModel
                {
                    Id = o.Id,
                    OseonOrderNumber = o.OseonOrderNumber,
                    ArticleNumber = o.ArticleNumber,
                    Description1 = o.Description1,
                    Description2 = o.Description2,
                    WorkplaceName = o.WorkplaceName,
                    OseonStatus = effectiveStatus,
                    StatusText = OseonStatusHelper.GetStatusText(effectiveStatus),
                    StatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(effectiveStatus),
                    QuantityTarget = o.QuantityTarget,
                    QuantityActual = o.QuantityActual,
                    DueDate = displayDueDate,
                    Color = orderColor,
                    Operations = operations
                });
            }

            // Worst color: Red > Yellow > Blue > Gray > Green
            var worstColor = subOrders.Count > 0
                ? subOrders.Max(s => s.Color)
                : TrafficLightColor.Gray;

            // Aggregierter Status: der "schlechteste" (= aktivste/dringendste) Status der Gruppe
            var worstStatus = subOrders.Count > 0
                ? GetWorstStatus(subOrders.Select(s => s.OseonStatus))
                : 0;

            groups.Add(new OseonOrderGroupViewModel
            {
                CustomerOrderNumber = g.Key,
                WorstColor = worstColor,
                TotalSubOrders = subOrders.Count,
                FinishedSubOrders = subOrders.Count(s => s.OseonStatus is 90 or 95),
                GroupStatusText = OseonStatusHelper.GetStatusText(worstStatus),
                GroupStatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(worstStatus),
                SubOrders = subOrders
            });
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
            FilterWorkplace = filterWorkplace,
            ShowFinished = showFinished,
            UseRelevanceFilter = useRelevanceFilter,
            AvailableWorkplaces = workplaces,
            CurrentPage = pagedResult.Page,
            TotalPages = pagedResult.TotalPages,
            TotalGroupCount = pagedResult.TotalGroupCount
        };

        return View(vm);
    }

    /// <summary>
    /// Bestimmt den "schlechtesten" (= aktivsten) Status einer Gruppe.
    /// Priorität: Gesperrt (70) > In Arbeit (60) > Freigegeben (30) > Gültig (20) > Unvollständig (10) > Fertig (90) > Storniert (95)
    /// </summary>
    private static int GetWorstStatus(IEnumerable<int> statuses)
    {
        var statusList = statuses.ToList();
        int[] priority = [70, 60, 30, 20, 10, 90, 95];
        foreach (var p in priority)
        {
            if (statusList.Contains(p))
                return p;
        }
        return statusList.FirstOrDefault();
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
