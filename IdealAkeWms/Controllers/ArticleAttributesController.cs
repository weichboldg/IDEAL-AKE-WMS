using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class ArticleAttributesController : Controller
{
    private readonly IArticleAttributeRepository _attributeRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticleAttributesController(
        IArticleAttributeRepository attributeRepository,
        ICurrentUserService currentUserService)
    {
        _attributeRepository = attributeRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Typ-Badge "Boolean"/"Dropdown", Reihenfolge als Zahl). Die Aktiv-Spalte rendert
    /// eine Checkbox ohne Text — Filter-Konvention dafuer ist "Ja"/"Nein"
    /// (analog StorageLocations).
    /// </summary>
    private static readonly Dictionary<string, Func<ArticleAttributeDefinition, string?>> ColumnMap = new()
    {
        ["name"] = d => d.Name,
        ["type"] = d => d.AttributeType == AttributeType.Boolean ? "Boolean" : "Dropdown",
        ["sort-order"] = d => d.SortOrder.ToString(),
        ["active"] = d => d.IsActive ? "Ja" : "Nein",
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var definitions = await _attributeRepository.GetAllDefinitionsAsync();

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(definitions, columnFilters, ColumnMap).ToList();

        var paged = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        // Pre-load value counts for each definition (only for the current page)
        var valueCounts = new Dictionary<int, bool>();
        foreach (var def in paged)
        {
            valueCounts[def.Id] = await _attributeRepository.DefinitionHasValuesAsync(def.Id);
        }
        ViewBag.HasValues = valueCounts;
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = filtered.Count
        };
        return View(paged);
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

        if (await _attributeRepository.DefinitionExistsByNameAsync(name.Trim()))
        {
            TempData["WarningMessage"] = $"Merkmal '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        var definition = new ArticleAttributeDefinition
        {
            Name = name.Trim(),
            AttributeType = attributeType,
            SortOrder = await _attributeRepository.GetNextSortOrderAsync(),
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _attributeRepository.AddDefinitionAsync(definition);
        TempData["SuccessMessage"] = $"Merkmal '{name.Trim()}' erstellt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> UpdateDefinition(int id, string name, int sortOrder, bool isActive)
    {
        var definition = await _attributeRepository.GetDefinitionByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        definition.Name = name.Trim();
        definition.SortOrder = sortOrder;
        definition.IsActive = isActive;
        definition.ModifiedAt = DateTime.Now;
        definition.ModifiedBy = _currentUserService.GetDisplayName();
        definition.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _attributeRepository.UpdateDefinitionAsync(definition);
        TempData["SuccessMessage"] = $"Merkmal '{name.Trim()}' aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteDefinition(int id)
    {
        var definition = await _attributeRepository.GetDefinitionByIdAsync(id);
        if (definition == null)
            return NotFound();

        if (await _attributeRepository.DefinitionHasValuesAsync(id))
        {
            TempData["WarningMessage"] = $"Merkmal '{definition.Name}' wird verwendet — bitte zuerst deaktivieren statt loeschen.";
            return RedirectToAction(nameof(Index));
        }

        var name = definition.Name;
        await _attributeRepository.DeleteDefinitionAsync(id);
        TempData["SuccessMessage"] = $"Merkmal '{name}' geloescht.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> AddOption(int definitionId, string value, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["WarningMessage"] = "Wert darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        var definition = await _attributeRepository.GetDefinitionByIdAsync(definitionId);
        if (definition == null)
            return NotFound();

        var option = new ArticleAttributeOption
        {
            ArticleAttributeDefinitionId = definitionId,
            Value = value.Trim(),
            SortOrder = sortOrder
        };

        await _attributeRepository.AddOptionAsync(option);
        TempData["SuccessMessage"] = $"Option '{value.Trim()}' zu '{definition.Name}' hinzugefuegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteOption(int id)
    {
        if (await _attributeRepository.OptionIsInUseAsync(id))
        {
            TempData["WarningMessage"] = "Option wird von Artikeln verwendet und kann nicht geloescht werden.";
            return RedirectToAction(nameof(Index));
        }

        await _attributeRepository.DeleteOptionAsync(id);
        TempData["SuccessMessage"] = "Option geloescht.";
        return RedirectToAction(nameof(Index));
    }
}
