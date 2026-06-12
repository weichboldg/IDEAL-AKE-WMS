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
    private readonly IBomRepository _bomRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IArticleAttributeRepository _articleAttributeRepository;
    private readonly IUserRepository _userRepository;
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
        IBomRepository bomRepository,
        IStockMovementRepository stockMovementRepository,
        IArticleAttributeRepository articleAttributeRepository,
        IUserRepository userRepository,
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
        _bomRepository = bomRepository;
        _stockMovementRepository = stockMovementRepository;
        _articleAttributeRepository = articleAttributeRepository;
        _userRepository = userRepository;
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

    // GET /FaWorklist/Bom/{id} — read-only Stueckliste fuer die Abarbeitungsliste (Spec §7).
    // Bewusste Kopie der reinen BOM-Anzeige-Teile aus PickingController.Bom (Plan Task 14):
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

        // User-Defaults fuer client-seitige Spaltenfilter (wie PickingController.Bom).
        string? defaultFilterBeschaffung = null;
        string? defaultFilterArtikelgruppe = null;
        var recursiveFilterSearch = false;
        var appUserId = _currentUser.GetCurrentAppUserId();
        if (appUserId.HasValue)
        {
            var currentUser = await _userRepository.GetByIdAsync(appUserId.Value);
            if (currentUser != null)
            {
                defaultFilterBeschaffung = currentUser.DefaultFilterBeschaffung;
                defaultFilterArtikelgruppe = currentUser.DefaultFilterArtikelgruppe;
                recursiveFilterSearch = currentUser.RecursiveFilterSearch;
            }
        }

        var bomResult = await _bomRepository.GetBomItemsAsync(order.ArticleNumber);
        var bomItems = bomResult.Items;

        var articleNumbers = bomItems
            .Select(b => b.Ressourcenummer)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => r!)
            .Distinct()
            .ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);
        var categoryByArticle = await _articleAttributeRepository.GetCategoryNamesByArticleNumbersAsync(articleNumbers);

        // Baugruppen-Hierarchie: sammle alle Baugruppen-Werte (fuer Baum-Navigation).
        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var viewItems = bomItems.Select(bom =>
        {
            stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var stockLocations);

            // TreeLevel aus Position ableiten: Anzahl Punkte = Ebene ("15.1" = 1).
            var treeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.');

            return new BomItemViewModel
            {
                Artikelnummer = bom.Artikelnummer,
                Position = bom.Position,
                Baugruppe = bom.Baugruppe,
                Ressourcenummer = bom.Ressourcenummer,
                Bezeichnung1 = bom.Bezeichnung1,
                Bezeichnung2 = bom.Bezeichnung2,
                Menge = bom.Menge * order.Quantity,
                Beschaffungsartikel = bom.Beschaffungsartikel,
                Artikelgruppe = bom.Artikelgruppe,
                KategorieName = categoryByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var catName) ? catName : null,
                StockLocations = stockLocations ?? new List<StockLocationInfo>(),
                TreeLevel = treeLevel,
                IsBaugruppe = !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer)
            };
        })
        .OrderBy(v => v.Position, new NaturalPositionComparer())
        .ToList();

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            viewItems = viewItems.Where(i =>
                (i.Ressourcenummer != null && i.Ressourcenummer.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Bezeichnung1 != null && i.Bezeichnung1.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Bezeichnung2 != null && i.Bezeichnung2.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Baugruppe != null && i.Baugruppe.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Position != null && i.Position.Contains(filterText, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        var vm = new BomViewModel
        {
            ProductionOrderId = id,
            OrderNumber = order.OrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Items = viewItems,
            FilterText = filterText,
            DefaultFilterBeschaffung = defaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = defaultFilterArtikelgruppe,
            DataSource = bomResult.DataSource,
            RecursiveFilterSearch = recursiveFilterSearch,
            ReadOnly = true
        };

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
