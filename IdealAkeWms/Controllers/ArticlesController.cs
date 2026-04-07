using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IArticleAttributeRepository _attributeRepository;
    private readonly IArticleCategoryRepository _categoryRepository;

    public ArticlesController(
        IArticleRepository articleRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IArticleAttributeRepository attributeRepository,
        IArticleCategoryRepository categoryRepository)
    {
        _articleRepository = articleRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _attributeRepository = attributeRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 100, string? search = null)
    {
        if (pageSize > 500) pageSize = 500;
        var (items, totalCount) = await _articleRepository.GetPaginatedAsync(page, pageSize, search);

        // Load active attribute definitions + batch-load values for displayed articles
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var articleIds = items.Select(a => a.Id).ToList();
        var attributeValues = await _attributeRepository.GetValuesByArticleIdsAsync(articleIds);

        var vm = new ArticleIndexViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = search,
            AttributeDefinitions = activeDefinitions,
            AttributeValuesByArticle = attributeValues
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

    [HttpGet]
    public async Task<IActionResult> Info(string? articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber))
            return View((ArticleInfoViewModel?)null);

        var article = await _articleRepository.GetByArticleNumberAsync(articleNumber.Trim());
        if (article == null)
        {
            ViewBag.NotFound = articleNumber.Trim();
            return View((ArticleInfoViewModel?)null);
        }

        var stock = await _stockMovementRepository.GetCurrentStockAsync(
            filterArticle: article.ArticleNumber);

        var vm = new ArticleInfoViewModel
        {
            ArticleNumber = article.ArticleNumber,
            Description = article.Description ?? string.Empty,
            Unit = article.Unit,
            ArticleGroup = article.ArticleGroup,
            ReorderLevel = article.ReorderLevel ?? 0,
            VaultUrl = $"http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Uri.EscapeDataString(article.ArticleNumber)}&searchContext=0",
            StockByLocation = stock
        };
        return View(vm);
    }
}
