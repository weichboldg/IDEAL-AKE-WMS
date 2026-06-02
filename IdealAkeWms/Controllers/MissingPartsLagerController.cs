using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class MissingPartsLagerController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsLagerController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo; _workplaces = workplaces; _user = user;
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

        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, workplaceId, null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock, workplaceId, null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
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
