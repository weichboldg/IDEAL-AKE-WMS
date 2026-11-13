using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

public class PickingController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderPickingStatusRepository _pickingStatusRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;
    private readonly IBomRepository _bomRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IPickingTransferService _pickingTransferService;
    private readonly IUserRepository _userRepository;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IPartRequisitionRepository _partRequisitionRepository;
    private readonly IArticleAttributeRepository _articleAttributeRepository;
    private readonly IServiceSettingRepository _serviceSettingRepository;
    private readonly ILogger<PickingController> _logger;
    private readonly IPickingOrderRepository _pickingOrderRepository;
    private readonly IPickingAreaRepository _pickingAreaRepository;
    private readonly PickingOrderWorkflow _pickingOrderWorkflow;

    public PickingController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IBomRepository bomRepository,
        IStockMovementRepository stockMovementRepository,
        IStorageLocationRepository storageLocationRepository,
        IArticleRepository articleRepository,
        IPickingTransferService pickingTransferService,
        IUserRepository userRepository,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IPartRequisitionRepository partRequisitionRepository,
        IArticleAttributeRepository articleAttributeRepository,
        IServiceSettingRepository serviceSettingRepository,
        IPickingOrderRepository pickingOrderRepository,
        IPickingAreaRepository pickingAreaRepository,
        PickingOrderWorkflow pickingOrderWorkflow,
        ILogger<PickingController> logger)
    {
        _pickingOrderRepository = pickingOrderRepository;
        _pickingAreaRepository = pickingAreaRepository;
        _pickingOrderWorkflow = pickingOrderWorkflow;
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _bomRepository = bomRepository;
        _stockMovementRepository = stockMovementRepository;
        _storageLocationRepository = storageLocationRepository;
        _articleRepository = articleRepository;
        _pickingTransferService = pickingTransferService;
        _userRepository = userRepository;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _partRequisitionRepository = partRequisitionRepository;
        _articleAttributeRepository = articleAttributeRepository;
        _serviceSettingRepository = serviceSettingRepository;
        _logger = logger;
    }

    // IDEAL FA-Liste-Hierarchie: Master-Zustand lesen (Muster wie ProductionOrdersController).
    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// v1.88.0 Auftragsliste: Server-Spaltenfilter Col-Key -> gerenderter Zell-Text (Termin im View-Format "dd.MM.yyyy KWxx" lowercase,
    /// "-" fuer fehlende Prio). Die Getter MUESSEN exakt das liefern, was die View rendert.
    /// </summary>
    private static readonly Dictionary<string, Func<PickingOrderListItem, string?>> ColumnMap = new()
    {
        ["priority"] = i => i.PickingPriority?.ToString() ?? "-",
        ["nummer"] = i => i.Nummer,
        ["order-number"] = i => i.OrderNumber,
        ["article-number"] = i => i.ArticleNumber,
        ["matchcode"] = i => i.Matchcode,   // v1.99.8
        ["description"] = i => i.Description1,
        ["workbench"] = i => i.WorkplaceName,
        ["bereich"] = i => i.Bereich,
        ["picking-date"] = i => FormatDateForFilter(i.Faelligkeit),
        ["status"] = i => PickingOrderListItem.StatusText(i.Status),
        ["picker"] = i => i.AssignedPickerName,
    };

    /// <summary>
    /// Formatiert ein Datum identisch zur View (<c>dd.MM.yyyy KWxx</c>) und lowercased,
    /// damit der Server denselben Text matched wie der clientseitige Filter.
    /// </summary>
    private static string FormatDateForFilter(DateTime? date)
    {
        if (!date.HasValue) return string.Empty;
        var d = date.Value;
        var kw = System.Globalization.ISOWeek.GetWeekOfYear(d);
        return $"{d:dd.MM.yyyy} KW{kw}".ToLowerInvariant();
    }

    /// <summary>
    /// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E2, AK 10): Kommissionierauftraege statt FAs. Standard: Status
    /// Offen/In Arbeit, FA nicht erledigt (IsDone/IsDonePicking) und nicht storniert; Filter Bereich, Status, „Nur meine“ (bei Zuweisung).
    /// Faelligkeit wie heute aus dem FA gerechnet (Kommissionier-Vorlauf), Bereich „Vorkommissionierung“ zusaetzlich mit dem
    /// Vorkommissionier-Vorlauf (Werkbank-Override, Antwort 8). Spaltenfilter VOR dem Paging, Datum nach der Terminberechnung.
    /// </summary>
    [RequirePickingAccess]
    public async Task<IActionResult> Index(bool showAll = false, int? bereichId = null, string? status = null, int page = 1, int? pageSize = null)
    {
        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!leitstandAktiv)
            return View("IndexDropdown");

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var statusFilter = status == "alle" ? "alle" : "offen";

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var auftraege = await _pickingOrderRepository.GetListeAsync(nurOffene: statusFilter == "offen");
        if (bereichId.HasValue) auftraege = auftraege.Where(o => o.PickingAreaId == bereichId).ToList();
        if (pickerAssignmentEnabled && !showAll)
        {
            var currentUserId = _currentUserService.GetCurrentAppUserId();
            auftraege = auftraege.Where(o => currentUserId.HasValue && o.AssignedPickerId == currentUserId).ToList();
        }

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();
        // v1.99.8: Matchcode des FA-Artikels, ein Batch-Lookup je Listenaufbau (wie FA-Liste)
        var matchcodes = await MatchcodeLookup.BuildAsync(_articleRepository, auftraege.Select(o => o.ProductionOrder?.ArticleNumber), _logger, "Kommissionierauftraege");

        var allItems = auftraege.Select(o =>
        {
            var fa = o.ProductionOrder;
            DateTime? faellig = null;
            if (fa?.ProductionDate is { } termin)
            {
                faellig = _businessDayService.SubtractBusinessDays(termin, kommissionierTage, holidays);
                if (o.PickingArea.IstVorkommissionierung)
                    faellig = _businessDayService.SubtractBusinessDays(faellig.Value,
                        PrePickingDaysResolver.Resolve(fa.ProductionWorkplace?.OverridePrePickingDays, vorkommissionierTage), holidays);
            }
            return new PickingOrderListItem
            {
                Id = o.Id, Nummer = o.Nummer, ProductionOrderId = o.ProductionOrderId, OrderNumber = fa?.OrderNumber,
                ArticleNumber = fa?.ArticleNumber, Matchcode = matchcodes.GetValueOrDefault(fa?.ArticleNumber ?? string.Empty),
                Description1 = fa?.Description1, WorkplaceName = fa?.ProductionWorkplace?.Name,
                Bereich = o.PickingArea.Name, PickingPriority = o.PickingPriority, Faelligkeit = faellig, Status = o.Status,
                AssignedPickerName = o.AssignedPickerName,
            };
        }).ToList();

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(allItems, columnFilters, ColumnMap, ColumnFilterTypes.For("Picking/Index")).ToList();
        if (page < 1) page = 1;

        return View(new PickingListViewModel
        {
            Items = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            ShowAllOrders = showAll,
            PickerAssignmentEnabled = pickerAssignmentEnabled,
            StatusFilter = statusFilter,
            BereichId = bereichId,
            Bereiche = await _pickingAreaRepository.GetAllAsync(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = filtered.Count
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> ToggleDone(int id, string? returnUrl)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        var status = await _pickingStatusRepository.GetByProductionOrderIdAsync(id);
        // IsDonePicking togglen (statt FA-Master-IsDone; Round-4-Korrektur Spec 10.4)
        await _pickingStatusRepository.SetIsDonePickingAsync(
            id, !(status?.IsDonePicking ?? false),
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E3, AK 15): Stueckliste des FA nur LESEND mit den Kommissionierauftraegen
    /// des FA als Links. Ohne Leitstand (Auswahlliste, Antwort 7) legt das Oeffnen die fehlenden Auftraege an.
    /// </summary>
    [RequirePickingAccess]
    public async Task<IActionResult> Bom(int id, string? filterText)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        // Kommissionierung nur am HauptFA (Spec 2026-09-25): Sub-FA-Stueckliste nicht interaktiv.
        if (order.IsSubFa)
        {
            TempData["WarningMessage"] = $"Kommissionierung erfolgt nur am HauptFA {order.OrderNumber}.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["WarningMessage"] = "Dieser Fertigungsauftrag hat keine Artikelnummer.";
            return RedirectToAction(nameof(Index));
        }

        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!leitstandAktiv)
        {
            var e = await _pickingOrderWorkflow.ErzeugeOffeneAsync(id);
            var (erfolg, warnung) = PickingOrderWorkflow.Sammelmeldung(new[] { (order.OrderNumber, e) });
            if (e.Nummern.Count > 0) TempData["SuccessMessage"] = erfolg;
            if (warnung != null) TempData["WarningMessage"] = warnung;
        }
        return await BomSeiteAsync(order, null, filterText);
    }

    /// <summary>v1.88.0 E3 (AK 11): Auftragsseite = Stueckliste im Auftragsmodus, nur die Zeilen dieses Auftrags.</summary>
    [RequirePickingAccess]
    public async Task<IActionResult> Auftrag(int id, string? filterText)
    {
        var auftrag = await _pickingOrderRepository.GetMitZeilenAsync(id);
        if (auftrag?.ProductionOrder == null) return NotFound();
        return await BomSeiteAsync(auftrag.ProductionOrder, auftrag, filterText);
    }

    private async Task<IActionResult> BomSeiteAsync(ProductionOrder order, PickingOrder? auftrag, string? filterText)
    {
        var id = order.Id;
        // User-Defaults für client-seitige Spaltenfilter laden
        string? defaultFilterBeschaffung = null;
        string? defaultFilterArtikelgruppe = null;
        string? defaultFilterBomDescription1 = null;
        string? defaultFilterBomKommissionierziel = null;
        var recursiveFilterSearch = false;
        var appUserId = _currentUserService.GetCurrentAppUserId();
        if (appUserId.HasValue)
        {
            var currentUser = await _userRepository.GetByIdAsync(appUserId.Value);
            if (currentUser != null)
            {
                defaultFilterBeschaffung = currentUser.DefaultFilterBeschaffung;
                defaultFilterArtikelgruppe = currentUser.DefaultFilterArtikelgruppe;
                defaultFilterBomDescription1 = currentUser.DefaultFilterBomDescription1;
                defaultFilterBomKommissionierziel = currentUser.DefaultFilterBomKommissionierziel;
                recursiveFilterSearch = currentUser.RecursiveFilterSearch;
            }
        }

        var bomResult = await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));
        var bomItems = bomResult.Items;

        // v1.88.0: Fortschritt kommt aus den Zeilen des Kommissionierauftrags (Schluessel Ressourcenummer + Position wie frueher PickingItem);
        // ohne Auftrag (FA-Stueckliste) nur lesend. Im Auftragsmodus erscheinen nur die Positionen dieses Auftrags.
        var zeileByKey = (auftrag?.Zeilen ?? new List<PickingOrderLine>())
            .GroupBy(z => PickingOrderRestmengen.Schluessel(z.ArticleNumber, z.BomPosition))
            .ToDictionary(g => g.Key, g => g.First());
        if (auftrag != null)
            bomItems = bomItems.Where(b => b.Ressourcenummer != null
                && zeileByKey.ContainsKey(PickingOrderRestmengen.Schluessel(b.Ressourcenummer, b.Position))).ToList();

        var articleNumbers = bomItems.Select(b => b.Ressourcenummer).Where(r => !string.IsNullOrEmpty(r)).Select(r => r!).Distinct().ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);
        var matchcodes = await MatchcodeLookup.BuildAsync(_articleRepository, articleNumbers, _logger, "Stueckliste");

        // Batch-load category names for BOM articles
        var categoryByArticle = await _articleAttributeRepository.GetCategoryNamesByArticleNumbersAsync(articleNumbers);

        var allStorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
        var targetStorageLocations = await _storageLocationRepository.GetActivePickingTransportLocationsAsync();

        // NAN-Lagerplatz als Default wenn kein buchbarer Bestand
        var nanLocation = await _storageLocationRepository.GetByCodeAsync("NAN");
        var nanLocationId = nanLocation?.Id;

        // Auto-Suggest darf nur buchbare (IstBuchbar=true), nicht-Wagen-Lagerplaetze
        // vorschlagen — sonst rendert das Dropdown "--" (Lager ist nicht in der Liste).
        var buchbarLocationIds = allStorageLocations.Select(sl => sl.Id).ToHashSet();

        // Baugruppen-Hierarchie: sammle alle Baugruppen-Werte
        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var viewItems = bomItems.Select(bom =>
        {
            PickingOrderLine? picking = null;
            if (bom.Ressourcenummer != null
                && zeileByKey.TryGetValue(PickingOrderRestmengen.Schluessel(bom.Ressourcenummer, bom.Position), out var found))
            {
                picking = found;
            }
            stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var stockLocations);
            var locations = stockLocations ?? new List<StockLocationInfo>();

            // TreeLevel aus Position ableiten: Anzahl Punkte = Ebene (z.B. "15" = 0, "15.1" = 1, "15.1.1" = 2)
            var treeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.');

            var suggestedLocationId = QuellVorschlag(locations, buchbarLocationIds, nanLocationId);

            // Gespeicherten SourceStorageLocationId nur uebernehmen, wenn er aktuell
            // buchbar ist — sonst fiele der Wert im Dropdown unsichtbar weg ("--").
            var savedSourceId = picking?.SourceStorageLocationId;
            if (savedSourceId.HasValue && !buchbarLocationIds.Contains(savedSourceId.Value))
            {
                savedSourceId = null;
            }
            var sourceLocationId = savedSourceId ?? suggestedLocationId;

            return new BomItemViewModel
            {
                Artikelnummer = bom.Artikelnummer,
                Position = bom.Position,
                Baugruppe = bom.Baugruppe,
                Ressourcenummer = bom.Ressourcenummer,
                Bezeichnung1 = bom.Bezeichnung1,
                Bezeichnung2 = bom.Bezeichnung2,
                Matchcode = (bom as FaHierarchyBomItem)?.Matchcode ?? matchcodes.GetValueOrDefault(bom.Ressourcenummer ?? ""),
                // Auftragsmodus: Soll der Zeile (Folgeauftrag = Rest), sonst Stuecklistenmenge
                Menge = picking?.SollMenge ?? BomQuantityResolver.Resolve(bomResult, bom, order),
                Beschaffungsartikel = bom.Beschaffungsartikel,
                Artikelgruppe = bom.Artikelgruppe,
                KategorieName = categoryByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var catName) ? catName : null,
                StockLocations = locations,
                TreeLevel = treeLevel,
                IsBaugruppe = bom is FaHierarchyBomItem fh
                    ? fh.IsBaugruppe
                    : !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer),
                Kommissionieren = (bom as FaHierarchyBomItem)?.Kommissionieren,
                Hauptlagerplatz = (bom as FaHierarchyBomItem)?.Hauptlagerplatz,
                Ebene = (bom as FaHierarchyBomItem)?.Ebene ?? 0,
                VaterSubFa = (bom as FaHierarchyBomItem)?.VaterSubFa,
                SagePosition = (bom as FaHierarchyBomItem)?.SagePosition,
                IsWaise = (bom as FaHierarchyBomItem)?.IsWaise ?? false,
                PositionsKollision = (bom as FaHierarchyBomItem)?.PositionsKollision ?? false,
                PickingItemId = picking?.Id,
                IsPicked = picking?.Status == PickingOrderLineStatus.Gepickt,
                SourceStorageLocationId = sourceLocationId,
                SuggestedSourceStorageLocationId = suggestedLocationId,
                // Umgebucht, entfallen oder in den Folgeauftrag uebertragen: nicht mehr bearbeitbar
                IsTransferred = picking != null && picking.Status is not (PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt),
                IstMenge = picking?.IstMenge,
                ZeilenStatus = picking?.Status
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
            SubOrderNumber = order.SubOrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Items = viewItems,
            FilterText = filterText,
            DefaultFilterBeschaffung = defaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = defaultFilterArtikelgruppe,
            DefaultFilterBomDescription1 = defaultFilterBomDescription1,
            DefaultFilterBomKommissionierziel = defaultFilterBomKommissionierziel,
            AllStorageLocations = allStorageLocations,
            TargetStorageLocations = targetStorageLocations,
            DataSource = bomResult.DataSource,
            RecursiveFilterSearch = recursiveFilterSearch,
            Hierarchical = bomResult.DataSource == BomDataSources.FaHierarchie,
            FullStructure = bomResult.DataSource == BomDataSources.FaHierarchie && BomScopes.ForOrder(order) == BomScope.FullStructure,
            // v1.88.0: interaktiv nur im Auftragsmodus mit offenem/in Arbeit befindlichem Auftrag; FA-Stueckliste lesend + Auftragsliste
            ReadOnly = auftrag == null || auftrag.Status is not (PickingOrderStatus.Offen or PickingOrderStatus.InArbeit),
            BackController = "Picking",
            BackText = "Zurück zur Kommissionierung",
            Auftrag = auftrag,
            FaAuftraege = auftrag == null ? await _pickingOrderRepository.GetByProductionOrderAsync(id) : new List<PickingOrder>(),
        };

        // Bedarfsmeldungen: Feature-Toggle + offene Meldungen laden
        var bestellungenAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.BestellungenAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.BestellungenAktiv = bestellungenAktiv;
        var openRequisitions = bestellungenAktiv
            ? await _partRequisitionRepository.GetByProductionOrderAsync(id)
            : new List<PartRequisition>();
        ViewBag.OpenRequisitions = openRequisitions;

        // Lagerbestellung-Button (v1.25.0): Master-Schalter (Default true: nur "false" sperrt)
        // + Order-Recht (Lager ODER Glas). Unabhaengig von Bedarfsmeldungen (BestellungenAktiv).
        var lagerbestellungAktivRaw = await _settingRepository.GetValueAsync(AppSettingKeys.LagerbestellungAktiv);
        ViewBag.LagerbestellungAktiv = !string.Equals(lagerbestellungAktivRaw, "false", StringComparison.OrdinalIgnoreCase);
        ViewBag.CanOrderWarehouse = await _currentUserService.CanOrderLagerAsync()
            || await _currentUserService.CanOrderGlasAsync();

        // QA Q1: auch die Action „Auftrag“ rendert die Stuecklisten-View (sonst sucht MVC „Auftrag.cshtml“ → HTTP 500).
        return View("Bom", vm);
    }

    /// <summary>v1.88.0 E3 (AK 11, 12): gewaehlte Zeilen des Auftrags mit Ist-Menge umbuchen (Server prueft Menge, Status, Bestand).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> AuftragUmbuchen(int pickingOrderId, int targetStorageLocationId, bool forceTransfer = false,
        [FromForm] List<PickingSelectionItem>? items = null)
    {
        try
        {
            var result = await _pickingTransferService.TransferOrderLinesAsync(
                pickingOrderId, targetStorageLocationId, forceTransfer, items ?? new(),
                _currentUserService.GetCurrentAppUserId(), _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

            if (result.IsPickingTransportConflict)
            {
                return Ok(new
                {
                    conflict = true,
                    locationId = result.ConflictStorageLocationId,
                    locationCode = result.ConflictStorageLocationCode,
                    currentWaNumbers = result.CurrentWaNumbers,
                    newWaNumber = result.NewWaNumber
                });
            }
            return Ok(new { conflict = false, count = result.TransferredCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>v1.88.0 E3 (AK 13, 14): Abschluss nach der Umbuchung – „vollständig“ oder „teilweise“ (Rest in Folgeauftrag).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> AuftragAbschliessen(int pickingOrderId, bool vollstaendig)
    {
        var r = await _pickingOrderWorkflow.AbschliessenAsync(pickingOrderId, vollstaendig);
        if (r.Fehler != null) return BadRequest(r.Fehler);
        var text = vollstaendig || r.Nummern.Count == 0 ? "Auftrag abgeschlossen." : $"Auftrag teilweise abgeschlossen, Rest im Folgeauftrag {string.Join(", ", r.Nummern)}.";
        if (r.FaAbgeschlossen) text += " Der FA ist vollständig kommissioniert.";
        TempData["SuccessMessage"] = text;
        if (r.Meldungen.Count > 0) TempData["WarningMessage"] = string.Join(" ", r.Meldungen);
        return Ok(new { folge = r.Nummern, faAbgeschlossen = r.FaAbgeschlossen });
    }

    // PrintBom auch fuer vorbau + fa_completion: der Druck-Button bleibt in der
    // read-only Stueckliste der FA-Abarbeitungsliste (vorbau) sowie der
    // FA-Vervollstaendigung (fa_completion) sichtbar (v1.22.0, Spec §7 "Druck bleibt").
    //
    // Hotfix v1.30.1 (HTTP 404.15): die Liste der sichtbaren Positionen kommt per POST im Formular-Body —
    // im GET-Query sprengte sie bei grossen Stuecklisten maxQueryString. GET bleibt fuer Lesezeichen/
    // Alt-Links und druckt dann immer ALLE Positionen (eine gefilterte GET-Variante gibt es nicht mehr).
    [HttpGet]
    [RequirePickingOrVorbauOrFaCompletionAccess]
    public Task<IActionResult> PrintBom(int id, string? filterInfo, string? visibleColumns)
        => RenderPrintBomAsync(id, visiblePositions: null, filterInfo, visibleColumns);

    [HttpPost, ValidateAntiForgeryToken, ActionName("PrintBom")]
    [RequirePickingOrVorbauOrFaCompletionAccess]
    public Task<IActionResult> PrintBomPost(int id,
        [FromForm] string? visiblePositions, [FromForm] string? filterInfo, [FromForm] string? visibleColumns)
        => RenderPrintBomAsync(id, visiblePositions, filterInfo, visibleColumns);

    private async Task<IActionResult> RenderPrintBomAsync(int id, string? visiblePositions, string? filterInfo, string? visibleColumns)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (string.IsNullOrEmpty(order.ArticleNumber))
            return NotFound();

        var bomResult = await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));
        var bomItems = bomResult.Items;

        // Stock-Daten laden für Lagerplatz-Anzeige
        var articleNumbers = bomItems
            .Select(b => b.Ressourcenummer)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => r!)
            .Distinct()
            .ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);
        var matchcodes = await MatchcodeLookup.BuildAsync(_articleRepository, articleNumbers, _logger, "Stueckliste-Druck");

        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Sichtbare Positionen in Anzeige-Reihenfolge (Filterung + Baumstruktur + Sortierung der
        // interaktiven Ansicht). Hotfix v1.30.3: die Reihenfolge der Liste IST die Sortierung des Drucks —
        // vorher war die Liste nur eine Menge und der Druck immer nach Position sortiert.
        Dictionary<string, int>? visibleOrder = null;
        if (!string.IsNullOrEmpty(visiblePositions))
        {
            visibleOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var p in visiblePositions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                visibleOrder.TryAdd(p.Trim(), visibleOrder.Count);
        }

        var allItems = bomItems.Select(bom => new PrintBomItem
        {
            Position = bom.Position,
            Baugruppe = bom.Baugruppe,
            Ressourcenummer = bom.Ressourcenummer,
            Bezeichnung1 = bom.Bezeichnung1,
            Bezeichnung2 = bom.Bezeichnung2,
            Matchcode = (bom as FaHierarchyBomItem)?.Matchcode ?? matchcodes.GetValueOrDefault(bom.Ressourcenummer ?? ""),
            Menge = BomQuantityResolver.Resolve(bomResult, bom, order),
            Beschaffungsartikel = bom.Beschaffungsartikel,
            Artikelgruppe = bom.Artikelgruppe,
            TreeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.'),
            IsBaugruppe = bom is FaHierarchyBomItem fh
                ? fh.IsBaugruppe
                : (!string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer)),
            Lagerplatz = stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var locs) && locs.Any()
                ? string.Join(", ", locs.Select(sl => $"{sl.Code} ({sl.Quantity:N3})"))
                : null,
            Kommissionieren = (bom as FaHierarchyBomItem)?.Kommissionieren,
            Hauptlagerplatz = (bom as FaHierarchyBomItem)?.Hauptlagerplatz,
            Ebene = (bom as FaHierarchyBomItem)?.Ebene ?? 0
        })
        .OrderBy(v => v.Position, new NaturalPositionComparer())
        .ToList();

        // Auf sichtbare Positionen einschränken und in deren Reihenfolge bringen (falls aus dem Client übergeben)
        var items = visibleOrder != null
            ? allItems.Where(i => !string.IsNullOrEmpty(i.Position) && visibleOrder.ContainsKey(i.Position))
                .OrderBy(i => visibleOrder[i.Position!])
                .ToList()
            : allItems;

        var vm = new PrintBomViewModel
        {
            OrderNumber = order.OrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Quantity = order.Quantity,
            ProductionDate = order.ProductionDate,
            PrintedBy = _currentUserService.GetDisplayName(),
            Items = items,
            FilterInfo = filterInfo,
            VisibleColumns = string.IsNullOrEmpty(visibleColumns)
                ? new List<string>()
                : visibleColumns.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };

        return View("PrintBom", vm);
    }

    /// <summary>
    /// v1.88.0 (E4, AK 18): Druck je Kommissionierauftrag – Kopf mit Nummer, FA, Bereich, Faelligkeit und Barcode der Auftragsnummer
    /// (<see cref="IBarcodeService"/> wie die Kommissionierlisten), Zeilen mit Soll/Ist/Lagerplatz. Ersetzt den FA-Druck aus PickingItems.
    /// </summary>
    [HttpGet]
    [RequirePickingAccess]
    public Task<IActionResult> AuftragDrucken(int id, [FromServices] IBarcodeService barcode)
        => RenderAuftragDruckenAsync(id, reihenfolge: null, barcode);

    /// <summary>v1.97.0: wie <see cref="AuftragDrucken"/>, aber in der Reihenfolge der sortierten Ansicht (Positionen per POST wie beim
    /// Stuecklisten-Druck, Hotfix v1.30.1/v1.30.3). Es werden immer alle Zeilen des Auftrags gedruckt; Zeilen ohne Position in der Liste stehen
    /// am Ende in Positions-Reihenfolge.</summary>
    [HttpPost, ValidateAntiForgeryToken, ActionName("AuftragDrucken")]
    [RequirePickingAccess]
    public Task<IActionResult> AuftragDruckenPost(int id, [FromForm] string? reihenfolge, [FromServices] IBarcodeService barcode)
        => RenderAuftragDruckenAsync(id, reihenfolge, barcode);

    private async Task<IActionResult> RenderAuftragDruckenAsync(int id, string? reihenfolge, IBarcodeService barcode)
    {
        var auftrag = await _pickingOrderRepository.GetMitZeilenAsync(id);
        if (auftrag?.ProductionOrder == null) return NotFound();
        var order = auftrag.ProductionOrder;
        var bomItems = (await _bomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order))).Items;
        var bezeichnung = bomItems.Where(b => b.Ressourcenummer != null)
            .GroupBy(b => b.Ressourcenummer!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Bezeichnung1, StringComparer.OrdinalIgnoreCase);
        var plaetze = (await _storageLocationRepository.GetAllAsync()).ToDictionary(l => l.Id, l => l.Code);
        // v1.99.13 (Punkt 4a): offene/gepickte Zeilen ohne Quell-Platz drucken den Vorschlag der Auftragsseite (gleiche Regel, gleiche Daten).
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(
            auftrag.Zeilen.Select(z => z.ArticleNumber).Distinct().ToList());
        var buchbarIds = (await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync()).Select(sl => sl.Id).ToHashSet();
        var nanId = (await _storageLocationRepository.GetByCodeAsync("NAN"))?.Id;
        int? QuellPlatz(PickingOrderLine z) => z.SourceStorageLocationId
            ?? (z.Status is PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt
                ? QuellVorschlag(stockByArticle.GetValueOrDefault(z.ArticleNumber) ?? new List<StockLocationInfo>(), buchbarIds, nanId)
                : null);

        DateTime? faellig = null;
        if (order.ProductionDate is { } termin)
        {
            var holidays = await _holidayRepository.GetHolidayDatesAsync();
            faellig = _businessDayService.SubtractBusinessDays(termin, await _settingRepository.GetIntValueAsync("KommissionierTage", 4), holidays);
            if (auftrag.PickingArea.IstVorkommissionierung)
                faellig = _businessDayService.SubtractBusinessDays(faellig.Value, PrePickingDaysResolver.Resolve(
                    order.ProductionWorkplace?.OverridePrePickingDays, await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1)), holidays);
        }

        return View("PrintPicking", new PrintPickingViewModel
        {
            AuftragNummer = auftrag.Nummer,
            AuftragBarcode = barcode.EncodeCode128Base64(auftrag.Nummer),
            Bereich = auftrag.PickingArea.Name,
            Status = PickingOrderListItem.StatusText(auftrag.Status),
            Faelligkeit = faellig,
            OrderNumber = order.OrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Quantity = order.Quantity,
            ProductionDate = order.ProductionDate,
            PrintedBy = _currentUserService.GetDisplayName(),
            PickedBy = auftrag.AssignedPickerName,
            Matchcode = (await MatchcodeLookup.BuildAsync(_articleRepository, new[] { order.ArticleNumber }, _logger, "Auftrag-Druck"))
                .GetValueOrDefault(order.ArticleNumber ?? string.Empty),   // v1.99.8
            Items = SortiereZeilen(auftrag.Zeilen, reihenfolge).Select(z => new PrintPickingItem
            {
                Artikelnummer = z.ArticleNumber,
                Position = z.BomPosition,
                Bezeichnung1 = bezeichnung.GetValueOrDefault(z.ArticleNumber),
                Menge = z.SollMenge,
                IstMenge = z.IstMenge,
                Lagerplatz = QuellPlatz(z) is { } sl ? plaetze.GetValueOrDefault(sl) : null,
                Status = PickingOrderListItem.ZeilenStatusText(z.Status),
            }).ToList()
        });
    }

    /// <summary>Vorgeschlagener Quell-Lagerplatz (Auftragsseite und Auftragsdruck, v1.99.13): Hauptlagerplatz mit buchbarem Bestand > 0,
    /// sonst buchbarer Platz mit der hoechsten Menge, sonst NAN. Nur buchbare Plaetze — Sage-synchronisierte sind standardmaessig
    /// IstBuchbar=false und fehlen im Dropdown; als Vorschlag blieb das Select sonst leer („--“).</summary>
    internal static int? QuellVorschlag(IEnumerable<StockLocationInfo> locations, ISet<int> buchbarIds, int? nanId)
    {
        var buchbar = locations.Where(sl => sl.Quantity > 0 && buchbarIds.Contains(sl.StorageLocationId)).ToList();
        return (buchbar.FirstOrDefault(sl => sl.IsPrimaryStorageLocation)
                ?? buchbar.OrderByDescending(sl => sl.Quantity).FirstOrDefault())?.StorageLocationId
            ?? nanId;
    }

    /// <summary>v1.97.0: Zeilen in der Reihenfolge der Positionsliste (Anzeige-Reihenfolge), ohne Liste nach Position.</summary>
    internal static IEnumerable<PickingOrderLine> SortiereZeilen(IEnumerable<PickingOrderLine> zeilen, string? reihenfolge)
    {
        var nachPosition = zeilen.OrderBy(z => z.BomPosition, new NaturalPositionComparer());
        if (string.IsNullOrEmpty(reihenfolge)) return nachPosition;
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var p in reihenfolge.Split(',', StringSplitOptions.RemoveEmptyEntries))
            index.TryAdd(p.Trim(), index.Count);
        // OrderBy ist stabil: Zeilen ohne Listeneintrag behalten ihre Positions-Reihenfolge am Ende.
        return nachPosition.OrderBy(z => z.BomPosition != null && index.TryGetValue(z.BomPosition, out var i) ? i : int.MaxValue);
    }
}
