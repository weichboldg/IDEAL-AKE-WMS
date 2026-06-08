using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class ArticlesController : Controller
{
    private readonly IArticleRepository _articleRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IArticleAttributeRepository _attributeRepository;
    private readonly IArticleCategoryRepository _categoryRepository;
    private readonly IBomCacheRepository _bomCacheRepository;
    private readonly IProductionOrderRepository _productionOrderRepository;

    public ArticlesController(
        IArticleRepository articleRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IArticleAttributeRepository attributeRepository,
        IArticleCategoryRepository categoryRepository,
        IBomCacheRepository bomCacheRepository,
        IProductionOrderRepository productionOrderRepository)
    {
        _articleRepository = articleRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _attributeRepository = attributeRepository;
        _categoryRepository = categoryRepository;
        _bomCacheRepository = bomCacheRepository;
        _productionOrderRepository = productionOrderRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null, string? search = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var (items, totalCount) = await _articleRepository.GetPaginatedAsync(page, effectivePageSize, search, columnFilters);

        // Load active attribute definitions + batch-load values for displayed articles
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var articleIds = items.Select(a => a.Id).ToList();
        var attributeValues = await _attributeRepository.GetValuesByArticleIdsAsync(articleIds);

        var vm = new ArticleIndexViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = effectivePageSize,
            TotalCount = totalCount,
            Search = search,
            AttributeDefinitions = activeDefinitions,
            AttributeValuesByArticle = attributeValues,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            }
        };
        return View(vm);
    }

    [RequireMasterDataAccess]
    public IActionResult Create()
    {
        return View(new Article());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
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

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
            return NotFound();

        var categories = await _categoryRepository.GetAllOrderedAsync();
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var existingValues = await _attributeRepository.GetValuesByArticleIdAsync(id);

        var attributeItems = activeDefinitions.Select(def =>
        {
            var existing = existingValues.FirstOrDefault(v => v.ArticleAttributeDefinitionId == def.Id);
            return new AttributeEditItem
            {
                DefinitionId = def.Id,
                Name = def.Name,
                AttributeType = def.AttributeType,
                BooleanValue = existing?.BooleanValue,
                SelectedOptionId = existing?.SelectedOptionId,
                Options = def.Options.ToList()
            };
        }).ToList();

        var vm = new ArticleEditViewModel
        {
            Article = article,
            Categories = categories,
            Attributes = attributeItems
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, ArticleEditViewModel vm)
    {
        if (id != vm.Article.Id)
            return NotFound();

        // Re-populate for validation failure
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categoryRepository.GetAllOrderedAsync();
            var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
            // Re-fill options for dropdown attributes
            foreach (var attr in vm.Attributes)
            {
                var def = activeDefinitions.FirstOrDefault(d => d.Id == attr.DefinitionId);
                if (def != null)
                {
                    attr.Name = def.Name;
                    attr.AttributeType = def.AttributeType;
                    attr.Options = def.Options.ToList();
                }
            }
            return View(vm);
        }

        var existing = await _articleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.ArticleNumber = vm.Article.ArticleNumber;
        existing.Description = vm.Article.Description;
        existing.Unit = vm.Article.Unit;
        existing.ReorderLevel = vm.Article.ReorderLevel;
        existing.ArticleCategoryId = vm.Article.ArticleCategoryId;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _articleRepository.UpdateAsync(existing);

        // Save attribute values
        var attributeValues = vm.Attributes.Select(a => new ArticleAttributeValue
        {
            ArticleAttributeDefinitionId = a.DefinitionId,
            BooleanValue = a.BooleanValue,
            SelectedOptionId = a.SelectedOptionId
        }).ToList();

        await _attributeRepository.SaveValuesAsync(
            id,
            attributeValues,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = "Artikel gespeichert.";
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

        // Load category
        string? categoryName = null;
        if (article.ArticleCategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(article.ArticleCategoryId.Value);
            categoryName = category?.Name;
        }

        // Load attribute values
        var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
        var attrValues = await _attributeRepository.GetValuesByArticleIdAsync(article.Id);
        var attrDisplayValues = activeDefinitions.Select(def =>
        {
            var val = attrValues.FirstOrDefault(v => v.ArticleAttributeDefinitionId == def.Id);
            string displayValue = "—";
            if (val != null)
            {
                if (def.AttributeType == AttributeType.Boolean)
                    displayValue = val.BooleanValue == true ? "Ja" : "Nein";
                else
                    displayValue = val.SelectedOption?.Value ?? "—";
            }
            return new AttributeDisplayValue { Name = def.Name, DisplayValue = displayValue };
        }).ToList();

        // Production orders containing this article (via BOM cache)
        var deviceArticleNumbers = await _bomCacheRepository.GetDeviceArticleNumbersByComponentAsync(article.ArticleNumber);
        var usedInOrders = new List<ArticleUsageItem>();
        if (deviceArticleNumbers.Count > 0)
        {
            var orders = await _productionOrderRepository.GetByArticleNumbersAsync(deviceArticleNumbers);
            usedInOrders = orders
                .Where(o => !o.IsDone)
                .OrderBy(o => o.ProductionDate)
                .Select(o => new ArticleUsageItem
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    ProductionDate = o.ProductionDate,
                    DeliveryDate = o.DeliveryDate
                })
                .ToList();
        }

        var vm = new ArticleInfoViewModel
        {
            ArticleNumber = article.ArticleNumber,
            Description = article.Description ?? string.Empty,
            Unit = article.Unit,
            ArticleGroup = article.ArticleGroup,
            ReorderLevel = article.ReorderLevel ?? 0,
            VaultUrl = $"http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Uri.EscapeDataString(article.ArticleNumber)}&searchContext=0",
            StockByLocation = stock,
            CategoryName = categoryName,
            AttributeDisplayValues = attrDisplayValues,
            UsedInOrders = usedInOrders
        };
        return View(vm);
    }
}
