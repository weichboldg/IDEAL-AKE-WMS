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

    public async Task<IActionResult> Index(int? workplaceId, bool mineOnly = false,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
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
            {
                effectiveWorkplaceId = -1;   // inkonsistent -> leer
            }
            else if (!workplaceId.HasValue && userWorkplaceIds.Count == 1)
            {
                effectiveWorkplaceId = userWorkplaceIds[0];
            }
            // Bei mehreren User-Workplaces ohne expliziten Filter: in-memory filtern (siehe unten)
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rawRows, total) = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock,
            effectiveWorkplaceId == -1 ? null : effectiveWorkplaceId,
            columnFilters,
            null, null, page, effectivePageSize);

        IReadOnlyList<MissingPartRow> rows = rawRows;
        if (mineOnly && effectiveWorkplaceId == -1)
        {
            rows = new List<MissingPartRow>();
            total = 0;
        }
        else if (mineOnly && effectiveWorkplaceId == null && userWorkplaceIds != null)
        {
            // mehrere eigene Werkbaenke: in-memory filter
            var allWp = await _workplaces.GetAllAsync();
            var allowedNames = allWp.Where(w => userWorkplaceIds.Contains(w.Id))
                                    .Select(w => w.Name).ToHashSet();
            var filtered = rawRows.Where(r => allowedNames.Contains(r.WorkplaceName)).ToList();
            rows = filtered;
            total = filtered.Count;
        }

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = mineOnly,
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
