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

    public SettingsController(
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        ICurrentUserService currentUserService,
        IHolidayImportService holidayImportService)
    {
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _currentUserService = currentUserService;
        _holidayImportService = holidayImportService;
    }

    private static readonly string[] QrErstesFeldWerte = { "Artikelnummer", "Matchcode" };

    public async Task<IActionResult> Index()
    {
        var vm = new SettingsViewModel
        {
            Settings = await _settingRepository.GetAllAsync(),
            Holidays = await _holidayRepository.GetAllOrderedAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings)
    {
        // v1.68.0: QrErstesFeld ist ein Textfeld der Maske - ein Tippfehler fiele sonst still auf "Artikelnummer" zurueck.
        if (settings.TryGetValue(AppSettingKeys.QrErstesFeld, out var qrErstesFeld))
        {
            var kanonisch = QrErstesFeldWerte.FirstOrDefault(w => string.Equals(w, qrErstesFeld?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (kanonisch == null)
            {
                TempData["WarningMessage"] = "QrErstesFeld: erlaubt sind „Artikelnummer“ oder „Matchcode“ – nichts gespeichert.";
                return RedirectToAction(nameof(Index));
            }
            settings[AppSettingKeys.QrErstesFeld] = kanonisch;
        }

        foreach (var kvp in settings)
        {
            await _settingRepository.SetValueAsync(kvp.Key, kvp.Value);
        }

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
