using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequirePickingOrStockAccess]
public class PartRequisitionsController : Controller
{
    private readonly IPartRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUserService;

    public PartRequisitionsController(
        IPartRequisitionRepository requisitionRepository,
        ICurrentUserService currentUserService)
    {
        _requisitionRepository = requisitionRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (deutsche Status-Badge-Texte, Datum im View-Format, "—" fuer leer).
    /// </summary>
    private static readonly Dictionary<string, Func<PartRequisitionListItem, string?>> ColumnMap = new()
    {
        ["status"] = r => r.Status switch
        {
            PartRequisitionStatus.Offen => "Offen",
            PartRequisitionStatus.Erfuellt => "Erfüllt",
            PartRequisitionStatus.Storniert => "Storniert",
            _ => r.Status
        },
        ["priority"] = r => r.Priority switch
        {
            PartRequisitionPriority.Eilt => "Eilt",
            PartRequisitionPriority.Dringend => "Dringend",
            _ => "Normal"
        },
        ["order-number"] = r => r.OrderNumber,
        ["customer"] = r => r.Customer,
        ["resource-number"] = r => r.ArticleNumber,
        ["description"] = r => r.ArticleDescription,
        ["quantity"] = r => r.Quantity.ToString("N3"),
        ["unit"] = r => r.Unit,
        ["ordered-by"] = r => r.CreatedBy,
        ["ordered-at"] = r => r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        ["email"] = r => r.EmailSentAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
        ["remark"] = r => string.IsNullOrEmpty(r.Notes) ? "—" : r.Notes,
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null, bool showAll = false, string? searchTerm = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // Server-Side-Spaltenfilter: ALLE Rows laden -> ViewModel -> filtern -> zaehlen -> paginieren.
        // (Filter muss ueber alle Eintraege wirken, nicht nur die aktuelle Seite.)
        var (allRows, _) = await _requisitionRepository.GetPagedAsync(1, int.MaxValue, showAll, searchTerm);
        var allItems = allRows.Select(r => new PartRequisitionListItem
        {
            Id = r.Id,
            Status = r.Status,
            Priority = r.Priority,
            OrderNumber = r.ProductionOrder.OrderNumber,
            ProductionOrderId = r.ProductionOrderId,
            Customer = r.ProductionOrder.Customer,
            ArticleNumber = r.ArticleNumber,
            ArticleDescription = r.ArticleDescription,
            Quantity = r.Quantity,
            Unit = r.Unit,
            CreatedBy = r.CreatedBy,
            CreatedAt = r.CreatedAt,
            EmailSentAt = r.EmailSentAt,
            Notes = r.Notes
        }).ToList();

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(allItems, columnFilters, ColumnMap).ToList();
        var totalCount = filtered.Count;
        var pagedItems = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

        var vm = new PartRequisitionIndexViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)effectivePageSize),
            TotalCount = totalCount,
            ShowAll = showAll,
            SearchTerm = searchTerm,
            Items = pagedItems,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, int page = 1, bool showAll = false)
    {
        var canPick = await _currentUserService.CanPickAsync();
        if (!canPick)
        {
            TempData["WarningMessage"] = "Keine Berechtigung zum Stornieren.";
            return RedirectToAction(nameof(Index), new { page, showAll });
        }

        await _requisitionRepository.CancelAsync(
            id,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = "Bedarfsmeldung storniert.";
        return RedirectToAction(nameof(Index), new { page, showAll });
    }
}
