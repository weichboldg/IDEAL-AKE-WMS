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

    public async Task<IActionResult> Index(int page = 1, bool showAll = false, string? searchTerm = null)
    {
        const int pageSize = 25;
        var (items, totalCount) = await _requisitionRepository.GetPagedAsync(page, pageSize, showAll, searchTerm);

        var vm = new PartRequisitionIndexViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalCount = totalCount,
            ShowAll = showAll,
            SearchTerm = searchTerm,
            Items = items.Select(r => new PartRequisitionListItem
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
            }).ToList()
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
