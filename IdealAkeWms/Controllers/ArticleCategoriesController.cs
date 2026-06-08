using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class ArticleCategoriesController : Controller
{
    private readonly IArticleCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticleCategoriesController(
        IArticleCategoryRepository categoryRepository,
        ICurrentUserService currentUserService)
    {
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var categories = await _categoryRepository.GetAllOrderedAsync();
        var articleCounts = await _categoryRepository.GetArticleCountByCategoryAsync();
        ViewBag.ArticleCounts = articleCounts;
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = categories.Count
        };
        return View(categories.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        if (await _categoryRepository.ExistsByNameAsync(name.Trim()))
        {
            TempData["WarningMessage"] = $"Kategorie '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        var category = new ArticleCategory
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _categoryRepository.AddAsync(category);
        TempData["SuccessMessage"] = $"Kategorie '{name.Trim()}' erstellt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Update(int id, string name, string? description)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["WarningMessage"] = "Name darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }

        // Check uniqueness (excluding self)
        var existing = await _categoryRepository.GetByNameAsync(name.Trim());
        if (existing != null && existing.Id != id)
        {
            TempData["WarningMessage"] = $"Kategorie '{name.Trim()}' existiert bereits.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = name.Trim();
        category.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        category.ModifiedAt = DateTime.Now;
        category.ModifiedBy = _currentUserService.GetDisplayName();
        category.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _categoryRepository.UpdateAsync(category);
        TempData["SuccessMessage"] = $"Kategorie '{name.Trim()}' aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var articleCounts = await _categoryRepository.GetArticleCountByCategoryAsync();
        if (articleCounts.TryGetValue(id, out var count) && count > 0)
        {
            TempData["WarningMessage"] = $"Kategorie '{category.Name}' kann nicht geloescht werden — {count} Artikel verwenden diese Kategorie.";
            return RedirectToAction(nameof(Index));
        }

        var name = category.Name;
        var isOseon = category.Source == "OSEON";
        await _categoryRepository.DeleteAsync(id);

        var msg = $"Kategorie '{name}' geloescht.";
        if (isOseon)
            msg += " Hinweis: OSEON-Kategorie wird beim naechsten Sync ggf. neu erstellt.";

        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index));
    }
}
