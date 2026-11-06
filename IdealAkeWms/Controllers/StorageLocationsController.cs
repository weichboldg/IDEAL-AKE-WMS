using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using BarcodeStandard;
using SkiaSharp;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
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

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Badge-Texte "Wagen"/"Sage"/"Manuell"/"Ja"/"Nein", Datum/Kapazitaet im View-Format).
    /// </summary>
    private static readonly Dictionary<string, Func<StorageLocation, string?>> ColumnMap = new()
    {
        ["code"] = l => l.Code,
        ["description"] = l => l.Description,
        ["zone"] = l => l.Zone,
        ["storage-area"] = l => StorageAreaText(l),
        ["sage-locked"] = l => SageSperreText(l),
        ["capacity"] = l => l.Capacity?.ToString("N2"),
        ["type"] = l => l.IsPickingTransport ? "Wagen" : "",
        ["source"] = l => l.Source == StorageLocationSource.Sage ? "Sage" : "Manuell",
        ["bookable"] = l => l.IstBuchbar ? "Ja" : "Nein",
        ["sage-buchung"] = l => l.SageBuchungErlaubt ? "Ja" : "Nein",
        ["is-active"] = l => l.IsActive ? "Ja" : "Nein",
        ["sage-uebergabe"] = l => SageUebergabeStatusText.Text(l.SageUebergabeStatus),
        ["created-at"] = l => l.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
    };

    /// <summary>Zelltext der Lagerort-Spalte (Sage-Lagerort, v1.73.0): Bezeichnung, sonst Lagerkennung; leer ohne Lagerort.</summary>
    public static string? StorageAreaText(StorageLocation l) =>
        l.StorageArea is null ? null : string.IsNullOrWhiteSpace(l.StorageArea.Bezeichnung) ? l.StorageArea.Lagerkennung : l.StorageArea.Bezeichnung;

    /// <summary>Zelltext der Sperr-Spalte (aus Sage): "Gesperrt", "Inventur", beides oder leer (nicht gesperrt bzw. nie aus SData gelesen).</summary>
    public static string SageSperreText(StorageLocation l) =>
        string.Join(", ", new[] { l.SageIstGesperrt == true ? "Gesperrt" : null, l.SageIstGesperrtInventur == true ? "Inventur" : null }.OfType<string>());

    public async Task<IActionResult> Index(bool showInactive = false, bool onlyBookable = false,
        bool onlyUebergabeOffen = false, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var all = await _storageLocationRepository.GetAllOrderedAsync();
        var query = all.AsQueryable();
        if (!showInactive)
            query = query.Where(l => l.IsActive);
        if (onlyBookable)
            query = query.Where(l => l.IstBuchbar);
        // E9/F11: „Offene Übergaben an Sage“ = im WMS angelegt, in Sage noch nicht gefunden (Offen oder Fehler).
        if (onlyUebergabeOffen)
            query = query.Where(l => SageUebergabeStatusText.IstOffen(l.SageUebergabeStatus));

        // Server-Side-Spaltenfilter: nach den Top-Filtern, vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(query, columnFilters, ColumnMap, ColumnFilterTypes.For("StorageLocations/Index")).ToList();

        ViewBag.ShowInactive = showInactive;
        ViewBag.OnlyBookable = onlyBookable;
        ViewBag.OnlyUebergabeOffen = onlyUebergabeOffen;
        ViewBag.UebergabeOffenCount = all.Count(l => l.IsActive && SageUebergabeStatusText.IstOffen(l.SageUebergabeStatus));
        ViewBag.HasInactive = all.Any(l => !l.IsActive);
        ViewBag.HasNonBookable = all.Any(l => !l.IstBuchbar);
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = filtered.Count
        };
        return View(filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [RequireMasterDataAccess]
    public IActionResult Create()
    {
        return View(new StorageLocation());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(StorageLocation location)
    {
        if (!ModelState.IsValid)
            return View(location);

        // BarcodeValue automatisch aus Code setzen
        location.BarcodeValue = location.Code;
        // E9/F11: im WMS neu angelegt -> Uebergabe an Sage offen (Repository merkt den Outbox-Eintrag vor; gesendet wird nichts).
        location.SageUebergabeStatus = SageUebergabeStatus.Offen;
        location.SageUebergabeAm = null;
        location.SageUebergabeFehler = null;
        location.CreatedAt = DateTime.Now;
        location.CreatedBy = _currentUserService.GetDisplayName();
        location.CreatedByWindows = _currentUserService.GetWindowsUserName();

        await _storageLocationRepository.AddAsync(location);
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var location = await _storageLocationRepository.GetByIdAsync(id);
        if (location == null)
            return NotFound();

        return View(location);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
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
            existing.IstBuchbar = location.IstBuchbar;       // user-controlled, auch fuer Sage
            existing.SageBuchungErlaubt = location.SageBuchungErlaubt; // user-controlled Opt-in
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
            existing.IstBuchbar = location.IstBuchbar;       // user-controlled
            existing.SageBuchungErlaubt = location.SageBuchungErlaubt; // user-controlled Opt-in
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
