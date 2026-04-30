using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Helpers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireReportingAccess]
public class OseonReportingController : Controller
{
    private readonly IOseonProductionOrderRepository _orders;
    private readonly IOseonOperationConfigRepository _configs;
    private readonly IHolidayRepository _holidays;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IAppSettingRepository _settings;
    private readonly IBusinessDayService _businessDays;

    public OseonReportingController(
        IOseonProductionOrderRepository orders,
        IOseonOperationConfigRepository configs,
        IHolidayRepository holidays,
        IProductionWorkplaceRepository workplaces,
        IAppSettingRepository settings,
        IBusinessDayService businessDays)
    {
        _orders = orders;
        _configs = configs;
        _holidays = holidays;
        _workplaces = workplaces;
        _settings = settings;
        _businessDays = businessDays;
    }

    public async Task<IActionResult> OperationsOverview(
        int? workplaceId,
        string[]? operationNames,
        string? customerOrderNumber,
        string? faNumber,
        int? horizonDays,
        OseonReportingSlice? slice)
    {
        var effectiveSlice = slice ?? OseonReportingSlice.Today;

        var defaultHorizonText = await _settings.GetValueAsync(AppSettingKeys.OseonReportingHorizonDays);
        var defaultHorizon = int.TryParse(defaultHorizonText, out var parsed) ? parsed : 10;
        var horizonEffective = Math.Clamp(horizonDays ?? defaultHorizon, 1, 60);

        var lookbackText = await _settings.GetValueAsync(AppSettingKeys.OseonReportingOverdueLookbackDays);
        var lookbackEffective = int.TryParse(lookbackText, out var lookbackParsed)
            ? Math.Clamp(lookbackParsed, 1, 365)
            : 90;

        var today = DateTime.Today;
        var fromDate = today.AddDays(-lookbackEffective);
        var toDate = today.AddDays(horizonEffective);

        var opNamesList = (operationNames == null || operationNames.Length == 0)
            ? null
            : operationNames.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

        var queryResult = await _orders.GetRelevantOperationsForReportingAsync(
            workplaceId, opNamesList, customerOrderNumber, faNumber, fromDate, toDate);

        var allHolidays = await _holidays.GetAllAsync();
        var holidaySet = new HashSet<DateTime>(allHolidays.Select(h => h.Date.Date));

        var rows = new List<OseonReportingRowViewModel>();
        foreach (var qr in queryResult.Rows)
        {
            if (qr.Order.DueDate is null || qr.Config is null) continue;

            var calcDue = OseonDueDateCalculator.Calculate(
                qr.Order.DueDate.Value, qr.Config.DueDateOffsetDays, _businessDays, holidaySet);

            OseonReportingSlice rowSlice;
            if (calcDue < today && qr.WorkOperation.OseonStatus != 90)
                rowSlice = OseonReportingSlice.Overdue;
            else if (calcDue == today)
                rowSlice = OseonReportingSlice.Today;
            else if (calcDue > today && calcDue <= today.AddDays(horizonEffective))
                rowSlice = OseonReportingSlice.Future;
            else
                continue;

            var isDoneToday = qr.WorkOperation.OseonStatus == 90
                              && qr.WorkOperation.LastStatusReportInOseon?.Date == today;

            rows.Add(new OseonReportingRowViewModel(
                qr.Order.CustomerOrderNumber ?? string.Empty,
                qr.Order.OseonOrderNumber,
                qr.WorkOperation.PositionNumber,
                qr.WorkOperation.Name,
                qr.Order.WorkplaceName,
                calcDue,
                qr.WorkOperation.OseonStatus,
                OseonStatusHelper.GetStatusText(qr.WorkOperation.OseonStatus),
                OseonStatusHelper.GetStatusBadgeClass(qr.WorkOperation.OseonStatus),
                rowSlice,
                isDoneToday));
        }

        var kpis = new OseonReportingKpiViewModel(
            OverdueCount: rows.Count(r => r.Slice == OseonReportingSlice.Overdue),
            TodayPlannedCount: rows.Count(r => r.Slice == OseonReportingSlice.Today),
            TodayDoneCount: rows.Count(r => r.IsDoneToday),
            FutureCount: rows.Count(r => r.Slice == OseonReportingSlice.Future));

        var filteredRows = effectiveSlice switch
        {
            OseonReportingSlice.Overdue => rows.Where(r => r.Slice == OseonReportingSlice.Overdue).ToList(),
            OseonReportingSlice.Today => rows.Where(r => r.Slice == OseonReportingSlice.Today).ToList(),
            OseonReportingSlice.Future => rows.Where(r => r.Slice == OseonReportingSlice.Future).ToList(),
            _ => rows
        };

        filteredRows = filteredRows
            .OrderBy(r => r.CalculatedDueDate)
            .ThenBy(r => r.OseonStatus)
            .ThenBy(r => r.PositionNumber)
            .ToList();

        var futureGroups = new List<OseonReportingDayGroup>();
        if (effectiveSlice == OseonReportingSlice.Future)
        {
            futureGroups = filteredRows
                .GroupBy(r => r.CalculatedDueDate)
                .OrderBy(g => g.Key)
                .Select(g => new OseonReportingDayGroup(g.Key, g.Count(), g.ToList()))
                .ToList();
        }

        var allConfigs = await _configs.GetAllAsync();
        var availableOpNames = allConfigs
            .Where(c => c.IsOseonRelevant)
            .Select(c => c.OperationName)
            .OrderBy(n => n)
            .ToList();

        var availableWorkplaces = (await _workplaces.GetAllAsync())
            .OrderBy(w => w.Name)
            .ToList();

        var vm = new OseonReportingViewModel
        {
            Kpis = kpis,
            Rows = filteredRows,
            FutureDayGroups = futureGroups,
            OperationsWithoutConfigCount = queryResult.OperationsWithoutConfigCount,
            DataAsOf = queryResult.DataAsOf,
            Filter = new OseonReportingFilter
            {
                WorkplaceId = workplaceId,
                OperationNames = opNamesList ?? new(),
                CustomerOrderNumber = customerOrderNumber,
                FaNumber = faNumber,
                HorizonDaysOverride = horizonDays,
                Slice = effectiveSlice
            },
            AvailableOperationNames = availableOpNames,
            AvailableWorkplaces = availableWorkplaces,
            HorizonDaysEffective = horizonEffective
        };

        return View(vm);
    }
}
