using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class MissingPartsController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo;
        _workplaces = workplaces;
        _user = user;
    }

    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        int? workplaceId = null,
        bool mineOnly = false,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        // Tab-Normalisierung: None ist ungueltig fuer die Liste -> Default-Tab
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        int? effectiveWorkplaceId = workplaceId;
        List<int>? userWorkplaceIds = null;
        if (mineOnly)
        {
            var userId = _user.GetCurrentAppUserId() ?? 0;
            var userWorkplaces = await _workplaces.GetByUserIdAsync(userId);
            userWorkplaceIds = userWorkplaces.Select(w => w.Id).ToList();
            if (workplaceId.HasValue && !userWorkplaceIds.Contains(workplaceId.Value))
                effectiveWorkplaceId = -1;
            else if (!workplaceId.HasValue && userWorkplaceIds.Count == 1)
                effectiveWorkplaceId = userWorkplaceIds[0];
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rawRows, total) = await _repo.GetMissingPartsAsync(
            tab,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            columnFilters,
            null, null, page, effectivePageSize);

        IReadOnlyList<MissingPartRow> rows = rawRows;
        if (mineOnly && effectiveWorkplaceId == null && userWorkplaceIds != null)
        {
            var allWp = await _workplaces.GetAllAsync();
            var allowedNames = allWp.Where(w => userWorkplaceIds.Contains(w.Id))
                                    .Select(w => w.Name).ToHashSet();
            var filtered = rawRows.Where(r => allowedNames.Contains(r.WorkplaceName)).ToList();
            rows = filtered;
            total = filtered.Count;
        }
        else if (mineOnly && effectiveWorkplaceId == -1)
        {
            rows = new List<MissingPartRow>();
            total = 0;
        }

        // Counts fuer beide Tabs (Tab-Header-Badges)
        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = mineOnly,
            ActiveTab = tab,
            WaitingTotalCount = waitingResult.TotalCount,
            NoRestockTotalCount = noRestockResult.TotalCount,
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
