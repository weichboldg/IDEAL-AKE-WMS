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
    private readonly IPlannedConsumptionService _plannedConsumption;
    private readonly IStorageLocationRepository _storageLocationRepository;
    private readonly ICreditorRepository _creditorRepository;
    private readonly IAppSettingRepository _appSettings;

    public ArticlesController(
        IArticleRepository articleRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IArticleAttributeRepository attributeRepository,
        IArticleCategoryRepository categoryRepository,
        IPlannedConsumptionService plannedConsumption,
        IStorageLocationRepository storageLocationRepository,
        ICreditorRepository creditorRepository,
        IAppSettingRepository appSettings)
    {
        _appSettings = appSettings;
        _articleRepository = articleRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _attributeRepository = attributeRepository;
        _categoryRepository = categoryRepository;
        _plannedConsumption = plannedConsumption;
        _storageLocationRepository = storageLocationRepository;
        _creditorRepository = creditorRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null, string? search = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        // v1.99.72 F15: Schalter aus = Spalte und Filter „Fräsen-Pfad“ gibt es nicht (auch kein manipulierter colf).
        ViewBag.FraesenPfadAktiv = await RequireFraesenPfadAktivFilter.IstAktivAsync(_appSettings);
        if (ViewBag.FraesenPfadAktiv != true) columnFilters.Remove("fraesen-pfad");
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
            LieferantTexte = await _articleRepository.GetLieferantTexteAsync(articleIds),
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
                TextValue = existing?.TextValue,
                IsSageSynced = def.IsSageSynced,
                Options = def.Options.ToList()
            };
        }).ToList();

        var storageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();

        var vm = new ArticleEditViewModel
        {
            Article = article,
            Categories = categories,
            Attributes = attributeItems,
            StorageLocations = storageLocations,
            IsPrimarySageControlled = !string.IsNullOrWhiteSpace(article.SagePrimaryStorageLocation),
            SageKarten = await SageKartenAsync(article.Id),
        };
        await FraesenPfadFlagsAsync();

        return View(vm);
    }

    /// <summary>v1.99.72 (F13/F14): Schalter und Pflegerecht fuer Artikelinfo/„Artikel bearbeiten“ (Baustein + Pflegeformular).</summary>
    private async Task FraesenPfadFlagsAsync()
    {
        ViewBag.FraesenPfadAktiv = await RequireFraesenPfadAktivFilter.IstAktivAsync(_appSettings);
        ViewBag.FraesenPfadPflegen = ViewBag.FraesenPfadAktiv == true && await _currentUserService.CanPflegeFraesenPfadAsync();
    }

    /// <summary>
    /// v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F14): EINZIGER Schreibweg des Fraesen-Pfads. Schreibt nur
    /// <c>CamPath</c> + Modified* (der Edit-POST fasst <c>CamPath</c> nie an). Pruefregel <see cref="Helpers.FraesenPfad.Pruefen"/>.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireFraesenPfadAktiv]
    [RequireFraesenPfadPflegenAccess]
    public async Task<IActionResult> FraesenPfad(string articleNumber, string? camPath, string? returnUrl)
    {
        var nr = articleNumber?.Trim() ?? string.Empty;
        var article = nr.Length == 0 ? null : await _articleRepository.GetByArticleNumberAsync(nr);
        if (article == null)
            TempData["WarningMessage"] = $"Artikel {nr} nicht gefunden – Pfad nicht gespeichert.";
        else
        {
            var (wert, fehler) = Helpers.FraesenPfad.Pruefen(camPath);
            if (fehler != null)
                TempData["WarningMessage"] = $"Fräsen-Pfad nicht gespeichert: {fehler}";
            else
            {
                article.CamPath = wert;
                article.ModifiedAt = DateTime.Now;
                article.ModifiedBy = _currentUserService.GetDisplayName();
                article.ModifiedByWindows = _currentUserService.GetWindowsUserName();
                await _articleRepository.UpdateAsync(article);
                TempData["SuccessMessage"] = "Fräsen-Pfad gespeichert.";
            }
        }
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Info), new { articleNumber = article?.ArticleNumber ?? nr });
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
            vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
            var sageCheck = await _articleRepository.GetByIdAsync(id);
            vm.IsPrimarySageControlled = !string.IsNullOrWhiteSpace(sageCheck?.SagePrimaryStorageLocation);
            vm.SageKarten = await SageKartenAsync(id);
            var activeDefinitions = await _attributeRepository.GetActiveDefinitionsOrderedAsync();
            // Re-fill options for dropdown attributes
            foreach (var attr in vm.Attributes)
            {
                var def = activeDefinitions.FirstOrDefault(d => d.Id == attr.DefinitionId);
                if (def != null)
                {
                    attr.Name = def.Name;
                    attr.AttributeType = def.AttributeType;
                    attr.IsSageSynced = def.IsSageSynced;
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
        existing.Matchcode = vm.Article.Matchcode;
        existing.Unit = vm.Article.Unit;
        existing.ReorderLevel = vm.Article.ReorderLevel;
        existing.ArticleCategoryId = vm.Article.ArticleCategoryId;

        // POST-Guard: Sage-kontrollierter Hauptlagerplatz ist read-only — eingehende Id ignorieren.
        var isSagePrimary = !string.IsNullOrWhiteSpace(existing.SagePrimaryStorageLocation);
        if (!isSagePrimary)
        {
            existing.PrimaryStorageLocationId = vm.Article.PrimaryStorageLocationId;
        }

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
        // Kein Treffer über Artikelnummer: exakt (kein Teilstring) über Matchcode nachschlagen (D4). Mehrdeutig: Meldung statt
        // still des ersten Treffers (v1.68.0).
        if (article == null)
        {
            var treffer = await _articleRepository.GetByMatchcodeExactAsync(articleNumber.Trim(), 6);
            if (treffer.Count > 1)
            {
                ViewBag.Mehrdeutig = $"Matchcode „{articleNumber.Trim()}“ ist mehrdeutig ({(treffer.Count > 5 ? "mehr als 5" : treffer.Count.ToString())} Artikel: "
                    + string.Join(", ", treffer.Take(5).Select(a => a.ArticleNumber)) + (treffer.Count > 5 ? ", …" : "") + ") – bitte die Artikelnummer eingeben.";
                return View((ArticleInfoViewModel?)null);
            }
            article = treffer.FirstOrDefault();
        }
        if (article == null)
        {
            ViewBag.NotFound = articleNumber.Trim();
            return View((ArticleInfoViewModel?)null);
        }

        // filterArticle ist eine Teiltextsuche (auch Bezeichnung/Matchcode) – nur Zeilen des gefundenen Artikels zählen (v1.99.45).
        var stock = (await _stockMovementRepository.GetCurrentStockAsync(
            filterArticle: article.ArticleNumber)).Where(s => s.ArticleId == article.Id).ToList();

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
                displayValue = def.AttributeType switch
                {
                    AttributeType.Boolean => val.BooleanValue == true ? "Ja" : "Nein",
                    AttributeType.Text => val.TextValue ?? "—",
                    _ => val.SelectedOption?.Value ?? "—",
                };
            }
            return new AttributeDisplayValue { Name = def.Name, DisplayValue = displayValue };
        }).ToList();

        // v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F4, Antwort 7): Bedarfe aus demselben Service wie die Bestellvorschlaege –
        // FA-Rest (BOM − Ausbuchungen), max je FA mit Bedarfsmeldungen, max je Artikel mit Lagerbestellungen (Einstellung EinkaufOffeneBedarfe).
        var bedarfe = await _plannedConsumption.GetJeArtikelAsync(new[] { article });
        var hierarchical = bedarfe.Hierarchisch;
        var bedarf = bedarfe.Fuer(article.ArticleNumber);
        // Flach (AKE) nach Fertigungstermin; hierarchisch nach HauptFA + Sub-FA, damit die Sub-FAs eines Auftrags beisammen stehen.
        var ausStueckliste = bedarf.Fas.Where(f => f.AusStueckliste);
        var usedInOrders = (hierarchical
                ? ausStueckliste.OrderBy(f => f.OrderNumber).ThenBy(f => f.SubOrderNumber)
                : ausStueckliste.OrderBy(f => f.ProductionDate))
            .Select(f => new ArticleUsageItem
            {
                Id = f.ProductionOrderId, OrderNumber = f.OrderNumber, ProductionDate = f.ProductionDate, DeliveryDate = f.DeliveryDate,
                SubOrderNumber = f.SubOrderNumber, Soll = f.Soll, Rest = f.Rest,
            })
            .ToList();

        var vm = new ArticleInfoViewModel
        {
            ArticleNumber = article.ArticleNumber,
            Description = article.Description ?? string.Empty,
            Matchcode = article.Matchcode,
            Unit = article.Unit,
            ArticleGroup = article.ArticleGroup,
            ReorderLevel = article.ReorderLevel ?? 0,
            StockByLocation = stock,
            Bedarf = bedarf,
            BedarfeMitLagerbestellungen = bedarfe.MitLagerbestellungen,
            BedarfeMitBedarfsmeldungen = bedarfe.MitBedarfsmeldungen,
            CategoryName = categoryName,
            AttributeDisplayValues = attrDisplayValues,
            UsedInOrders = usedInOrders,
            Hierarchical = hierarchical,
            CamPath = article.CamPath,
        };
        await FraesenPfadFlagsAsync();
        var karten = await SageKartenAsync(article.Id);
        vm.SageDetail = karten.SageDetail;
        vm.Lieferanten = karten.Lieferanten;
        vm.HauptlieferantKreditor = karten.HauptlieferantKreditor;
        vm.LetzterLieferantKreditor = karten.LetzterLieferantKreditor;
        return View(vm);
    }

    /// <summary>
    /// v1.80.0 F1 / v1.85.0 Nachbesserung 1: Sage-Satellit, Artikel-Lieferanten und Haupt-/letzter Lieferant (EIN Kreditor-Aufruf) fuer
    /// Artikelinfo und „Artikel bearbeiten“. Der Satellit schreibt Sage-Werte ungetrimmt (ArticleSageDetailsWriter) -> Trim hier;
    /// Gross/Klein egal wie der Artikel-Lieferanten-Abgleich.
    /// </summary>
    private async Task<ArtikelSageKartenViewModel> SageKartenAsync(int articleId)
    {
        var k = new ArtikelSageKartenViewModel
        {
            SageDetail = await _articleRepository.GetSageDetailAsync(articleId),
            Lieferanten = await _articleRepository.GetSuppliersAsync(articleId),
        };
        var haupt = k.SageDetail?.Hauptlieferant?.Trim();
        var letzter = k.SageDetail?.LetzterLieferant?.Trim();
        var konten = new[] { haupt, letzter }.Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (konten.Count > 0)
        {
            var kreditoren = (await _creditorRepository.GetBySageKtosAsync(konten))
                .Where(c => c.SageKto != null)
                .GroupBy(c => c.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            k.HauptlieferantKreditor = haupt != null ? kreditoren.GetValueOrDefault(haupt) : null;
            k.LetzterLieferantKreditor = letzter != null ? kreditoren.GetValueOrDefault(letzter) : null;
        }
        return k;
    }
}
