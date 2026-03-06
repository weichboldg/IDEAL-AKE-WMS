using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class ProductionWorkplacesController : Controller
{
    private readonly IProductionWorkplaceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public ProductionWorkplacesController(
        IProductionWorkplaceRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var workplaces = await _repository.GetAllOrderedAsync();
        return View(workplaces);
    }

    public IActionResult Create()
    {
        return View(new ProductionWorkplaceEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionWorkplaceEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var workplace = new ProductionWorkplace
        {
            Name = vm.Name,
            Hall = vm.Hall,
            OverridePrePickingDays = vm.OverridePrePickingDays,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _repository.AddAsync(workplace);
        TempData["SuccessMessage"] = $"Werkbank '{workplace.Name}' wurde angelegt.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var workplace = await _repository.GetByIdAsync(id);
        if (workplace == null)
            return NotFound();

        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = workplace.Id,
            Name = workplace.Name,
            Hall = workplace.Hall,
            OverridePrePickingDays = workplace.OverridePrePickingDays
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductionWorkplaceEditViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(vm);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = vm.Name;
        existing.Hall = vm.Hall;
        existing.OverridePrePickingDays = vm.OverridePrePickingDays;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _repository.UpdateAsync(existing);
        TempData["SuccessMessage"] = $"Werkbank '{existing.Name}' wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }
}
