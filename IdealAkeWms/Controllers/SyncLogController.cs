using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class SyncLogController : Controller
{
    // v1.70.0: Auswahlfeld alphabetisch nach Anzeigename (Antwort 1).
    private static readonly IReadOnlyList<string> KnownServices =
        SyncLogServices.All.OrderBy(SyncLogServices.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

    private readonly ISyncLogRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public SyncLogController(ISyncLogRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        string? service,
        string? level,
        string? reference,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var (rows, totalCount) = await _repo.GetPagedAsync(service, level, reference, page, effectivePageSize, columnFilters);

        return View(new SyncLogIndexViewModel
        {
            Entries = rows,
            FilterService = service,
            FilterLevel = level,
            FilterReference = reference,
            AvailableServices = KnownServices.ToList(),
            Pagination = new PaginationState
            {
                CurrentPage = Math.Max(page, 1),
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount,
            },
        });
    }
}
