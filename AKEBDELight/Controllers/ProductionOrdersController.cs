using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class ProductionOrdersController : Controller
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
    private readonly IWebHostEnvironment _env;

    public ProductionOrdersController(
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
        IWebHostEnvironment env)
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
        _env = env;
    }

    public IActionResult Picking()
    {
        return View();
    }

    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false)
    {
        var orders = await _productionOrderRepository.GetAllOrderedAsync();

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
        {
            orders = orders.Where(o => o.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
        {
            orders = orders.Where(o => o.ArticleNumber != null && o.ArticleNumber.Contains(filterArticleNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterCustomer))
        {
            orders = orders.Where(o => o.Customer != null && o.Customer.Contains(filterCustomer, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!showDone)
        {
            orders = orders.Where(o => !o.IsDone).ToList();
        }

        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        var viewItems = orders.Select(o =>
        {
            var item = new ProductionOrderViewItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Quantity = o.Quantity,
                Customer = o.Customer,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Description2 = o.Description2,
                ProductionDate = o.ProductionDate,
                DeliveryDate = o.DeliveryDate,
                IsDone = o.IsDone,
                PickingStatus = o.PickingStatus
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, vorkommissionierTage, holidays);
                item.BeschichtungTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, beschichtungTage, holidays);
            }

            return item;
        }).ToList();

        var vm = new ProductionOrderViewModel
        {
            Items = viewItems,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
            KommissionierTage = kommissionierTage,
            VorkommissionierTage = vorkommissionierTage,
            BeschichtungTage = beschichtungTage
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Bom(int id, string? filterText)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        if (string.IsNullOrEmpty(order.ArticleNumber))
        {
            TempData["SuccessMessage"] = "Dieser Werkstattauftrag hat keine Artikelnummer.";
            return RedirectToAction(nameof(Index));
        }

        var bomItems = await _bomRepository.GetBomItemsAsync(order.ArticleNumber);

        await _pickingRepository.InitializePickingAsync(
            id, bomItems,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        var pickingItems = await _pickingRepository.GetByProductionOrderAsync(id);

        var articleNumbers = bomItems.Select(b => b.Artikelnummer).Distinct().ToList();
        var stockByArticle = await _stockMovementRepository.GetStockByArticleNumbersAsync(articleNumbers);

        var allStorageLocations = await _storageLocationRepository.GetAllOrderedAsync();

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
            var picking = pickingItems.FirstOrDefault(p => p.BomArticleNumber == bom.Artikelnummer && p.BomPosition == bom.Position);
            stockByArticle.TryGetValue(bom.Artikelnummer, out var stockLocations);
            var locations = stockLocations ?? new List<StockLocationInfo>();

            // TreeLevel: Ebene 0 wenn Baugruppe leer/gleich WA-Artikel, sonst Ebene 1
            var treeLevel = 0;
            if (!string.IsNullOrEmpty(bom.Baugruppe) &&
                !string.Equals(bom.Baugruppe, order.ArticleNumber, StringComparison.OrdinalIgnoreCase))
            {
                treeLevel = 1;
            }

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
                Menge = bom.Menge,
                Beschaffungsartikel = bom.Beschaffungsartikel,
                Artikelgruppe = bom.Artikelgruppe,
                StockLocations = locations,
                TreeLevel = treeLevel,
                PickingItemId = picking?.Id,
                IsPicked = picking?.IsPicked ?? false,
                SourceStorageLocationId = sourceLocationId,
                SuggestedSourceStorageLocationId = suggestedLocationId,
                IsTransferred = picking?.IsTransferred ?? false
            };
        })
        .OrderBy(v => v.Position)
        .ToList();

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            viewItems = viewItems.Where(i =>
                i.Artikelnummer.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
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
            AllStorageLocations = allStorageLocations
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePicked(int pickingItemId, int? storageLocationId)
    {
        await _pickingRepository.TogglePickedAsync(
            pickingItemId,
            storageLocationId,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferPicked(int productionOrderId, int targetStorageLocationId)
    {
        var pickedItems = await _pickingRepository.GetPickedNotTransferredAsync(productionOrderId);
        if (!pickedItems.Any())
        {
            return BadRequest("Keine gepickten Artikel zum Umbuchen vorhanden.");
        }

        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        var appUserId = _currentUserService.GetCurrentAppUserId();
        var now = DateTime.Now;

        foreach (var item in pickedItems)
        {
            if (!item.SourceStorageLocationId.HasValue) continue;

            var article = await _articleRepository.GetByArticleNumberAsync(item.BomArticleNumber);
            if (article == null) continue;

            var movement = new StockMovement
            {
                ArticleId = article.Id,
                Quantity = item.Quantity,
                StorageLocationId = targetStorageLocationId,
                SourceStorageLocationId = item.SourceStorageLocationId.Value,
                ProductionOrder = order?.OrderNumber,
                MovementType = MovementType.Umbuchung,
                Timestamp = now,
                UserId = appUserId,
                WindowsUser = _currentUserService.GetWindowsUserName(),
                CreatedAt = now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };

            await _stockMovementRepository.AddAsync(movement);
        }

        await _pickingRepository.MarkAsTransferredAsync(
            pickedItems.Select(p => p.Id).ToList(), now);

        return Ok(new { count = pickedItems.Count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPickingStatus(int productionOrderId, string status)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        order.PickingStatus = status;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

        return Ok();
    }

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
            PickedBy = pickedItems.FirstOrDefault()?.PickedBy,
            Items = pickedItems.Select(p => new PrintPickingItem
            {
                Artikelnummer = p.BomArticleNumber,
                Menge = p.Quantity
            }).ToList()
        };

        if (!string.IsNullOrEmpty(order.ArticleNumber))
        {
            var bomItems = await _bomRepository.GetBomItemsAsync(order.ArticleNumber);
            foreach (var item in vm.Items)
            {
                var bom = bomItems.FirstOrDefault(b => b.Artikelnummer == item.Artikelnummer);
                if (bom != null)
                    item.Bezeichnung1 = bom.Bezeichnung1;
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int productionOrderId, IFormFile photo)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        var photosDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung");
        Directory.CreateDirectory(photosDir);

        // Bestehende Fotos für diesen WA zählen
        var existingPhotos = Directory.GetFiles(photosDir, $"{order.OrderNumber}_*");
        var seq = existingPhotos.Length + 1;

        var fileName = $"{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}_{seq}.jpg";
        var filePath = Path.Combine(photosDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        return Ok(new
        {
            success = true,
            fileName,
            url = $"/Fotos/Kommissionierung/{fileName}"
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPhotos(int productionOrderId)
    {
        var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
        if (order == null)
            return NotFound();

        var photosDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung");

        if (!Directory.Exists(photosDir))
            return Ok(Array.Empty<object>());

        var photos = Directory.GetFiles(photosDir, $"{order.OrderNumber}_*")
            .Select(f => new
            {
                fileName = Path.GetFileName(f),
                url = $"/Fotos/Kommissionierung/{Path.GetFileName(f)}"
            })
            .OrderBy(f => f.fileName)
            .ToArray();

        return Ok(photos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePhoto(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            return BadRequest();

        var filePath = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "Fotos", "Kommissionierung", fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return Ok();
    }
}
