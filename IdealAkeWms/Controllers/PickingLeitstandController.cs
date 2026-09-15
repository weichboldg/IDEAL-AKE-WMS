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
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IServiceSettingRepository _serviceSettingRepository;
    private readonly IProductionOrderBdeStatusRepository _bdeStatusRepository;
    private readonly IBdeBookingRepository _bdeBookingRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly ILogger<PickingLeitstandController> _logger;

    public PickingLeitstandController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        IFaWorkStepRepository faWorkStepRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IUserRepository userRepository,
        IServiceSettingRepository serviceSettingRepository,
        IProductionOrderBdeStatusRepository bdeStatusRepository,
        IBdeBookingRepository bdeBookingRepository,
        IArticleRepository articleRepository,
        ILogger<PickingLeitstandController> logger)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _faWorkStepRepository = faWorkStepRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _userRepository = userRepository;
        _serviceSettingRepository = serviceSettingRepository;
        _bdeStatusRepository = bdeStatusRepository;
        _bdeBookingRepository = bdeBookingRepository;
        _articleRepository = articleRepository;
        _logger = logger;
    }

    // IDEAL FA-Liste-Hierarchie: Master-Zustand lesen (Muster wie ProductionOrdersController).
    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase));

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

        // Datum- UND VK-VA-Filter werden in C# nach dem FaWorkStep-Pivot-/Termin-Mapping angewendet
        // (beide Datenquellen liegen ausserhalb der SQL-Query). "matchcode" (Task 2, Artikelstamm):
        // seit dem Umbau auf Article.Matchcode ebenfalls kein SQL-Praedikat mehr (A1 — Article ist per
        // Equi-Join, nicht per BuildExtraInfoOrContains-Subquery filterbar) -- gehoert deshalb hier zur
        // gleichen In-Memory-Gruppe. Wenn aktiv: alle Text-gefilterten Rows materialisieren, mappen,
        // dann erst paginieren.
        var memoryFilters = columnFilters
            .Where(kv => LeitstandDateColumnKeys.Contains(kv.Key) || LeitstandWorkStepColumnKeys.ContainsKey(kv.Key)
                         || kv.Key.Equals("matchcode", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var hasMemoryFilters = memoryFilters.Count > 0;
        var sqlColumnFilters = hasMemoryFilters
            ? columnFilters.Where(kv => !LeitstandDateColumnKeys.Contains(kv.Key) && !LeitstandWorkStepColumnKeys.ContainsKey(kv.Key)
                                      && !kv.Key.Equals("matchcode", StringComparison.OrdinalIgnoreCase))
                           .ToDictionary(kv => kv.Key, kv => kv.Value)
            : columnFilters;

        var sqlPageSize = hasMemoryFilters ? int.MaxValue : effectivePageSize;
        var sqlPage = hasMemoryFilters ? 1 : page;

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var beschichtungAbholtageSetting = await _settingRepository.GetValueAsync(AppSettingKeys.BeschichtungAbholtage) ?? "Dienstag,Donnerstag";
        var pickupDays = _businessDayService.ParsePickupDays(beschichtungAbholtageSetting);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();
        var lackierteilName = await _settingRepository.GetValueAsync(AppSettingKeys.LackierteilKategorieName);
        var coatingFeatureActive = !string.IsNullOrWhiteSpace(lackierteilName);
        ViewBag.LackierteilKategorieName = lackierteilName;

        // Matchcode (Task 2, Artikelstamm): kein DB-Feld auf ProductionOrder mehr — je Aufbau (flach
        // ODER hierarchisch) per Batch-Lookup gegen Article befuellt, BEVOR MapItem/der Postfilter
        // laeuft. MapItem liest das Ergebnis ueber Closure statt zusaetzlichem Parameter.
        var matchcodes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Gemeinsame Zeilen-Anreicherung (Pivot + PickingStatus + Termine) — flach UND gruppiert.
        // Detail-Pivot liefert pro aktivem AG FaWorkStepId + Status (VK-VA-Zellen, 3-Wert-Erledigt).
        PickingLeitstandItem MapItem(LeitstandOrderRow o, ProductionOrderPickingStatus? ps,
            Dictionary<string, FaWorkStepPivotCell> grp)
        {
            var item = new PickingLeitstandItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Quantity = o.Quantity,
                Customer = o.Customer,
                ArticleNumber = o.ArticleNumber,
                Matchcode = matchcodes.GetValueOrDefault(o.ArticleNumber ?? string.Empty),
                Description1 = o.Description1,
                Description2 = o.Description2,
                ProductionDate = o.ProductionDate,
                DeliveryDate = o.DeliveryDate,
                IsDone = o.IsDone || o.IsDonePicking,
                WorkplaceName = o.WorkplaceName,
                Kaeltemittel = o.Kaeltemittel,
                Ventil = o.Ventil,
                AusfuehrungEZ = o.AusfuehrungEZ,
                Maschine = o.Maschine,
                SageStatus = o.SageStatus,
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
                WorkSteps = grp,
                SubOrderNumber = o.SubOrderNumber,
                ParentSubOrderNumber = o.ParentSubOrderNumber,
                SageMissingSince = o.SageMissingSince,
            };

            // Werkbank-Override „Abweichende Vorkommissioniertage" schlaegt den globalen Wert (v1.27.0).
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
                // Beschichtungstermin: shared CoatingDateCalculator (DRY mit FA-Abarbeitungsliste).
                item.BeschichtungTermin = CoatingDateCalculator.Compute(
                    item.VorkommissionierTermin, beschichtungTage, holidays, pickupDays,
                    ps?.HasCoatingParts ?? false, coatingFeatureActive, _businessDayService);
            }

            return item;
        }

        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var pickerAssignmentEnabled = leitstandAktiv && (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var canPick = await _currentUserService.CanPickAsync();
        var canManagePickingRelease = await _currentUserService.CanManagePickingReleaseAsync();

        // IDEAL FA-Liste-Hierarchie: nur bei Master ProduktionsauftragHierarchisch = true.
        var hierarchical = IsTrue(await _serviceSettingRepository.GetValueAsync(
            IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master));

        PickingLeitstandViewModel vm;

        if (hierarchical)
        {
            // Paginierung ueber GRUPPEN; bei Memory-Filtern alle Gruppen laden, in C# filtern/paginieren.
            var sqlGroupPageSize = hasMemoryFilters ? int.MaxValue : effectivePageSize;
            var sqlGroupPage = hasMemoryFilters ? 1 : page;
            var grouped = await _productionOrderRepository.GetForLeitstandGroupedAsync(
                filterOrderNumber, filterArticleNumber, filterCustomer, showDone, sqlGroupPage, sqlGroupPageSize, sqlColumnFilters);

            var allRows = grouped.Groups.SelectMany(g => g.Rows).ToList();

            // Matchcode-Join (Task 2): EIN Batch-Lookup fuer die gesamte (bei Memory-Filtern
            // ungepaginierte) Zeilenmenge, VOR MapItem/dem "matchcode"-Postfilter.
            matchcodes = await MatchcodeLookup.BuildAsync(
                _articleRepository, allRows.Select(r => r.ArticleNumber), _logger, "PickingLeitstand (hierarchisch)");

            var groupIds = allRows.Select(r => r.Id).ToList();
            var groupPivotH = await _faWorkStepRepository.GetWorkStepDetailPivotAsync(groupIds);
            var pickingStatusesH = await _pickingStatusRepository.GetByProductionOrderIdsAsync(groupIds);

            var listGroups = grouped.Groups.Select(g => new PickingLeitstandGroup
            {
                OrderNumber = g.OrderNumber,
                Items = g.Rows.Select(r => MapItem(
                    r,
                    pickingStatusesH.GetValueOrDefault(r.Id),
                    groupPivotH.GetValueOrDefault(r.Id) ?? new Dictionary<string, FaWorkStepPivotCell>())).ToList()
            }).ToList();

            var totalGroupCount = grouped.TotalGroupCount;
            var totalRowCount = grouped.TotalRowCount;

            // Datum-/VK-VA-Filter wirken auf ZEILEN (Z3b): Gruppe bleibt, solange >=1 Zeile passt.
            if (hasMemoryFilters)
            {
                foreach (var (key, raw) in memoryFilters)
                {
                    var (tokens, negate) = ColumnFilterHelper.Parse(raw);
                    if (tokens.Count == 0) continue;
                    if (LeitstandWorkStepColumnKeys.TryGetValue(key, out var code))
                        foreach (var g in listGroups)
                            g.Items = g.Items.Where(it => MatchLeitstandWorkStepFilter(it, code, tokens, negate)).ToList();
                    else if (key.Equals("matchcode", StringComparison.OrdinalIgnoreCase))
                        foreach (var g in listGroups)
                            g.Items = g.Items.Where(it => MatchLeitstandTextFilter(it.Matchcode, tokens, negate)).ToList();
                    else
                        foreach (var g in listGroups)
                            g.Items = g.Items.Where(it => MatchLeitstandDateFilter(it, key, tokens, negate)).ToList();
                }
                listGroups = listGroups.Where(g => g.Items.Count > 0).ToList(); // leere Gruppen entfernen (Z3b)
                totalGroupCount = listGroups.Count;
                totalRowCount = listGroups.Sum(g => g.Items.Count);
                listGroups = listGroups.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
            }

            var groupDmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(
                listGroups.Select(g => g.OrderNumber).Distinct().ToList());

            vm = new PickingLeitstandViewModel
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
                CanPick = canPick,
                CanManagePickingRelease = canManagePickingRelease,
                LeitstandAktiv = leitstandAktiv,
                PickerAssignmentEnabled = pickerAssignmentEnabled,
                EnaioDmsLinks = groupDmsLinks,
                Pagination = new PaginationState
                {
                    CurrentPage = page,
                    PageSize = effectivePageSize,
                    PageSizeRaw = rawPageSize,
                    TotalCount = totalGroupCount // Paginierung ueber Gruppen (F3)
                }
            };

            if (pickerAssignmentEnabled)
                ViewBag.ActivePickers = await _userRepository.GetActivePickersAsync();

            return View(vm);
        }

        // --- Flachmodus (AKE / Master false): unveraendert zu heute ---
        var ordersPage = await _productionOrderRepository.GetForLeitstandAsync(
            filterOrderNumber, filterArticleNumber, filterCustomer, showDone, sqlPage, sqlPageSize, sqlColumnFilters);
        var orders = ordersPage.Rows;

        // Matchcode-Join (Task 2): siehe hierarchischer Zweig oben, hier fuer die (bei
        // Memory-Filtern ungepaginierte) Flachmodus-Zeilenmenge, VOR MapItem/dem Postfilter.
        matchcodes = await MatchcodeLookup.BuildAsync(
            _articleRepository, orders.Select(o => o.ArticleNumber), _logger, "PickingLeitstand (flach)");

        var orderIds = orders.Select(o => o.Id).ToList();
        var groupPivot = await _faWorkStepRepository.GetWorkStepDetailPivotAsync(orderIds);
        var pickingStatuses = await _pickingStatusRepository.GetByProductionOrderIdsAsync(orderIds);

        var viewItems = orders.Select(o => MapItem(
            o,
            pickingStatuses.GetValueOrDefault(o.Id),
            groupPivot.GetValueOrDefault(o.Id) ?? new Dictionary<string, FaWorkStepPivotCell>())).ToList();

        // Wenn Datum-/VK-VA-Filter aktiv waren: in-Memory filtern und C#-seitig paginieren.
        int finalTotalCount = ordersPage.TotalCount;
        if (hasMemoryFilters)
        {
            foreach (var (key, raw) in memoryFilters)
            {
                var (tokens, negate) = ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                if (LeitstandWorkStepColumnKeys.TryGetValue(key, out var code))
                    viewItems = viewItems.Where(it => MatchLeitstandWorkStepFilter(it, code, tokens, negate)).ToList();
                else if (key.Equals("matchcode", StringComparison.OrdinalIgnoreCase))
                    viewItems = viewItems.Where(it => MatchLeitstandTextFilter(it.Matchcode, tokens, negate)).ToList();
                else
                    viewItems = viewItems.Where(it => MatchLeitstandDateFilter(it, key, tokens, negate)).ToList();
            }
            finalTotalCount = viewItems.Count;
            viewItems = viewItems.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        }

        // enaio DMS-Links laden (Bulk-Lookup fuer alle FA-Nummern)
        var orderNumbers = viewItems.Select(i => i.OrderNumber).Distinct().ToList();
        var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

        vm = new PickingLeitstandViewModel
        {
            Items = viewItems,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
            KommissionierTage = kommissionierTage,
            VorkommissionierTage = vorkommissionierTage,
            BeschichtungTage = beschichtungTage,
            CanPick = canPick,
            CanManagePickingRelease = canManagePickingRelease,
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
    // IDEAL Teil 7/8 — Kaskade „Alle Sub-FAs fertigmelden" (nur Gruppen-Kopfzeile).
    // Traeger: IsDoneBde (ProductionOrderBdeStatus). Zeilen-Toggle bleibt unveraendert; keine
    // Gruppen-Ruecknahme (setzt nur true). Zugriff wie die Einzelaktion (BdeStatusApi = PickingAccess).
    // ---------------------------------------------------------------------

    /// <summary>
    /// Vorschau-Zahlen fuer den Bestaetigungsdialog: Gesamtzahl Sub-FAs dieser HauptFA und — separat —
    /// wie viele davon eine offene Buchung haben (jede nicht beendete, nicht stornierte BDE-Buchung).
    /// Filter-unabhaengig (die Kaskade betrifft ALLE Nachfahren, nicht nur die sichtbaren Zeilen).
    /// </summary>
    [HttpGet]
    [RequirePickingAccess]
    public async Task<IActionResult> CascadeDonePreview(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return BadRequest("orderNumber fehlt.");

        var totalSubFas = await _productionOrderRepository.CountByOrderNumberAsync(orderNumber);
        var withOpenBooking = await _bdeBookingRepository.CountSubFasWithOpenBookingForOrderNumberAsync(orderNumber);

        return Ok(new { orderNumber, totalSubFas, withOpenBooking });
    }

    /// <summary>
    /// Fuehrt die Kaskade aus: setzt IsDoneBde=true fuer ALLE Nachfahren-Sub-FAs dieser HauptFA
    /// (atomar), protokolliert EINEN Log-Eintrag. Setzt nur true (keine Gruppen-Ruecknahme).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> CascadeDone(string? orderNumber, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            TempData["WarningMessage"] = "Keine HauptFA angegeben — nichts fertiggemeldet.";
            return RedirectBackOrIndex(returnUrl);
        }

        var affected = await _bdeStatusRepository.SetIsDoneBdeForOrderNumberAsync(
            orderNumber, true,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        // AK D: EIN Log-Eintrag je Kaskade (HauptFA, Anzahl Sub-FAs, Benutzer) — als eine Aktion erkennbar.
        _logger.LogInformation(
            "IDEAL-Kaskade Fertigmeldung (IsDoneBde): HauptFA {OrderNumber} → {Count} Sub-FA(s) auf true durch {User} ({Windows}).",
            orderNumber, affected, _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = affected > 0
            ? $"HauptFA {orderNumber}: {affected} Sub-FA(s) fertiggemeldet."
            : $"HauptFA {orderNumber}: keine Sub-FAs gefunden — nichts geändert.";

        return RedirectBackOrIndex(returnUrl);
    }

    // ---------------------------------------------------------------------
    // Freigabe-Kaskade „Alle Sub-FAs freigeben" (nur Gruppen-Kopfzeile).
    // Traeger: IsReleasedForPicking (ProductionOrderPickingStatus). Zeilen-Toggle (ToggleRelease)
    // bleibt unveraendert. Zugriff wie die Einzel-/Bulk-Freigabe (Leitstand-Recht, nicht Picking-Recht
    // wie CascadeDone) — Freigabe ist eine Leitstand-Aufgabe. Die Zuweisungspflicht aus
    // ToggleRelease/BulkRelease gilt unveraendert auch fuer diese Massenaktion.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Vorschau-Zahlen fuer den Bestaetigungsdialog: Gesamtzahl Sub-FAs dieser HauptFA, wie viele
    /// davon bereits freigegeben sind, und wie viele die Kaskade noch freigeben wuerde.
    /// </summary>
    [HttpGet]
    [RequireLeitstandAccess]
    public async Task<IActionResult> CascadeReleasePreview(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return BadRequest("orderNumber fehlt.");

        var totalSubFas = await _productionOrderRepository.CountByOrderNumberAsync(orderNumber);
        var alreadyReleased = await _pickingStatusRepository.CountReleasedByOrderNumberAsync(orderNumber);

        return Ok(new { orderNumber, totalSubFas, alreadyReleased, toRelease = totalSubFas - alreadyReleased });
    }

    /// <summary>
    /// Fuehrt die Kaskade aus: setzt IsReleasedForPicking=true fuer alle noch nicht freigegebenen
    /// Sub-FAs dieser HauptFA. Die Zuweisungspflicht (AppSettingKeys.KommissionierungMitZuweisung)
    /// gilt unveraendert wie bei der Einzelfreigabe (ToggleRelease) und der Mehrfachfreigabe
    /// (BulkRelease) — eine Massenaktion darf diese Pflicht nicht umgehen.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> CascadeRelease(string? orderNumber, int? assignedPickerId, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            TempData["WarningMessage"] = "Keine HauptFA angegeben — nichts freigegeben.";
            return RedirectBackOrIndex(returnUrl);
        }

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(
            AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (pickerAssignmentEnabled && !assignedPickerId.HasValue)
        {
            TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
            return RedirectBackOrIndex(returnUrl);
        }

        string? pickerName = null;
        if (assignedPickerId.HasValue)
            pickerName = (await _userRepository.GetByIdAsync(assignedPickerId.Value))?.Name;

        var affected = await _pickingStatusRepository.SetReleaseForOrderNumberAsync(
            orderNumber, assignedPickerId, pickerName,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        _logger.LogInformation(
            "IDEAL-Kaskade Freigabe (IsReleasedForPicking): HauptFA {OrderNumber} → {Count} Sub-FA(s) " +
            "freigegeben durch {User} ({Windows}), Kommissionierer {Picker}.",
            orderNumber, affected, _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName(), pickerName ?? "(keiner)");

        TempData["SuccessMessage"] = affected > 0
            ? $"HauptFA {orderNumber}: {affected} Sub-FA(s) freigegeben."
            : $"HauptFA {orderNumber}: alle Sub-FAs waren bereits freigegeben — nichts geändert.";

        return RedirectBackOrIndex(returnUrl);
    }

    private IActionResult RedirectBackOrIndex(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
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
    /// VK-VA-Spalten-Filter-Keys -> WorkStep-Code. Die Zelle stammt aus dem FaWorkStep-Detail-Pivot
    /// (ausserhalb der SQL-Query), darum wird hier — wie bei den Datumsspalten — in C# gefiltert.
    /// </summary>
    private static readonly Dictionary<string, string> LeitstandWorkStepColumnKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cooling"] = "VK",
        ["fan"] = "VL",
        ["electric"] = "VE",
        ["doors"] = "VT",
        ["superstructure"] = "VA"
    };

    /// <summary>
    /// Gerenderter Zellentext fuer eine VK-VA-Spalte (damit der Filter sinnvoll bleibt):
    /// AG nicht anwendbar -> "" (leere Zelle), sonst der Status-Text "offen"/"in bearbeitung"/"fertig".
    /// </summary>
    private static string FormatWorkStepForFilter(PickingLeitstandItem item, string code)
    {
        if (!item.WorkSteps.TryGetValue(code, out var cell)) return string.Empty;
        return cell.Status switch
        {
            FaWorkStepStatus.Fertig => "fertig",
            FaWorkStepStatus.InBearbeitung => "in bearbeitung",
            _ => "offen"
        };
    }

    private static bool MatchLeitstandWorkStepFilter(
        PickingLeitstandItem item, string code, List<string> tokens, bool negate)
    {
        var text = FormatWorkStepForFilter(item, code);
        var hasMatch = tokens.Any(t => text.Contains(t));
        return negate ? !hasMatch : hasMatch;
    }

    /// <summary>
    /// Matchcode-Spaltenfilter (Task 2, Artikelstamm): einfacher Contains-Match auf einem bereits
    /// aus Article befuellten Zeilenfeld — kein Datumsformat, deshalb kein Fall in
    /// <see cref="MatchLeitstandDateFilter"/>. Tokens sind bereits lowercase (ColumnFilterHelper.Parse).
    /// </summary>
    private static bool MatchLeitstandTextFilter(string? value, List<string> tokens, bool negate)
    {
        var text = (value ?? string.Empty).ToLowerInvariant();
        var hasMatch = tokens.Any(t => text.Contains(t));
        return negate ? !hasMatch : hasMatch;
    }

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
