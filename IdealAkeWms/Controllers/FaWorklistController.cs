using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// FA-Abarbeitungsliste je Arbeitsgang (v1.22.0, Spec §7): Filter ist EIN Arbeitsgang.
/// Pro offenem FA mit aktivem (IsRemoved=0) FaWorkStep des gewaehlten AGs eine Zeile —
/// ueber ALLE Werkbaenke. Spalten: Werkbank (Info), Merkmal-Spalten des gewaehlten AGs
/// und EINE Erledigt-Checkbox (AJAX-Toggle <c>/api/fa-work-steps/toggle-completed</c>).
/// Default-Arbeitsgang aus <see cref="User.DefaultWorkStepId"/>.
/// Feature-Gate: AppSetting <c>FaCompletionAktiv</c> (wie FA-Vervollstaendigung).
/// </summary>
[RequireVorbauAccess]
public class FaWorklistController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly IFaAttributeRepository _faAttributeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly ReadOnlyBomBuilder _readOnlyBomBuilder;
    private readonly ICurrentUserService _currentUser;

    public FaWorklistController(
        IProductionOrderRepository productionOrderRepository,
        IFaWorkStepRepository faWorkStepRepository,
        IWorkStepRepository workStepRepository,
        IFaAttributeRepository faAttributeRepository,
        IUserRepository userRepository,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        ReadOnlyBomBuilder readOnlyBomBuilder,
        ICurrentUserService currentUser)
    {
        _productionOrderRepository = productionOrderRepository;
        _faWorkStepRepository = faWorkStepRepository;
        _workStepRepository = workStepRepository;
        _faAttributeRepository = faAttributeRepository;
        _userRepository = userRepository;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _readOnlyBomBuilder = readOnlyBomBuilder;
        _currentUser = currentUser;
    }

    // GET /FaWorklist?workStepId=...
    public async Task<IActionResult> Index(
        int? workStepId,
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

        // Schritt 1: aktive Arbeitsgaenge als Filter-Optionen.
        var availableWorkSteps = await _workStepRepository.GetActiveAsync();

        // Schritt 2: Default-Arbeitsgang aus dem aktuellen User (User.DefaultWorkStepId),
        // falls kein expliziter ?workStepId mitkommt.
        if (workStepId == null)
        {
            var appUserId = _currentUser.GetCurrentAppUserId();
            if (appUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(appUserId.Value);
                workStepId = user?.DefaultWorkStepId;
            }
        }

        var vm = new FaWorklistViewModel
        {
            SelectedWorkStepId = workStepId,
            ShowDone = showDone,
            AvailableWorkSteps = availableWorkSteps,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = 0,
            },
        };

        // Arbeitsgang ist Pflicht-Filter: ohne Auswahl nur Dropdown rendern, leere Liste.
        if (workStepId == null)
        {
            return View(vm);
        }

        // Schritt 3: gewaehlter WorkStep (Header der Erledigt-Spalte) + Merkmal-Defs des AGs.
        vm.SelectedWorkStep = availableWorkSteps.FirstOrDefault(w => w.Id == workStepId.Value)
            ?? await _workStepRepository.GetByIdAsync(workStepId.Value);
        vm.AttributeColumns = await _faAttributeRepository
            .GetActiveForWorkStepsAsync(new List<int> { workStepId.Value });

        // Schritt 4: offene FAs MIT aktivem (IsRemoved=0) FaWorkStep des gewaehlten AGs —
        // ueber ALLE Werkbaenke. "Erledigt" (FA) = Sage-IsDone ODER App-Komm-erledigt
        // (IsDonePicking), konsistent zur FA-Liste/Leitstand.
        var orderIdsWithStep = (await _faWorkStepRepository.GetForWorkStepAsync(workStepId.Value))
            .Select(f => f.ProductionOrderId)
            .ToHashSet();

        var orders = (await _productionOrderRepository.GetAllOrderedAsync())
            .Where(o => !o.IsDone
                        && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                        && orderIdsWithStep.Contains(o.Id))
            .ToList();

        // Schritt 5: Termin-Berechnung (KommissionierTage/VorkommissionierTage, OHNE Beschichtung)
        // — VOR Filter/Pagination, weil bg-date/picking-date filterbar sind.
        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var rows = new List<FaWorklistRow>();
        foreach (var order in orders)
        {
            // Aktive FA-Arbeitsgaenge (IsRemoved=0) inkl. WorkStep -> den gewaehlten AG holen.
            var faSteps = await _faWorkStepRepository.GetByProductionOrderIdAsync(order.Id);
            var selectedStep = faSteps.FirstOrDefault(f => f.WorkStepId == workStepId.Value);

            // Defensive: kein aktiver FaWorkStep des AGs mehr -> Zeile ueberspringen.
            if (selectedStep == null)
            {
                continue;
            }

            // Default: erledigte FAs (gewaehlter AG IsCompleted) ausblenden.
            if (!showDone && selectedStep.IsCompleted)
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
                WorkplaceName = order.ProductionWorkplace?.Name,
                WorkStepCell = new FaWorklistCell
                {
                    FaWorkStepId = selectedStep.Id,
                    IsCompleted = selectedStep.IsCompleted,
                },
            };

            if (order.ProductionDate.HasValue)
            {
                row.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    order.ProductionDate.Value, kommissionierTage, holidays);
                row.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    row.KommissionierTermin.Value, vorkommissionierTage, holidays);
            }

            // Schritt 6: Merkmal-Werte als Anzeigetext (Dropdown -> Option.Value,
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

    // GET /FaWorklist/Bom/{id} — read-only Stueckliste fuer die Abarbeitungsliste (Spec §7).
    // Reine BOM-Anzeige via gemeinsamem ReadOnlyBomBuilder (DRY mit FaCompletion.Bom):
    // KEINE Picking-Initialisierung, keine Lagerplatz-Suggests/Dropdowns, kein Umbuchen,
    // keine Fotos, keine Bedarfsmeldungen. Rendert die Picking-View mit ReadOnly=true.
    public async Task<IActionResult> Bom(int id, string? filterText)
    {
        // Feature-Gate wie in Index (FaCompletionAktiv gated das gesamte Modul).
        var aktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.FaCompletionAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!aktiv)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["WarningMessage"] = "Dieser Fertigungsauftrag hat keine Artikelnummer.";
            return RedirectToAction(nameof(Index));
        }

        var vm = await _readOnlyBomBuilder.BuildAsync(id, filterText, _currentUser.GetCurrentAppUserId());
        if (vm == null)
            return NotFound();

        return View("~/Views/Picking/Bom.cshtml", vm);
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
    /// ColumnMap-Keys: order-number, workbench (= WorkplaceName), article-number, quantity,
    /// bg-date, picking-date, production-date + je Merkmal-Spalte dynamisch "attr-{DefinitionId}".
    /// </summary>
    private static Dictionary<string, Func<FaWorklistRow, string?>> BuildColumnMap(
        List<FaAttributeDefinition> attributeColumns)
    {
        var map = new Dictionary<string, Func<FaWorklistRow, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["order-number"] = r => r.OrderNumber,
            ["workbench"] = r => r.WorkplaceName,
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
