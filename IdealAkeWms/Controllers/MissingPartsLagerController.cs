using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireLagerProcessingAccess]
public class MissingPartsLagerController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;

    public MissingPartsLagerController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IStockMovementRepository stock,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _stock = stock; _user = user;
    }

    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rows, total) = await _repo.GetMissingPartsAsync(
            tab, workplaceId, columnFilters, null, null, page, effectivePageSize);

        // Lagerplatz-Bestand pro Artikel bulk-fetchen (vermeidet N+1)
        var articleNumbers = rows.Select(r => r.ArticleNumber)
                                 .Where(a => !string.IsNullOrWhiteSpace(a))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();
        var stockByArticle = articleNumbers.Count > 0
            ? await _stock.GetStockByArticleNumbersAsync(articleNumbers)
            : new Dictionary<string, List<StockLocationInfo>>();

        var enrichedRows = rows.Select(r =>
        {
            if (!stockByArticle.TryGetValue(r.ArticleNumber, out var locs) || locs.Count == 0)
                return r;
            var nonZero = locs.Where(l => l.Quantity > 0)
                              .OrderByDescending(l => l.Quantity)
                              .ToList();
            if (nonZero.Count == 0) return r;
            var locStr = string.Join(", ",
                nonZero.Select(l => $"{l.Code} ({l.Quantity:N3})"));
            return r with { StorageLocations = locStr };
        }).ToList();

        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, workplaceId, null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock, workplaceId, null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = enrichedRows,
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = false,
            ActiveTab = tab,
            WaitingTotalCount = waitingResult.TotalCount,
            NoRestockTotalCount = noRestockResult.TotalCount,
            HasNoWorkplaceMapping = false,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = total
            }
        };
        return View(vm);
    }
}
