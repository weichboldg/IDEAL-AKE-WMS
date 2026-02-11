using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

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

    public async Task<IActionResult> Index()
    {
        var locations = await _storageLocationRepository.GetAllOrderedAsync();
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

        existing.Code = location.Code;
        existing.Description = location.Description;
        existing.Zone = location.Zone;
        existing.Capacity = location.Capacity;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _storageLocationRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }
}
