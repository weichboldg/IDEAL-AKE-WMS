using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class SyncLogController : Controller
{
    private const int PageSize = 200;
    private static readonly string[] KnownServices = new[] { "Lagerplatz", "OseonTracking", "Article", "ProductionOrder", "EnaioDms", "BomCache", "Holiday" };

    private readonly ISyncLogRepository _repo;

    public SyncLogController(ISyncLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<IActionResult> Index(string? service, string? level, string? reference)
    {
        var entries = await _repo.GetRecentAsync(service, level, PageSize);
        if (!string.IsNullOrWhiteSpace(reference))
            entries = entries.Where(e => e.Reference != null && e.Reference.Contains(reference, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new SyncLogIndexViewModel
        {
            Entries = entries,
            FilterService = service,
            FilterLevel = level,
            FilterReference = reference,
            AvailableServices = KnownServices.ToList()
        });
    }
}
