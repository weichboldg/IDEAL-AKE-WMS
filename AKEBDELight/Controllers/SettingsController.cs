using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Filters;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

[RequireMasterDataAccess]
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
}
