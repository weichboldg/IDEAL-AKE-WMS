using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;
    private readonly ICurrentUserService _currentUserService;

    public ArticlesController(
        IArticleRepository articleRepository,
        ICurrentUserService currentUserService)
    {
        _articleRepository = articleRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 100, string? search = null)
    {
        if (pageSize > 500) pageSize = 500;
        var (items, totalCount) = await _articleRepository.GetPaginatedAsync(page, pageSize, search);
        var vm = new ArticleIndexViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search
        };
        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new Article());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Article article)
    {
        if (!ModelState.IsValid)
            return View(article);

        article.CreatedAt = DateTime.Now;
        article.CreatedBy = _currentUserService.GetDisplayName();
        article.CreatedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.AddAsync(article);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        return View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Article article)
    {
        if (id != article.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(article);

        var existing = await _articleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.ArticleNumber = article.ArticleNumber;
        existing.Description = article.Description;
        existing.Unit = article.Unit;
        existing.ReorderLevel = article.ReorderLevel;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }
}
