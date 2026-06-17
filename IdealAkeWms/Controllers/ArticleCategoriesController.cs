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

        // Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
        // Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
        // (Quelle-Badge "OSEON"/"Manuell", Artikel-Anzahl aus dem articleCounts-Dictionary).
        // Map wird lokal gebaut, weil die Artikel-Anzahl pro Request aus articleCounts kommt.
        var columnMap = new Dictionary<string, Func<ArticleCategory, string?>>
        {
            ["name"] = c => c.Name,
            ["description"] = c => c.Description,
            ["source"] = c => c.Source == "OSEON" ? "OSEON" : "Manuell",
            ["article-count"] = c => (articleCounts.TryGetValue(c.Id, out var count) ? count : 0).ToString(),
        };

        // Filter vor der Pagination — muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(categories, columnFilters, columnMap).ToList();

        ViewBag.Pagination = new Models.ViewModels.PaginationState
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
