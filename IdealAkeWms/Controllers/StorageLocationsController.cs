using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using BarcodeStandard;
using SkiaSharp;

namespace IdealAkeWms.Controllers;

public class StorageLocationsController : Controller
{
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly ICurrentUserService _currentUserService;

    public StorageLocationsController(
        IStorageLocationRepository storageLocationRepository,
        ICurrentUserService currentUserService)
    {
        _storageLocationRepository = storageLocationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(bool showInactive = false)
    {
        var all = await _storageLocationRepository.GetAllOrderedAsync();
        var locations = showInactive ? all : all.Where(l => l.IsActive).ToList();
        ViewBag.ShowInactive = showInactive;
        ViewBag.HasInactive = all.Any(l => !l.IsActive);
        return View(locations);
    }

    public IActionResult Create()
    {
        return View(new StorageLocation());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StorageLocation location)
    {
        if (!ModelState.IsValid)
            return View(location);

        // BarcodeValue automatisch aus Code setzen
        location.BarcodeValue = location.Code;
        location.CreatedAt = DateTime.Now;
        location.CreatedBy = _currentUserService.GetDisplayName();
        location.CreatedByWindows = _currentUserService.GetWindowsUserName();

        await _storageLocationRepository.AddAsync(location);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var location = await _storageLocationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        return View(location);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StorageLocation location)
    {
        if (id != location.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(location);

        var existing = await _storageLocationRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (existing.Source == StorageLocationSource.Sage)
        {
            // Sage-kontrollierte Felder bleiben unangetastet — der Sync ist Master.
            existing.Capacity = location.Capacity;
            existing.IsPickingTransport = location.IsPickingTransport;
            // IsActive ist Sync-kontrolliert: NICHT aus dem POST uebernehmen.
        }
        else
        {
            existing.Code = location.Code;
            existing.Description = location.Description;
            existing.Zone = location.Zone;
            existing.Capacity = location.Capacity;
            existing.IsPickingTransport = location.IsPickingTransport;
            existing.IsActive = location.IsActive;
            existing.BarcodeValue = location.Code; // BarcodeValue aktualisieren
        }

        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _storageLocationRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> PrintLabels()
    {
        var locations = await _storageLocationRepository.GetAllOrderedAsync();

        // Barcodes generieren
        var labelsData = new List<(StorageLocation Location, string BarcodeBase64)>();
        foreach (var loc in locations)
        {
            var barcodeBase64 = GenerateBarcodeBase64(loc.BarcodeValue ?? loc.Code);
            labelsData.Add((loc, barcodeBase64));
        }

        ViewBag.LabelsData = labelsData;
        return View(locations);
    }

    public async Task<IActionResult> PrintLabel(int id)
    {
        var location = await _storageLocationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        var barcodeBase64 = GenerateBarcodeBase64(location.BarcodeValue ?? location.Code);
        ViewBag.LabelsData = new List<(StorageLocation Location, string BarcodeBase64)>
        {
            (location, barcodeBase64)
        };

        return View("PrintLabels", new List<StorageLocation> { location });
    }

    private string GenerateBarcodeBase64(string value)
    {
        var barcode = new Barcode();
        // Breite dynamisch: längere Texte brauchen mehr Pixel für dickere Balken
        var width = Math.Max(800, value.Length * 45);
        var image = barcode.Encode(BarcodeStandard.Type.Code128, value, SKColors.Black, SKColors.White, width, 200);

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(data.ToArray());
    }
}
