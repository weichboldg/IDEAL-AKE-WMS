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
    private readonly IPickingRepository _pickingRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IPickingTransferService _pickingTransferService;
    private readonly IUserRepository _userRepository;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly IPartRequisitionRepository _partRequisitionRepository;
    private readonly IArticleAttributeRepository _articleAttributeRepository;

    public PickingController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderPickingStatusRepository pickingStatusRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService,
        IBomRepository bomRepository,
        IPickingRepository pickingRepository,
        IStockMovementRepository stockMovementRepository,
        IStorageLocationRepository storageLocationRepository,
        IArticleRepository articleRepository,
        IPickingTransferService pickingTransferService,
        IUserRepository userRepository,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        IPartRequisitionRepository partRequisitionRepository,
        IArticleAttributeRepository articleAttributeRepository)
    {
        _productionOrderRepository = productionOrderRepository;
        _pickingStatusRepository = pickingStatusRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
        _bomRepository = bomRepository;
        _pickingRepository = pickingRepository;
        _stockMovementRepository = stockMovementRepository;
        _storageLocationRepository = storageLocationRepository;
        _articleRepository = articleRepository;
        _pickingTransferService = pickingTransferService;
        _userRepository = userRepository;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _partRequisitionRepository = partRequisitionRepository;
        _articleAttributeRepository = articleAttributeRepository;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Termin im View-Format "dd.MM.yyyy KWxx" lowercase, "Offen" fuer leeren Status, "-" fuer fehlende Prio).
    /// </summary>
    private static readonly Dictionary<string, Func<PickingListItem, string?>> ColumnMap = new()
    {
        ["priority"] = i => i.PickingPriority?.ToString() ?? "-",
        ["order-number"] = i => i.OrderNumber,
        ["article-number"] = i => i.ArticleNumber,
        ["description"] = i => i.Description1,
        ["customer"] = i => i.Customer,
        ["quantity"] = i => i.Quantity.ToString("N0"),
        ["picking-date"] = i => FormatDateForFilter(i.KommissionierTermin),
        ["status"] = i => string.IsNullOrEmpty(i.PickingStatus) ? "Offen" : i.PickingStatus,
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

    [RequirePickingAccess]
    public async Task<IActionResult> Index(bool showAll = false, int page = 1, int? pageSize = null)
    {
        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!leitstandAktiv)
            return View("IndexDropdown");

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        List<ProductionOrder> releasedOrders;
        if (pickerAssignmentEnabled && !showAll)
        {
            var currentUserId = _currentUserService.GetCurrentAppUserId();
            releasedOrders = currentUserId.HasValue
                ? await _pickingStatusRepository.GetReleasedForPickingByPickerAsync(currentUserId.Value)
                : new List<ProductionOrder>();
        }
        else
        {
            releasedOrders = await _pickingStatusRepository.GetReleasedForPickingAsync();
        }

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        // Termin-Berechnung fuer ALLE freigegebenen FAs (nicht nur die aktuelle Seite):
        // der Termin-Spaltenfilter (dd.MM.yyyy KWxx) muss ueber alle Eintraege wirken.
        // Reine In-Memory-Datumsmathematik, unkritisch fuer die Performance.
        var allItems = releasedOrders.Select(o =>
        {
            var ps = o.PickingStatus; // Nav-Property (Include() im Repo)
            var item = new PickingListItem
            {
                Id = o.Id,
                PickingPriority = ps?.PickingPriority,
                OrderNumber = o.OrderNumber,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Customer = o.Customer,
                Quantity = o.Quantity,
                ProductionDate = o.ProductionDate,
                PickingStatus = ps?.PickingStatus,
                AssignedPickerId = ps?.AssignedPickerId,
                AssignedPickerName = ps?.AssignedPickerName
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
            }

            return item;
        }).ToList();

        // Server-Side-Spaltenfilter VOR Pagination: filtern -> zaehlen -> Skip/Take.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(allItems, columnFilters, ColumnMap).ToList();
        var totalCount = filtered.Count;
        if (page < 1) page = 1;
        var items = filtered
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToList();

        return View(new PickingListViewModel
        {
            Items = items,
            ShowAllOrders = showAll,
            PickerAssignmentEnabled = pickerAssignmentEnabled,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
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

    [RequirePickingAccess]
    public async Task<IActionResult> Bom(int id, string? filterText)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["WarningMessage"] = "Dieser Fertigungsauftrag hat keine Artikelnummer.";
            return RedirectToAction(nameof(Index));
        }

        // User-Defaults für client-seitige Spaltenfilter laden
        string? defaultFilterBeschaffung = null;
        string? defaultFilterArtikelgruppe = null;
        var recursiveFilterSearch = false;
        var appUserId = _currentUserService.GetCurrentAppUserId();
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

        await _pickingRepository.InitializePickingAsync(
            id, bomItems,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        var pickingItems = await _pickingRepository.GetByProductionOrderAsync(id);

        // O(1) Lookup statt O(P) FirstOrDefault pro BOM-Item
        var pickingByKey = pickingItems
            .Where(p => p.BomArticleNumber != null && p.BomPosition != null)
            .GroupBy(p => (p.BomArticleNumber!, p.BomPosition!))
            .ToDictionary(g => g.Key, g => g.First());

        var articleNumbers = bomItems.Select(b => b.Ressourcenummer).Where(r => !string.IsNullOrEmpty(r)).Select(r => r!).Distinct().ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);

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
            PickingItem? picking = null;
            if (bom.Ressourcenummer != null && bom.Position != null
                && pickingByKey.TryGetValue((bom.Ressourcenummer, bom.Position), out var found))
            {
                picking = found;
            }
            stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var stockLocations);
            var locations = stockLocations ?? new List<StockLocationInfo>();

            // TreeLevel aus Position ableiten: Anzahl Punkte = Ebene (z.B. "15" = 0, "15.1" = 1, "15.1.1" = 2)
            var treeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.');

            // Auto-Suggest: buchbarer Lagerplatz mit hoechster Menge, sonst NAN.
            // Sage-synchronisierte Lagerplaetze sind by default IstBuchbar=false
            // und tauchen deshalb nicht im Dropdown auf — wuerden sie hier als
            // Suggestion gewinnen, blieb das Select leer ("--").
            int? suggestedLocationId = null;
            var buchbarStock = locations
                .Where(sl => sl.Quantity > 0 && buchbarLocationIds.Contains(sl.StorageLocationId))
                .ToList();
            if (buchbarStock.Count > 0)
            {
                suggestedLocationId = buchbarStock.OrderByDescending(sl => sl.Quantity).First().StorageLocationId;
            }
            else if (nanLocationId.HasValue)
            {
                suggestedLocationId = nanLocationId;
            }

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
                Menge = bom.Menge * order.Quantity,
                Beschaffungsartikel = bom.Beschaffungsartikel,
                Artikelgruppe = bom.Artikelgruppe,
                KategorieName = categoryByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var catName) ? catName : null,
                StockLocations = locations,
                TreeLevel = treeLevel,
                IsBaugruppe = !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer),
                PickingItemId = picking?.Id,
                IsPicked = picking?.IsPicked ?? false,
                SourceStorageLocationId = sourceLocationId,
                SuggestedSourceStorageLocationId = suggestedLocationId,
                IsTransferred = picking?.IsTransferred ?? false
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
            AllStorageLocations = allStorageLocations,
            TargetStorageLocations = targetStorageLocations,
            DataSource = bomResult.DataSource,
            RecursiveFilterSearch = recursiveFilterSearch
        };

        // Bedarfsmeldungen: Feature-Toggle + offene Meldungen laden
        var bestellungenAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.BestellungenAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.BestellungenAktiv = bestellungenAktiv;
        var openRequisitions = bestellungenAktiv
            ? await _partRequisitionRepository.GetByProductionOrderAsync(id)
            : new List<PartRequisition>();
        ViewBag.OpenRequisitions = openRequisitions;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> TogglePicked(int pickingItemId, int? storageLocationId, bool isBaugruppe = false)
    {
        await _pickingRepository.TogglePickedAsync(
            pickingItemId,
            storageLocationId,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName(),
            isBaugruppe);

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> TransferPicked(int productionOrderId, int targetStorageLocationId, bool forceTransfer = false, [FromForm] List<PickingSelectionItem>? items = null)
    {
        try
        {
            var result = await _pickingTransferService.CheckAndTransferPickedItemsAsync(
                productionOrderId,
                targetStorageLocationId,
                forceTransfer,
                items,
                _currentUserService.GetCurrentAppUserId(),
                _currentUserService.GetDisplayName(),
                _currentUserService.GetWindowsUserName());

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePickingAccess]
    public async Task<IActionResult> SetPickingStatus(int productionOrderId, string status)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        await _pickingStatusRepository.SetPickingStatusTextAsync(
            productionOrderId, status,
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        // Kommissionierung abgeschlossen → IsDonePicking auf PickingStatus setzen
        // (NICHT mehr order.IsDone — das bleibt Sage-Master-only. Round-4-Korrektur Spec 10.4)
        if (status == "abgeschlossen")
        {
            await _pickingStatusRepository.SetIsDonePickingAsync(
                productionOrderId, true,
                _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());
        }

        return Ok();
    }

    // PrintBom auch fuer vorbau: der Druck-Button bleibt in der read-only
    // Stueckliste der FA-Abarbeitungsliste sichtbar (v1.22.0, Spec §7 "Druck bleibt").
    [RequirePickingOrVorbauAccess]
    public async Task<IActionResult> PrintBom(int id, string? visiblePositions, string? filterInfo, string? visibleColumns)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (string.IsNullOrEmpty(order.ArticleNumber))
            return NotFound();

        var bomItems = (await _bomRepository.GetBomItemsAsync(order.ArticleNumber)).Items;

        // Stock-Daten laden für Lagerplatz-Anzeige
        var articleNumbers = bomItems
            .Select(b => b.Ressourcenummer)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => r!)
            .Distinct()
            .ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);

        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Sichtbare Positionen filtern (Filterung + Baumstruktur aus der interaktiven Ansicht)
        HashSet<string>? visibleSet = null;
        if (!string.IsNullOrEmpty(visiblePositions))
        {
            visibleSet = visiblePositions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToHashSet(StringComparer.Ordinal);
        }

        var allItems = bomItems.Select(bom => new PrintBomItem
        {
            Position = bom.Position,
            Baugruppe = bom.Baugruppe,
            Ressourcenummer = bom.Ressourcenummer,
            Bezeichnung1 = bom.Bezeichnung1,
            Bezeichnung2 = bom.Bezeichnung2,
            Menge = bom.Menge * order.Quantity,
            Beschaffungsartikel = bom.Beschaffungsartikel,
            Artikelgruppe = bom.Artikelgruppe,
            TreeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.'),
            IsBaugruppe = !string.IsNullOrEmpty(bom.Ressourcenummer) && baugruppen.Contains(bom.Ressourcenummer),
            Lagerplatz = stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var locs) && locs.Any()
                ? string.Join(", ", locs.Select(sl => $"{sl.Code} ({sl.Quantity:N3})"))
                : null
        })
        .OrderBy(v => v.Position, new NaturalPositionComparer())
        .ToList();

        // Auf sichtbare Positionen einschränken (falls aus dem Client übergeben)
        var items = visibleSet != null
            ? allItems.Where(i => !string.IsNullOrEmpty(i.Position) && visibleSet.Contains(i.Position)).ToList()
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

        return View(vm);
    }

    [RequirePickingAccess]
    public async Task<IActionResult> PrintPicking(int id)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        var pickingItems = await _pickingRepository.GetByProductionOrderAsync(id);
        var pickedItems = pickingItems.Where(p => p.IsPicked).ToList();

        var vm = new PrintPickingViewModel
        {
            OrderNumber = order.OrderNumber,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Quantity = order.Quantity,
            ProductionDate = order.ProductionDate,
            PrintedBy = _currentUserService.GetDisplayName(),
            PickedBy = pickedItems.FirstOrDefault()?.PickedBy,
            Items = pickedItems.Select(p => new PrintPickingItem
            {
                Artikelnummer = p.BomArticleNumber,
                Menge = p.Quantity
            }).ToList()
        };

        if (!string.IsNullOrEmpty(order.ArticleNumber))
        {
            var bomItems = (await _bomRepository.GetBomItemsAsync(order.ArticleNumber)).Items;
            foreach (var item in vm.Items)
            {
                var bom = bomItems.FirstOrDefault(b => b.Ressourcenummer == item.Artikelnummer);
                if (bom != null)
                    item.Bezeichnung1 = bom.Bezeichnung1;
            }
        }

        return View(vm);
    }
}
