using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services.Oseon;

public class OseonGroupViewModelBuilder : IOseonGroupViewModelBuilder
{
    private readonly IOseonTrafficLightService _trafficLightService;
    private readonly IOseonOperationConfigRepository _operationConfigRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IHolidayRepository _holidayRepository;

    public OseonGroupViewModelBuilder(
        IOseonTrafficLightService trafficLightService,
        IOseonOperationConfigRepository operationConfigRepository,
        IBusinessDayService businessDayService,
        IHolidayRepository holidayRepository)
    {
        _trafficLightService = trafficLightService;
        _operationConfigRepository = operationConfigRepository;
        _businessDayService = businessDayService;
        _holidayRepository = holidayRepository;
    }

    public async Task<OseonOrderGroupViewModel> BuildAsync(
        string customerOrderKey,
        IEnumerable<OseonProductionOrder> subOrders,
        bool useRelevanceFilter,
        string? filterArticle,
        CancellationToken ct = default)
    {
        var opConfigs = await _operationConfigRepository.GetAllAsDictionaryAsync();
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var subOrderList = new List<OseonSubOrderViewModel>();
        foreach (var o in subOrders.OrderBy(o => o.OseonOrderNumber))
        {
            var operations = new List<OseonOperationViewModel>();
            foreach (var op in o.WorkOperations.OrderBy(op => op.PositionNumber))
            {
                var hasConfig = opConfigs.TryGetValue(op.Name, out var opConfig);
                var isRelevant = !hasConfig || opConfig!.IsOseonRelevant;

                // AG-spezifischen Soll-Termin berechnen
                DateTime? calculatedDueDate = null;
                if (o.DueDate.HasValue)
                {
                    calculatedDueDate = hasConfig
                        ? OseonDueDateCalculator.Calculate(o.DueDate.Value, opConfig!.DueDateOffsetDays, _businessDayService, holidays)
                        : o.DueDate.Value.Date;
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

            subOrderList.Add(new OseonSubOrderViewModel
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

        // Stats aus dem VOLLEN Sub-Set fuer "X/Y fertig"-Counter
        var totalSubsInGroup = subOrderList.Count;
        var finishedSubsInGroup = subOrderList.Count(s => s.OseonStatus is 90 or 95);

        // Bei aktivem Artikel-Filter Sub-Auftraege auf Treffer reduzieren.
        // Worst-Color/Status weiter aus VOLLEM Set — die Kundenauftrag-Gruppe behaelt ihren Status-Kontext.
        var displaySubs = subOrderList;
        if (!string.IsNullOrWhiteSpace(filterArticle))
        {
            var artTerm = filterArticle.Trim();
            displaySubs = subOrderList
                .Where(s => s.ArticleNumber != null
                            && s.ArticleNumber.Contains(artTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Worst color: Red > Yellow > Blue > Gray > Green
        var worstColor = subOrderList.Count > 0
            ? subOrderList.Max(s => s.Color)
            : TrafficLightColor.Gray;

        // Aggregierter Status: der "schlechteste" (= aktivste/dringendste) Status der Gruppe
        var worstStatus = subOrderList.Count > 0
            ? GetWorstStatus(subOrderList.Select(s => s.OseonStatus))
            : 0;

        return new OseonOrderGroupViewModel
        {
            CustomerOrderNumber = customerOrderKey,
            WorstColor = worstColor,
            TotalSubOrders = totalSubsInGroup,
            FinishedSubOrders = finishedSubsInGroup,
            GroupStatusText = OseonStatusHelper.GetStatusText(worstStatus),
            GroupStatusBadgeClass = OseonStatusHelper.GetStatusBadgeClass(worstStatus),
            SubOrders = displaySubs
        };
    }

    /// <summary>
    /// Bestimmt den "schlechtesten" (= aktivsten) Status einer Gruppe.
    /// Prioritaet: Gesperrt (70) > In Arbeit (60) > Freigegeben (30) > Gueltig (20) > Unvollstaendig (10) > Fertig (90) > Storniert (95)
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
}
