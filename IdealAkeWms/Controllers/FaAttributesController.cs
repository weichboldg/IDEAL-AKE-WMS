using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Stammdaten "FA-Merkmale": konfigurierbare Vorbau-Merkmale (Boolean/Dropdown)
/// inkl. Dropdown-Optionen und Arbeitsgang-Zuordnung (FaAttributeWorkSteps-Junction).
/// Muster: ArticleAttributesController (Definition/Option), Edit-Seite zusaetzlich
/// mit Checkbox-Liste der zugeordneten Arbeitsgaenge.
/// </summary>
[RequireMasterDataReadAccess]
public class FaAttributesController : Controller
{
    private readonly IFaAttributeRepository _attributeRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly ICurrentUserService _currentUserService;

    public FaAttributesController(
        IFaAttributeRepository attributeRepository,
        IWorkStepRepository workStepRepository,
        ICurrentUserService currentUserService)
    {
        _attributeRepository = attributeRepository;
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Typ-Badge "Boolean"/"Dropdown", Arbeitsgaenge als kommaseparierte Codes,
    /// Aktiv-Badge "Ja"/"Nein").
    /// </summary>
    private static readonly Dictionary<string, Func<FaAttributeDefinition, string?>> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = d => d.Name,
        ["type"] = d => d.AttributeType == AttributeType.Boolean ? "Boolean" : "Dropdown",
        ["work-steps"] = d => string.Join(", ", d.WorkSteps
            .OrderBy(j => j.WorkStep?.SortOrder ?? 0)
            .Select(j => j.WorkStep?.Code)
            .Where(c => !string.IsNullOrEmpty(c))),
        ["sort-order"] = d => d.SortOrder.ToString(),
        ["active"] = d => d.IsActive ? "Ja" : "Nein",
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var definitions = await _attributeRepository.GetAllAsync();

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(definitions, columnFilters, ColumnMap).ToList();

        ViewBag.Pagination = new PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = filtered.Count
        };
        return View(filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> CreateDefinition(string name, AttributeType attributeType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        var trimmed = name.Trim();
        var existing = await _attributeRepository.GetAllAsync();
        if (existing.Any(d => string.Equals(d.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["WarningMessage"] = $"Merkmal '{trimmed}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        var definition = new FaAttributeDefinition
        {
            Name = trimmed,
            AttributeType = attributeType,
            SortOrder = existing.Any() ? existing.Max(d => d.SortOrder) + 1 : 1,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _attributeRepository.AddDefinitionAsync(definition);
        TempData["SuccessMessage"] = $"Merkmal '{trimmed}' erstellt.";
        return RedirectToAction(nameof(Edit), new { id = definition.Id });
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var definition = await _attributeRepository.GetByIdAsync(id);
        if (definition == null)
            return NotFound();

        return View(await BuildEditViewModelAsync(definition));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, string name, int sortOrder, bool isActive, int[] workStepIds)
    {
        var definition = await _attributeRepository.GetByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(FaAttributeDefinition.Name), "Name darf nicht leer sein.");
            return View(await BuildEditViewModelAsync(definition, workStepIds));
        }

        definition.Name = name.Trim();
        definition.SortOrder = sortOrder;
        definition.IsActive = isActive;
        definition.ModifiedAt = DateTime.Now;
        definition.ModifiedBy = _currentUserService.GetDisplayName();
        definition.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _attributeRepository.UpdateDefinitionAsync(definition);
        await _attributeRepository.SetWorkStepsAsync(id, (workStepIds ?? Array.Empty<int>()).ToList(),
            _currentUserService.GetDisplayName(), _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Merkmal '{definition.Name}' aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteDefinition(int id)
    {
        var definition = await _attributeRepository.GetByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (!await _attributeRepository.DeleteDefinitionAsync(id))
        {
            TempData["WarningMessage"] = $"Merkmal '{definition.Name}' wird verwendet — bitte zuerst deaktivieren statt loeschen.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Merkmal '{definition.Name}' geloescht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> AddOption(int definitionId, string value, int sortOrder)
    {
        var definition = await _attributeRepository.GetByIdAsync(definitionId);
        if (definition == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["WarningMessage"] = "Wert darf nicht leer sein.";
            return RedirectToAction(nameof(Edit), new { id = definitionId });
        }

        var option = new FaAttributeOption
        {
            FaAttributeDefinitionId = definitionId,
            Value = value.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _attributeRepository.AddOptionAsync(option);
        TempData["SuccessMessage"] = $"Option '{option.Value}' zu '{definition.Name}' hinzugefuegt.";
        return RedirectToAction(nameof(Edit), new { id = definitionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> UpdateOption(int id, int definitionId, string value, int sortOrder, bool isActive)
    {
        var definition = await _attributeRepository.GetByIdAsync(definitionId);
        var option = definition?.Options.FirstOrDefault(o => o.Id == id);
        if (definition == null || option == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["WarningMessage"] = "Wert darf nicht leer sein.";
            return RedirectToAction(nameof(Edit), new { id = definitionId });
        }

        option.Value = value.Trim();
        option.SortOrder = sortOrder;
        option.IsActive = isActive;
        option.ModifiedAt = DateTime.Now;
        option.ModifiedBy = _currentUserService.GetDisplayName();
        option.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _attributeRepository.UpdateOptionAsync(option);
        TempData["SuccessMessage"] = $"Option '{option.Value}' aktualisiert.";
        return RedirectToAction(nameof(Edit), new { id = definitionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteOption(int id, int definitionId)
    {
        if (!await _attributeRepository.DeleteOptionAsync(id))
        {
            TempData["WarningMessage"] = "Option wird verwendet — bitte stattdessen deaktivieren.";
            return RedirectToAction(nameof(Edit), new { id = definitionId });
        }

        TempData["SuccessMessage"] = "Option geloescht.";
        return RedirectToAction(nameof(Edit), new { id = definitionId });
    }

    private async Task<FaAttributeEditViewModel> BuildEditViewModelAsync(
        FaAttributeDefinition definition, int[]? postedWorkStepIds = null)
    {
        return new FaAttributeEditViewModel
        {
            Definition = definition,
            AllWorkSteps = await _workStepRepository.GetActiveAsync(),
            SelectedWorkStepIds = postedWorkStepIds?.ToList()
                ?? definition.WorkSteps.Select(j => j.WorkStepId).ToList()
        };
    }
}
