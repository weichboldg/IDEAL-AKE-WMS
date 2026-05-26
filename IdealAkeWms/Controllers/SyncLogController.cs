using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class SyncLogController : Controller
{
    private static readonly string[] KnownServices = new[] { "Lagerplatz", "Lagerbestand", "OseonTracking", "Article", "ProductionOrder", "EnaioDms", "BomCache", "Holiday" };

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

        var (rows, totalCount) = await _repo.GetPagedAsync(service, level, reference, page, effectivePageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filteredRows = ColumnFilterHelper.Apply(rows, columnFilters, SyncLogColumnMap).ToList();

        return View(new SyncLogIndexViewModel
        {
            Entries = filteredRows,
            FilterService = service,
            FilterLevel = level,
            FilterReference = reference,
            AvailableServices = KnownServices.ToList(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount,
            },
        });
    }

    private static readonly Dictionary<string, Func<SyncLog, string?>> SyncLogColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Timestamp"] = e => e.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"),
            ["Service"] = e => e.Service,
            ["Level"] = e => e.Level,
            ["Reference"] = e => e.Reference,
            ["Message"] = e => e.Message,
        };
}
