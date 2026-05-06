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

    [RequirePickingAccess]
    public async Task<IActionResult> Index(bool showAll = false)
    {
        var leitstandAktiv = (await _settingRepository.GetValueAsync(AppSettingKeys.LeitstandAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!leitstandAktiv)
            return View("IndexDropdown");

        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        List<ProductionOrder> releasedOrders;
        if (pickerAssignmentEnabled && !showAll)
        {
            var currentUserId = _currentUserService.GetCurrentAppUserId();
            releasedOrders = currentUserId.HasValue
                ? await _productionOrderRepository.GetReleasedForPickingByPickerAsync(currentUserId.Value)
                : new List<ProductionOrder>();
        }
        else
        {
            releasedOrders = await _productionOrderRepository.GetReleasedForPickingAsync();
        }

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var items = releasedOrders.Select(o =>
        {
            var item = new PickingListItem
            {
                Id = o.Id,
                PickingPriority = o.PickingPriority,
                OrderNumber = o.OrderNumber,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Customer = o.Customer,
                Quantity = o.Quantity,
                ProductionDate = o.ProductionDate,
                PickingStatus = o.PickingStatus,
                AssignedPickerId = o.AssignedPickerId,
                AssignedPickerName = o.AssignedPickerName
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
            }

            return item;
        }).ToList();

        return View(new PickingListViewModel
        {
            Items = items,
            ShowAllOrders = showAll,
            PickerAssignmentEnabled = pickerAssignmentEnabled
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

        order.IsDone = !order.IsDone;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

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

        var articleNumbers = bomItems.Select(b => b.Ressourcenummer).Where(r => !string.IsNullOrEmpty(r)).Select(r => r!).Distinct().ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);

        // Batch-load category names for BOM articles
        var categoryByArticle = await _articleAttributeRepository.GetCategoryNamesByArticleNumbersAsync(articleNumbers);

        var allStorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
        var targetStorageLocations = await _storageLocationRepository.GetActivePickingTransportLocationsAsync();

        // NAN-Lagerplatz als Default wenn kein Bestand
        var nanLocation = await _storageLocationRepository.GetByCodeAsync("NAN");
        var nanLocationId = nanLocation?.Id;

        // Baugruppen-Hierarchie: sammle alle Baugruppen-Werte
        var baugruppen = bomItems
            .Where(b => !string.IsNullOrEmpty(b.Baugruppe))
            .Select(b => b.Baugruppe!)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var viewItems = bomItems.Select(bom =>
        {
            var picking = pickingItems.FirstOrDefault(p => p.BomArticleNumber == bom.Ressourcenummer && p.BomPosition == bom.Position);
            stockByArticle.TryGetValue(bom.Ressourcenummer ?? "", out var stockLocations);
            var locations = stockLocations ?? new List<StockLocationInfo>();

            // TreeLevel aus Position ableiten: Anzahl Punkte = Ebene (z.B. "15" = 0, "15.1" = 1, "15.1.1" = 2)
            var treeLevel = string.IsNullOrEmpty(bom.Position) ? 0 : bom.Position.Count(c => c == '.');

            // Auto-Suggest: Lagerplatz mit höchster Menge, oder NAN als Default
            int? suggestedLocationId = null;
            if (locations.Any(sl => sl.Quantity > 0))
            {
                suggestedLocationId = locations.OrderByDescending(sl => sl.Quantity).First().StorageLocationId;
            }
            else if (nanLocationId.HasValue)
            {
                suggestedLocationId = nanLocationId;
            }

            // Wenn PickingItem schon einen SourceStorageLocationId hat, diesen verwenden
            var sourceLocationId = picking?.SourceStorageLocationId ?? suggestedLocationId;

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

        order.PickingStatus = status;

        // Kommissionierung abgeschlossen → FA automatisch erledigt setzen
        if (status == "abgeschlossen")
            order.IsDone = true;

        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

        return Ok();
    }

    [RequirePickingAccess]
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
