using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class SettingsController : Controller
{
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICurrentUserService _currentUserService;

    public SettingsController(
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        ICurrentUserService currentUserService)
    {
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _currentUserService = currentUserService;
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
}
