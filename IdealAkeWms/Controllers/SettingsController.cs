using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

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
        var kategorien = (await _categories.GetAllOrderedAsync()).Select(c => (c.Name, c.Name)).ToList();
        var gruppen = (await _recipients.GetAllGroupsAsync()).OrderBy(g => g.Name).Select(g => (g.Id.ToString(), $"{g.Name} (Id {g.Id})")).ToList();
        var plaetze = (await _storageLocations.GetAllOrderedAsync()).Select(l => (l.Code, l.Code)).ToList();
        var arbeitsgaenge = (await _operations.GetAllAsync()).Select(o => o.OperationName).Distinct(StringComparer.OrdinalIgnoreCase)
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
        // v1.68.0/v1.69.0: Einstellungen mit festen Werten (Choice im Katalog AppSettingDefinitions, in der Maske ein Dropdown) nur
        // kanonisch speichern - ein ungueltiger Wert (alter Stand, Handeingabe) fiele sonst still auf den Standard zurueck.
        foreach (var (key, werte) in AppSettingDefinitions.All.Where(d => d.Type == SettingType.Choice).Select(d => (d.Key, d.Werte!)))
        {
            if (!settings.TryGetValue(key, out var eingabe)) continue;
            var kanonisch = werte.Select(w => w.Wert).FirstOrDefault(w => string.Equals(w, eingabe?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (kanonisch == null)
            {
                TempData["WarningMessage"] = $"{key}: erlaubt sind {string.Join(" oder ", werte.Select(w => $"„{w.Wert}“"))} – nichts gespeichert.";
                return RedirectToAction(nameof(Index));
            }
            settings[key] = kanonisch;
        }

        // F4: Speichern ohne Nebenwirkung — ohne DB-Zeile und mit dem (wirksamen) Default gepostet = nichts schreiben (fuer manche Keys
        // wirkt "" anders als „Zeile fehlt“); unveraenderte Werte vorhandener Zeilen nicht neu schreiben.
        var bestand = (await _settingRepository.GetAllAsync()).GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First().Value);
        foreach (var kvp in settings)
        {
            var hatZeile = bestand.TryGetValue(kvp.Key, out var alt);
            if (hatZeile && string.Equals(alt, kvp.Value, StringComparison.Ordinal)) continue;
            if (!hatZeile && AppSettingDefinitions.TryGet(kvp.Key, out var def) && EntsprichtDefault(def, kvp.Value)) continue;
            await _settingRepository.SetValueAsync(kvp.Key, kvp.Value);
        }

        TempData["SuccessMessage"] = "Einstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    internal static bool EntsprichtDefault(SettingDefinition d, string? wert) => d.Type == SettingType.Bool
        ? SettingItem.BoolWirksam(d, wert) == SettingItem.BoolWirksam(d, d.DefaultValue)
        : string.Equals((wert ?? "").Trim(), d.DefaultValue.Trim(), StringComparison.Ordinal);

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
