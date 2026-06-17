using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class WorkStepsController : Controller
{
    private readonly IWorkStepRepository _workStepRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkStepsController(
        IWorkStepRepository workStepRepository,
        ICurrentUserService currentUserService)
    {
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Aktiv-Badge "Ja"/"Nein").
    /// </summary>
    private static readonly Dictionary<string, Func<WorkStep, string?>> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = w => w.Code,
        ["name"] = w => w.Name,
        ["search-string"] = w => w.SearchString,
        ["sort-order"] = w => w.SortOrder.ToString(),
        ["active"] = w => w.IsActive ? "Ja" : "Nein",
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var steps = await _workStepRepository.GetAllAsync();

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(steps, columnFilters, ColumnMap).ToList();

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
        return View(new WorkStep());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(WorkStep model)
    {
        // App-Layer-Unique-Check: InMemory-DB enforced den Unique-Index auf Code nicht.
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var existing = await _workStepRepository.GetByCodeAsync(model.Code.Trim());
            if (existing != null)
                ModelState.AddModelError(nameof(WorkStep.Code), $"Code '{model.Code.Trim()}' existiert bereits.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var step = new WorkStep
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            SearchString = string.IsNullOrWhiteSpace(model.SearchString) ? null : model.SearchString.Trim(),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _workStepRepository.AddAsync(step);
        TempData["SuccessMessage"] = $"Arbeitsgang '{step.Name}' wurde angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var step = await _workStepRepository.GetByIdAsync(id);
        if (step == null)
            return NotFound();

        return View(step);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, WorkStep model)
    {
        if (id != model.Id)
            return NotFound();

        // App-Layer-Unique-Check (ohne sich selbst): InMemory enforced Unique nicht.
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var byCode = await _workStepRepository.GetByCodeAsync(model.Code.Trim());
            if (byCode != null && byCode.Id != id)
                ModelState.AddModelError(nameof(WorkStep.Code), $"Code '{model.Code.Trim()}' existiert bereits.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var existing = await _workStepRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Code = model.Code.Trim();
        existing.Name = model.Name.Trim();
        existing.SearchString = string.IsNullOrWhiteSpace(model.SearchString) ? null : model.SearchString.Trim();
        existing.SortOrder = model.SortOrder;
        existing.IsActive = model.IsActive;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _workStepRepository.UpdateAsync(existing);
        TempData["SuccessMessage"] = $"Arbeitsgang '{existing.Name}' wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Delete(int id)
    {
        var step = await _workStepRepository.GetByIdAsync(id);
        if (step == null)
            return NotFound();

        var deleted = await _workStepRepository.DeleteAsync(id);
        if (!deleted)
        {
            TempData["WarningMessage"] = "Arbeitsgang wird verwendet und kann nicht geloescht werden — bitte deaktivieren.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Arbeitsgang '{step.Name}' wurde geloescht.";
        return RedirectToAction(nameof(Index));
    }
}
