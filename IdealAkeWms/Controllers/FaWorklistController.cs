using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// FA-Abarbeitungsliste je Werkbank (v1.22.0, Spec §7): pro offenem FA eine Zeile
/// mit Erledigt-Checkboxen je gemapptem Arbeitsgang der Werkbank (AJAX-Toggle
/// <c>/api/fa-work-steps/toggle-completed</c>), Merkmal-Spalten der gemappten AGs
/// und Orphan-Badge fuer offene AGs ausserhalb des Werkbank-Mappings.
/// Feature-Gate: AppSetting <c>FaCompletionAktiv</c> (wie FA-Vervollstaendigung).
/// </summary>
[RequireVorbauAccess]
public class FaWorklistController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly IFaAttributeRepository _faAttributeRepository;
    private readonly IProductionWorkplaceRepository _productionWorkplaceRepository;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly ICurrentUserService _currentUser;

    public FaWorklistController(
        IProductionOrderRepository productionOrderRepository,
        IFaWorkStepRepository faWorkStepRepository,
        IWorkStepRepository workStepRepository,
        IFaAttributeRepository faAttributeRepository,
        IProductionWorkplaceRepository productionWorkplaceRepository,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        ICurrentUserService currentUser)
    {
        _productionOrderRepository = productionOrderRepository;
        _faWorkStepRepository = faWorkStepRepository;
        _workStepRepository = workStepRepository;
        _faAttributeRepository = faAttributeRepository;
        _productionWorkplaceRepository = productionWorkplaceRepository;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _currentUser = currentUser;
    }

    // GET /FaWorklist?workplaceId=...
    public async Task<IActionResult> Index(
        int? workplaceId,
        bool showDone = false,
        int page = 1,
        int? pageSize = null)
    {
        // Feature-Gate: FaCompletionAktiv gated Vervollstaendigen UND Abarbeitungsliste
        // (Gate-Muster wie RequireFaCompletionAccessFilter, kein zweites Setting).
        var aktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.FaCompletionAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!aktiv)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var userDefaultPageSize = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var vm = new FaWorklistViewModel
        {
            SelectedWorkplaceId = workplaceId,
            ShowDone = showDone,
            AvailableWorkplaces = await _productionWorkplaceRepository.GetAllOrderedAsync(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = 0,
            },
        };

        // Werkbank ist Pflicht-Filter: ohne Auswahl nur Dropdown rendern, leere Liste.
        if (workplaceId == null)
        {
            return View(vm);
        }

        // Schritt 2: gemappte WorkSteps der Werkbank = Spalten; Merkmal-Defs der gemappten AGs.
        var mappedWorkStepIds = await _productionWorkplaceRepository.GetWorkStepIdsAsync(workplaceId.Value);
        var mappedIdSet = mappedWorkStepIds.ToHashSet();
        var allWorkSteps = await _workStepRepository.GetAllAsync();
        vm.MappedWorkSteps = allWorkSteps
            .Where(w => mappedIdSet.Contains(w.Id))
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Code)
            .ToList();
        vm.AttributeColumns = mappedWorkStepIds.Count == 0
            ? new List<FaAttributeDefinition>()
            : await _faAttributeRepository.GetActiveForWorkStepsAsync(mappedWorkStepIds);

        // Schritt 3: offene FAs (!IsDone) der Werkbank.
        var orders = (await _productionOrderRepository.GetAllOrderedAsync())
            .Where(o => !o.IsDone && o.ProductionWorkplaceId == workplaceId.Value)
            .ToList();

        // Schritt 4: Termin-Berechnung (KommissionierTage/VorkommissionierTage, OHNE Beschichtung)
        // — VOR Filter/Pagination, weil bg-date/picking-date filterbar sind.
        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var rows = new List<FaWorklistRow>();
        foreach (var order in orders)
        {
            // Aktive FA-Arbeitsgaenge (IsRemoved=0) inkl. WorkStep
            var faSteps = await _faWorkStepRepository.GetByProductionOrderIdAsync(order.Id);
            var mappedSteps = faSteps.Where(f => mappedIdSet.Contains(f.WorkStepId)).ToList();

            // Nur FAs mit mind. einem gemappten AG dieser Werkbank.
            if (mappedSteps.Count == 0)
            {
                continue;
            }

            // Default: komplett erledigte FAs (alle gemappten AGs IsCompleted) ausblenden.
            if (!showDone && mappedSteps.All(f => f.IsCompleted))
            {
                continue;
            }

            var row = new FaWorklistRow
            {
                ProductionOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ArticleNumber = order.ArticleNumber,
                Quantity = order.Quantity,
                ProductionDate = order.ProductionDate,
                WorkStepCells = mappedSteps.ToDictionary(
                    f => f.WorkStepId,
                    f => new FaWorklistCell { FaWorkStepId = f.Id, IsCompleted = f.IsCompleted }),
                // Schritt 6: offene AGs ausserhalb des Mappings ("+N weitere AG"-Badge).
                OrphanWorkStepCount = faSteps.Count(f => !mappedIdSet.Contains(f.WorkStepId) && !f.IsCompleted),
            };

            if (order.ProductionDate.HasValue)
            {
                row.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    order.ProductionDate.Value, kommissionierTage, holidays);
                row.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    row.KommissionierTermin.Value, vorkommissionierTage, holidays);
            }

            // Schritt 5: Merkmal-Werte als Anzeigetext (Dropdown -> Option.Value,
            // Boolean -> JA/NEIN, fehlend -> "").
            var values = await _faAttributeRepository.GetValuesByProductionOrderIdAsync(order.Id);
            var valuesByDefinition = values.ToDictionary(v => v.FaAttributeDefinitionId);
            foreach (var def in vm.AttributeColumns)
            {
                row.AttributeValues[def.Id] = FormatAttributeValue(def, valuesByDefinition.GetValueOrDefault(def.Id));
            }

            rows.Add(row);
        }

        // Schritt 7: ColumnMap (gerenderte Texte!) + Apply VOR Pagination.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var columnMap = BuildColumnMap(vm.AttributeColumns);
        var filteredRows = ColumnFilterHelper.Apply(rows, columnFilters, columnMap).ToList();

        var totalCount = filteredRows.Count;
        var pagedRows = filteredRows
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToList();

        vm.Items = pagedRows;
        vm.Pagination.TotalCount = totalCount;

        // Schritt 8: enaio DMS-Links (Bulk-Lookup fuer die angezeigten FA-Nummern).
        var orderNumbers = pagedRows.Select(i => i.OrderNumber).Distinct().ToList();
        vm.EnaioDmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

        return View(vm);
    }

    private static string FormatAttributeValue(FaAttributeDefinition def, FaAttributeValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return def.AttributeType switch
        {
            AttributeType.Boolean => value.BooleanValue == true ? "JA"
                : value.BooleanValue == false ? "NEIN"
                : string.Empty,
            AttributeType.Dropdown => value.SelectedOption?.Value ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static string FormatDateForFilter(DateTime? d) // KW-Format wie FA-Liste
        => d == null ? string.Empty : $"{d:dd.MM.yyyy} KW{System.Globalization.ISOWeek.GetWeekOfYear(d.Value)}".ToLowerInvariant();

    /// <summary>
    /// ColumnMap-Keys: order-number, article-number, quantity, bg-date, picking-date,
    /// production-date + je Merkmal-Spalte dynamisch "attr-{DefinitionId}".
    /// </summary>
    private static Dictionary<string, Func<FaWorklistRow, string?>> BuildColumnMap(
        List<FaAttributeDefinition> attributeColumns)
    {
        var map = new Dictionary<string, Func<FaWorklistRow, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["order-number"] = r => r.OrderNumber,
            ["article-number"] = r => r.ArticleNumber,
            ["quantity"] = r => r.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["bg-date"] = r => FormatDateForFilter(r.VorkommissionierTermin),
            ["picking-date"] = r => FormatDateForFilter(r.KommissionierTermin),
            ["production-date"] = r => FormatDateForFilter(r.ProductionDate),
        };

        foreach (var def in attributeColumns)
        {
            var definitionId = def.Id;
            map[$"attr-{definitionId}"] = r => r.AttributeValues.GetValueOrDefault(definitionId);
        }

        return map;
    }
}
