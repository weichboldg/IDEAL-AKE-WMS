using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class SettingsController : Controller
{
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHolidayImportService _holidayImportService;
    private readonly IOseonOperationConfigRepository _operationConfigRepository;

    public SettingsController(
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        ICurrentUserService currentUserService,
        IHolidayImportService holidayImportService,
        IOseonOperationConfigRepository operationConfigRepository)
    {
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _currentUserService = currentUserService;
        _holidayImportService = holidayImportService;
        _operationConfigRepository = operationConfigRepository;
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
    [RequireMasterDataAccess]
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
    [RequireMasterDataAccess]
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
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        await _holidayRepository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Feiertag gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
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

    public async Task<IActionResult> OperationConfig()
    {
        var vm = new OseonOperationConfigViewModel
        {
            Configs = await _operationConfigRepository.GetAllAsync(),
            UnconfiguredNames = await _operationConfigRepository.GetUnconfiguredOperationNamesAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> AddOperationConfig(string operationName, string? displayName, int dueDateOffsetDays, bool isOseonRelevant)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            TempData["WarningMessage"] = "AG-Name darf nicht leer sein.";
            return RedirectToAction(nameof(OperationConfig));
        }

        if (await _operationConfigRepository.ExistsAsync(operationName.Trim()))
        {
            TempData["WarningMessage"] = $"AG '{operationName}' existiert bereits.";
            return RedirectToAction(nameof(OperationConfig));
        }

        var config = new OseonOperationConfig
        {
            OperationName = operationName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            DueDateOffsetDays = dueDateOffsetDays,
            IsOseonRelevant = isOseonRelevant
        };

        await _operationConfigRepository.AddAsync(config);
        TempData["SuccessMessage"] = $"Arbeitsgang '{operationName}' hinzugefügt.";
        return RedirectToAction(nameof(OperationConfig));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> UpdateOperationConfig(int id, string? displayName, int dueDateOffsetDays, bool isOseonRelevant)
    {
        var config = await _operationConfigRepository.GetByIdAsync(id);
        if (config == null)
            return NotFound();

        config.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        config.DueDateOffsetDays = dueDateOffsetDays;
        config.IsOseonRelevant = isOseonRelevant;

        await _operationConfigRepository.UpdateAsync(config);
        TempData["SuccessMessage"] = $"Arbeitsgang '{config.OperationName}' aktualisiert.";
        return RedirectToAction(nameof(OperationConfig));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteOperationConfig(int id)
    {
        var config = await _operationConfigRepository.GetByIdAsync(id);
        if (config == null)
            return NotFound();

        var name = config.OperationName;
        await _operationConfigRepository.DeleteAsync(id);
        TempData["SuccessMessage"] = $"Arbeitsgang '{name}' gelöscht.";
        return RedirectToAction(nameof(OperationConfig));
    }
}
