using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.EntityFrameworkCore;
using BarcodeStandard;
using SkiaSharp;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class StorageLocationsController : Controller
{
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceSettingRepository _serviceSettings;

    public StorageLocationsController(
        IStorageLocationRepository storageLocationRepository,
        ICurrentUserService currentUserService,
        IServiceSettingRepository serviceSettings)
    {
        _storageLocationRepository = storageLocationRepository;
        _currentUserService = currentUserService;
        _serviceSettings = serviceSettings;
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
        ["sage-uebergabe-fehler"] = l => l.SageUebergabeFehler,
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
        // v1.99.71 Lagerplatz-Anlage in Sage (F6): Schalter fuer Knopf/Hinweis, offene Lager-Uebergaben als Hinweis ueber der Tabelle
        ViewBag.SageAnlageAktiv = await SageAnlageAnAsync();
        var lagerOffen = await _storageLocationRepository.GetOffeneLagerUebergabenAsync();
        var areas = lagerOffen.Count == 0 ? new List<StorageArea>() : await _storageLocationRepository.GetStorageAreasAsync();
        ViewBag.LagerUebergaben = lagerOffen
            .Select(kv => (Area: areas.FirstOrDefault(a => a.Id == kv.Key), Eintrag: kv.Value))
            .Where(x => x.Area is not null)
            .Select(x => (Area: x.Area!, x.Eintrag)).ToList();
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

        // v1.99.71 (AK 3): StorageAreaId kennzeichnet „Sage-Anlage“ - nie aus dem POST uebernehmen (Ueberposting).
        location.StorageAreaId = null;
        location.StorageArea = null;
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

        var existing = await _storageLocationRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // v1.99.71 (F7): Sage-Anlage-Platz vor der Uebergabe - Code/Bezeichnung/Zone gesperrt (Code = Schluessel zu Sage),
        // die 12-Zeichen-Regel fuer manuelle Codes gilt nicht (Code ist <Lagerkennung>;D1;D2;D3).
        var sageAnlage = IstSageAnlage(existing);
        if (sageAnlage)
        {
            ModelState.Remove(nameof(StorageLocation.Code));
            ModelState.Remove(nameof(StorageLocation.Description));
            ModelState.Remove(nameof(StorageLocation.Zone));
            location.Code = existing.Code;
            location.Description = existing.Description;
            location.Zone = existing.Zone;
            location.StorageAreaId = existing.StorageAreaId;
        }

        if (!ModelState.IsValid)
            return View(location);

        if (existing.Source == StorageLocationSource.Sage)
        {
            // Sage-kontrollierte Felder bleiben unangetastet — der Sync ist Master.
            existing.Capacity = location.Capacity;
            existing.IsPickingTransport = location.IsPickingTransport;
            existing.IstBuchbar = location.IstBuchbar;       // user-controlled, auch fuer Sage
            existing.SageBuchungErlaubt = location.SageBuchungErlaubt; // user-controlled Opt-in
            // IsActive ist Sync-kontrolliert: NICHT aus dem POST uebernehmen.
        }
        else if (sageAnlage)
        {
            existing.Capacity = location.Capacity;
            existing.IsPickingTransport = location.IsPickingTransport;
            existing.IsActive = location.IsActive;               // deaktiviert = wird nicht gesendet (F3)
            existing.IstBuchbar = location.IstBuchbar;
            existing.SageBuchungErlaubt = location.SageBuchungErlaubt;
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

    // ===================== v1.99.71 Lagerplatz-Anlage in Sage (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage) =====================

    internal const string AusgeschaltetText = "Lagerplatz-Anlage in Sage ist ausgeschaltet (Service-Einstellung SageLagerplatzAnlageAktiv) – es wurde nichts angelegt.";
    internal const string KeineLagerorteText = "Keine Sage-Lagerorte im WMS. Der Lagerplatz-Abgleich muss mit Quelle SData laufen (Service-Einstellung Sync:Quelle:Lagerplaetze), damit Lagerorte zur Auswahl stehen.";
    internal const string LagerAnlageNichtFreigegebenText = "Lager-Anlage in Sage ist an diesem Standort nicht freigegeben – Lagerplätze nur in vorhandenen Lagern.";
    internal const string InzwischenAngelegtText = "Inzwischen von jemand anderem angelegt – bitte erneut prüfen.";

    /// <summary>Kennzeichen „Sage-Anlage“ (F2): manuell erfasst UND einem Sage-Lagerort zugeordnet (StorageAreaId setzt sonst nur der Abgleich).</summary>
    public static bool IstSageAnlage(StorageLocation l) => l.Source == StorageLocationSource.Manual && l.StorageAreaId != null;

    private async Task<bool> SageAnlageAnAsync() =>
        string.Equals((await _serviceSettings.GetValueAsync(SageLagerplatzAnlage.ToggleKey))?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    private async Task<(List<StorageArea> Areas, Dictionary<int, SageStammdatenOutboxItem> Offen)> FuelleAsync(StorageLocationSageAnlageViewModel vm)
    {
        var areas = await _storageLocationRepository.GetStorageAreasAsync();
        var offen = await _storageLocationRepository.GetOffeneLagerUebergabenAsync();
        var techniken = SageLagerplatzAnlage.TechnikenDerStufe(await _serviceSettings.GetValueAsync(SageLagerplatzAnlage.LagertechnikenKey));
        vm.Lagerorte = areas.Where(a => a.IsActive && a.SageMissingSince is null).Select(a =>
        {
            var text = $"{a.Lagerkennung} – {a.Bezeichnung}" + (string.IsNullOrWhiteSpace(a.Lagertechnik) ? "" : $" ({a.Lagertechnik})");
            offen.TryGetValue(a.Id, out var o);
            var fehler = o?.Status == SageBookingQueueStatus.Fehler;
            if (fehler) text += " (Fehler – erst ‚Erneut senden‘)";
            else if (o is not null) text += " (neu, wartet auf Sage)";
            return new StorageLocationSageAnlageViewModel.LagerortOption(a.Id, text, !fehler);
        }).ToList();
        vm.Techniken = SageLagerplatzAnlage.Techniken.Where(t => techniken.Contains(t.Kennung)).ToList();
        return (areas, offen);
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> CreateInSage()
    {
        if (!await SageAnlageAnAsync())
        {
            TempData["WarningMessage"] = AusgeschaltetText;
            return RedirectToAction(nameof(Index));
        }
        var vm = new StorageLocationSageAnlageViewModel();
        await FuelleAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> CreateInSage(StorageLocationSageAnlageViewModel vm)
    {
        if (!await SageAnlageAnAsync())
        {
            TempData["WarningMessage"] = AusgeschaltetText;
            return RedirectToAction(nameof(Index));
        }
        var (areas, _) = await FuelleAsync(vm);
        if (vm.Lagerorte.Count == 0)
        {
            ModelState.AddModelError(string.Empty, KeineLagerorteText);
            return View(vm);
        }

        var codes = new HashSet<string>(await _storageLocationRepository.GetAllCodesAsync(), StringComparer.OrdinalIgnoreCase);
        var neu = vm.Lagerort == StorageLocationSageAnlageViewModel.NeuesLagerWert;
        StorageArea? gewaehlt = null;
        var kennung = "";
        string? technik = null;
        if (neu)
        {
            if (!vm.LagerAnlageFreigegeben)
                ModelState.AddModelError(nameof(vm.Lagerort), LagerAnlageNichtFreigegebenText);
            kennung = vm.NeueKennung?.Trim() ?? "";
            var basis = SageLagerplatzAnlage.Code(kennung, 0, 0, 0);
            if (!SageLagerplatzAnlage.KennungMuster.IsMatch(kennung))
                ModelState.AddModelError(nameof(vm.NeueKennung), "Lagerkennung: 1 bis 10 Zeichen, nur Buchstaben A–Z, Ziffern, „-“ und „_“ (keine Leer- oder Sonderzeichen).");
            else if (areas.Any(a => string.Equals(a.Lagerkennung, kennung, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(vm.NeueKennung), $"Lagerkennung {kennung} schon vorhanden (Groß/Kleinschreibung zählt nicht).");
            else if (codes.Contains(basis))
                ModelState.AddModelError(nameof(vm.NeueKennung), $"Platz-Code {basis} schon vorhanden – in Sage entstünde eine zweite Kurzbezeichnung.");
            var bez = vm.NeueBezeichnung?.Trim() ?? "";
            if (bez.Length is < 1 or > 40)
                ModelState.AddModelError(nameof(vm.NeueBezeichnung), "Bezeichnung: 1 bis 40 Zeichen.");
            technik = vm.Techniken.FirstOrDefault(t => t.Kennung == vm.NeueTechnik)?.Kennung;
            if (technik is null)
                ModelState.AddModelError(nameof(vm.NeueTechnik), "Bitte eine am Standort freigegebene Lagertechnik wählen.");
            else if (technik == "LTD0ST" && vm.Platzverwaltet)
                ModelState.AddModelError(nameof(vm.Platzverwaltet), "Standardtechnik hat keine Dimensionen – keine Plätze.");
        }
        else
        {
            var opt = int.TryParse(vm.Lagerort, out var areaId) ? vm.Lagerorte.FirstOrDefault(o => o.Id == areaId) : null;
            if (opt is null)
                ModelState.AddModelError(nameof(vm.Lagerort), "Bitte einen Lagerort wählen.");
            else if (!opt.Waehlbar)
                ModelState.AddModelError(nameof(vm.Lagerort), "Dieses Lager konnte nicht in Sage angelegt werden – erst in der Lagerplatzliste „Erneut senden“, dann Plätze erfassen.");
            else
            {
                gewaehlt = areas.First(a => a.Id == areaId);
                kennung = gewaehlt.Lagerkennung;
            }
        }

        var mitPlaetzen = !neu || vm.Platzverwaltet;
        if (mitPlaetzen) PruefeBereich(vm);
        if (!ModelState.IsValid) return View(vm);

        var jetzt = DateTime.Now;
        var user = _currentUserService.GetDisplayName();
        var win = _currentUserService.GetWindowsUserName();
        var neuesLager = neu
            ? new StorageArea
            {
                Lagerkennung = kennung, Bezeichnung = vm.NeueBezeichnung!.Trim(), Lagertechnik = technik, IstDispoLager = true, IsActive = true,
                CreatedAt = jetzt, CreatedBy = user, CreatedByWindows = win,
            }
            : null;

        var plaetze = new List<StorageLocation>();
        var uebersprungen = new List<string>();
        if (mitPlaetzen)
            foreach (var (d1, d2, d3) in SageLagerplatzAnlage.Koordinaten(vm.D1Von, vm.D1Bis, vm.D2Von, vm.D2Bis, vm.D3Von, vm.D3Bis))
            {
                var code = SageLagerplatzAnlage.Code(kennung, d1, d2, d3);
                if (!codes.Add(code)) { uebersprungen.Add(code); continue; }
                plaetze.Add(new StorageLocation
                {
                    Code = code, BarcodeValue = code, Zone = kennung, StorageAreaId = gewaehlt?.Id, Source = StorageLocationSource.Manual,
                    IsActive = true, IstBuchbar = vm.IstBuchbar, SageBuchungErlaubt = vm.SageBuchungErlaubt,
                    SageUebergabeStatus = SageUebergabeStatus.Offen, CreatedAt = jetzt, CreatedBy = user, CreatedByWindows = win,
                });
            }

        if (neuesLager is not null || plaetze.Count > 0)
        {
            try
            {
                await _storageLocationRepository.AddSageAnlageAsync(neuesLager, plaetze);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, InzwischenAngelegtText);
                await FuelleAsync(vm);
                return View(vm);
            }
        }

        var text = (neuesLager is null ? "" : $"Lager {kennung} erfasst, wird vom Dienst in Sage angelegt; ")
                   + $"{plaetze.Count} Lagerplätze angelegt" + (plaetze.Count > 0 ? ", der Dienst legt sie in Sage an" : "");
        if (uebersprungen.Count > 0)
            TempData["WarningMessage"] = text + $", {uebersprungen.Count} übersprungen (Code im WMS schon vorhanden: "
                + string.Join(", ", uebersprungen.Take(20)) + (uebersprungen.Count > 20 ? ", …" : "") + ").";
        else
            TempData["SuccessMessage"] = text + ".";
        return RedirectToAction(nameof(Index), new { onlyUebergabeOffen = true });
    }

    private void PruefeBereich(StorageLocationSageAnlageViewModel vm)
    {
        const int max = SageLagerplatzAnlage.MaxKoordinate;
        if (vm.D1Von < 1 || vm.D1Bis > max) ModelState.AddModelError(nameof(vm.D1Von), $"Dimension 1: ganze Zahlen von 1 bis {max}.");
        if (vm.D2Von < 0 || vm.D2Bis > max) ModelState.AddModelError(nameof(vm.D2Von), $"Dimension 2: ganze Zahlen von 0 bis {max}.");
        if (vm.D3Von < 0 || vm.D3Bis > max) ModelState.AddModelError(nameof(vm.D3Von), $"Dimension 3: ganze Zahlen von 0 bis {max}.");
        if (vm.D1Von > vm.D1Bis) ModelState.AddModelError(nameof(vm.D1Bis), "Dimension 1: „von“ ist größer als „bis“.");
        if (vm.D2Von > vm.D2Bis) ModelState.AddModelError(nameof(vm.D2Bis), "Dimension 2: „von“ ist größer als „bis“.");
        if (vm.D3Von > vm.D3Bis) ModelState.AddModelError(nameof(vm.D3Bis), "Dimension 3: „von“ ist größer als „bis“.");
        if (!ModelState.IsValid) return;
        var anzahl = SageLagerplatzAnlage.Anzahl(vm.D1Von, vm.D1Bis, vm.D2Von, vm.D2Bis, vm.D3Von, vm.D3Bis);
        if (anzahl > SageLagerplatzAnlage.MaxPlaetze)
            ModelState.AddModelError(string.Empty, $"Der Bereich ergibt {anzahl} Lagerplätze – höchstens {SageLagerplatzAnlage.MaxPlaetze} je Absenden.");
    }

    /// <summary>F6: „Erneut senden“ eines Platzes mit Uebergabe-Fehler (Outbox + Platz Fehler -> Offen).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public Task<IActionResult> ErneutSenden(int id) => ErneutSendenAsync(SageStammdatenOutbox.TypLagerplatz, id);

    /// <summary>F6: „Erneut senden“ eines Lagers mit Uebergabe-Fehler (Outbox Fehler -> Offen).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public Task<IActionResult> LagerErneutSenden(int id) => ErneutSendenAsync(SageStammdatenOutbox.TypLagerort, id);

    private async Task<IActionResult> ErneutSendenAsync(string typ, int id)
    {
        if (!await SageAnlageAnAsync())
            TempData["WarningMessage"] = AusgeschaltetText;
        else if (await _storageLocationRepository.ErneutSendenAsync(typ, id, _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName()))
            TempData["SuccessMessage"] = "Erneut eingereiht – der Dienst sendet beim nächsten Takt.";
        else
            TempData["WarningMessage"] = "Kein Übergabe-Fehler zum erneuten Senden gefunden (Status hat sich inzwischen geändert).";
        return RedirectToAction(nameof(Index), new { onlyUebergabeOffen = true });
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
