using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

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

    public async Task<IActionResult> Index()
    {
        var articles = await _articleRepository.GetAllOrderedAsync();
        return View(articles);
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
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }
}
