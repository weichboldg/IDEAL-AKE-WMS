using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockAccess]
public class WarehousePickingController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;

    public WarehousePickingController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IStockMovementRepository stock,
        ICurrentUserService user)
    {
        _repo = repo;
        _workplaces = workplaces;
        _stock = stock;
        _user = user;
    }

    public async Task<IActionResult> Index(WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page = 1)
    {
        const int pageSize = 25;
        var effectiveFilter = statusFilter ?? WarehouseRequisitionStatus.Submitted;
        var (items, total) = await _repo.GetForWarehouseAsync(effectiveFilter, workplaceId, page, pageSize);
        var allWorkplaces = await _workplaces.GetAllAsync();
        var openCount = (await _repo.GetForWarehouseAsync(WarehouseRequisitionStatus.Submitted, null, 1, 1)).TotalCount;

        var vm = new WarehouseRequisitionListViewModel
        {
            Items = items.Select(r => new WarehouseRequisitionListItemViewModel(
                r.Id, r.ProductionWorkplace?.Name ?? "", r.CreatedBy, r.CreatedAt,
                r.SubmittedAt, r.Items.Count, r.Status)).ToList(),
            TotalCount = total,
            CurrentPage = page,
            PageSize = pageSize,
            StatusFilter = statusFilter,
            WorkplaceFilter = workplaceId,
            AvailableWorkplaces = allWorkplaces.OrderBy(w => w.Name).ToList(),
            OpenCount = openCount
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var stock = await _stock.GetCurrentStockAsync(filterArticle: i.ArticleNumber);
            var locationStr = string.Join(", ", stock.Where(s => s.CurrentQuantity > 0)
                .Select(s => $"{s.StorageLocationCode} ({s.CurrentQuantity:N3})"));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr));
        }

        var vm = new WarehouseRequisitionDetailViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            ClosedAt = r.ClosedAt,
            CancelledAt = r.CancelledAt,
            CancellationReason = r.CancellationReason,
            Status = r.Status,
            RowVersion = r.RowVersion,
            Items = detailItems
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int[] itemIds, decimal[] quantitiesPicked, byte[] rowVersion)
    {
        if (quantitiesPicked.Any(q => q < 0m))
        {
            TempData["WarningMessage"] = "Ist-Mengen duerfen nicht negativ sein.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dict = new Dictionary<int, decimal>();
        for (int idx = 0; idx < itemIds.Length; idx++)
            dict[itemIds[idx]] = idx < quantitiesPicked.Length ? quantitiesPicked[idx] : 0m;

        try
        {
            await _repo.CloseAsync(id, dict, _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} abgeschlossen.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, byte[] rowVersion)
    {
        try
        {
            await _repo.CancelAsync(id, reason, _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Print(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var stock = await _stock.GetCurrentStockAsync(filterArticle: i.ArticleNumber);
            var locationStr = string.Join(", ", stock.Where(s => s.CurrentQuantity > 0)
                .Select(s => $"{s.StorageLocationCode} ({s.CurrentQuantity:N3})"));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr));
        }
        var vm = new WarehouseRequisitionDetailViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            Status = r.Status,
            Items = detailItems
        };
        return View(vm);
    }
}
