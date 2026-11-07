using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Settings;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class SettingsController : Controller
{
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHolidayImportService _holidayImportService;
    private readonly IArticleCategoryRepository _categories;
    private readonly IOrderRecipientRepository _recipients;
    private readonly IStorageLocationRepository _storageLocations;
    private readonly IOperationConfigRepository _operations;

    public SettingsController(
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        ICurrentUserService currentUserService,
        IHolidayImportService holidayImportService,
        IArticleCategoryRepository categories,
        IOrderRecipientRepository recipients,
        IStorageLocationRepository storageLocations,
        IOperationConfigRepository operations)
    {
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _currentUserService = currentUserService;
        _holidayImportService = holidayImportService;
        _categories = categories;
        _recipients = recipients;
        _storageLocations = storageLocations;
        _operations = operations;
    }

    public async Task<IActionResult> Index()
    {
        // v1.77.0 (Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung E2): jeder Katalog-Key, auch ohne DB-Zeile (dann Default).
        var rows = await _settingRepository.GetAllAsync();
        var byKey = rows.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => (string?)g.First().Value);
        var optionen = await OptionenAsync();
        var items = AppSettingDefinitions.All
            .Select(d => SettingItem.Aus(d, byKey.GetValueOrDefault(d.Key), optionen.GetValueOrDefault(d.Key)))
            .ToList();
        var vm = new SettingsViewModel
        {
            Groups = SettingGroup.Bilden(items),
            Orphans = rows.Where(r => !AppSettingDefinitions.TryGet(r.Key, out _)).OrderBy(r => r.Key).ToList(),
            Holidays = await _holidayRepository.GetAllOrderedAsync()
        };
        return View(vm);
    }

    /// <summary>Auswahl aus Stammdaten (MasterData) bzw. Vorschlaege (BdeDefaultArbeitsgang) je Key.</summary>
    internal async Task<Dictionary<string, IReadOnlyList<(string Wert, string Text)>>> OptionenAsync()
    {
        var kategorien = ((await _categories.GetAllOrderedAsync()) ?? new()).Select(c => (c.Name, c.Name)).ToList();
        var gruppen = ((await _recipients.GetAllGroupsAsync()) ?? new()).OrderBy(g => g.Name).Select(g => (g.Id.ToString(), $"{g.Name} (Id {g.Id})")).ToList();
        var plaetze = ((await _storageLocations.GetAllOrderedAsync()) ?? new()).Select(l => (l.Code, l.Code)).ToList();
        var arbeitsgaenge = ((await _operations.GetAllAsync()) ?? new()).Select(o => o.OperationName).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n).Select(n => (n, n)).ToList();
        var result = new Dictionary<string, IReadOnlyList<(string Wert, string Text)>>
        {
            [AppSettingKeys.BdeDefaultArbeitsgang] = arbeitsgaenge,
        };
        foreach (var d in AppSettingDefinitions.All.Where(d => d.Type == SettingType.MasterData))
            result[d.Key] = d.Stammdaten switch
            {
                SettingStammdaten.Artikelkategorie => kategorien,
                SettingStammdaten.Bestellempfaengergruppe => gruppen,
                SettingStammdaten.Lagerplatz => plaetze,
                _ => Array.Empty<(string, string)>(),
            };
        return result;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings)
    {
        settings ??= new Dictionary<string, string>();
        // v1.77.0 E4a (Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung F5, ADR 0020): nur Katalog-Keys (Allow-List), ALLE pruefen,
        // bevor geschrieben wird — bei einem Fehler wird nichts gespeichert. Speichern ohne Nebenwirkung (F4).
        var bestand = (await _settingRepository.GetAllAsync() ?? new List<AppSetting>()).GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var optionen = await OptionenAsync();
        var fehler = new List<string>();
        var schreiben = new List<(string Key, string Wert)>();
        foreach (var (key, roh) in settings)
        {
            if (!AppSettingDefinitions.TryGet(key, out var def)) continue;
            var stamm = def.Type == SettingType.MasterData && optionen.TryGetValue(key, out var o) ? o.Select(x => x.Wert).ToList() : null;
            var pruefung = SettingValidator.Validate(def, roh, stamm);
            if (!pruefung.Gueltig) { fehler.Add(pruefung.Fehler!); continue; }
            var wert = def.Type == SettingType.String ? roh ?? "" : pruefung.Kanonisch!;
            var hatZeile = bestand.TryGetValue(key, out var alt);
            if (hatZeile && string.Equals(alt, wert, StringComparison.Ordinal)) continue;
            if (!hatZeile && SettingItem.EntsprichtDefault(def, wert)) continue;
            schreiben.Add((key, wert));
        }

        // AK 13: „nur FA-Meldung“ braucht einen Default-Arbeitsgang (bisher nur per JS geprueft).
        string Gilt(string key) => settings.TryGetValue(key, out var v) ? v ?? "" : bestand.GetValueOrDefault(key) ?? "";
        if (Gilt(AppSettingKeys.BdeNurFaMeldung).Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(Gilt(AppSettingKeys.BdeDefaultArbeitsgang)))
            fehler.Add("„BDE: Default-Arbeitsgang“ ist Pflicht, solange „BDE: nur FA-Meldung“ an ist.");

        if (fehler.Count > 0)
        {
            TempData["WarningMessage"] = "Nicht gespeichert: " + string.Join(" ", fehler);
            return RedirectToAction(nameof(Index));
        }

        foreach (var (key, wert) in schreiben)
            await _settingRepository.SetValueAsync(key, wert);

        TempData["SuccessMessage"] = "Einstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHoliday(DateTime date, string? description)
    {
        var holiday = new Holiday
        {
            Date = date,
            Description = description,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _holidayRepository.AddAsync(holiday);
        TempData["SuccessMessage"] = $"Feiertag {date:dd.MM.yyyy} hinzugefügt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        await _holidayRepository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Feiertag gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncHolidays()
    {
        var currentYear = DateTime.Now.Year;
        var nextYear = currentYear + 1;

        var imported1 = await _holidayImportService.ImportHolidaysAsync(
            currentYear,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        var imported2 = await _holidayImportService.ImportHolidaysAsync(
            nextYear,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        var total = imported1 + imported2;
        TempData["SuccessMessage"] = total > 0
            ? $"{total} Feiertage für {currentYear}/{nextYear} importiert."
            : $"Alle Feiertage für {currentYear}/{nextYear} sind bereits vorhanden.";

        return RedirectToAction(nameof(Index));
    }

    // ========== Arbeitsgang-Konfiguration ==========
    // Seit v1.54.0 in den BDE-Stammdaten (OperationConfigsController); alte URL bleibt als 302 (Spec 2026-10-09 Antwort 5),
    // die frueheren POST-Actions AddOperationConfig/UpdateOperationConfig/DeleteOperationConfig entfallen.

    public IActionResult OperationConfig() => RedirectToAction("Index", "OperationConfigs");
}
