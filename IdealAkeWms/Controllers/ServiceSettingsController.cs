using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class ServiceSettingsController : Controller
{
    private readonly IServiceSettingRepository _repository;

    public ServiceSettingsController(IServiceSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _repository.GetAllAsync();
        return View(settings);
    }

    public IActionResult Create()
    {
        return View(new ServiceSetting());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceSetting setting)
    {
        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var setting = await _repository.GetAllAsync();
        var item = setting.FirstOrDefault(s => s.Key == id);
        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ServiceSetting setting)
    {
        if (id != setting.Key)
            return NotFound();

        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _repository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Einstellung gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
