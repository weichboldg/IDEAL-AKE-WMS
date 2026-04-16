using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireBdeActive]
[RequireBdeShiftleadAccess]
public class BdeBookingsController : Controller
{
    private readonly IBdeBookingRepository _repo;
    private readonly IBdeOperatorRepository _ops;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _userSvc;

    public BdeBookingsController(
        IBdeBookingRepository repo,
        IBdeOperatorRepository ops,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService userSvc)
    {
        _repo = repo;
        _ops = ops;
        _workplaces = workplaces;
        _userSvc = userSvc;
    }

    public async Task<IActionResult> Index(int skip = 0, int take = 50, int? operatorId = null, int? workplaceId = null, DateTime? from = null, DateTime? to = null)
    {
        // Default to today if no date filter provided
        if (!from.HasValue && !to.HasValue)
        {
            from = DateTime.Today;
            to = DateTime.Today.AddDays(1);
        }

        var list = await _repo.GetHistoryAsync(skip, take, operatorId, workplaceId, from, to);
        var vms = list.Select(b => new BdeBookingListViewModel
        {
            Id = b.Id,
            OperatorName = b.BdeOperator.DisplayName,
            WorkplaceName = b.ProductionWorkplace.Name,
            BookingType = b.BookingType.ToString(),
            Status = b.Status.ToString(),
            StartedAt = b.StartedAt,
            EndedAt = b.EndedAt,
            Target = b.WorkOperation != null
                ? $"{b.WorkOperation.ProductionOrder.OrderNumber}/{b.WorkOperation.OperationNumber}"
                : b.BdeActivity?.Name,
            TotalGood = b.Quantities.Sum(q => q.GoodQuantity),
            TotalScrap = b.Quantities.Sum(q => q.ScrapQuantity),
            IsCancelled = b.IsCancelled
        }).ToList();

        ViewBag.Operators = await _ops.GetAllActiveAsync();
        ViewBag.Workplaces = await _workplaces.GetAllOrderedAsync();
        ViewBag.Filter = new { skip, take, operatorId, workplaceId, from, to };

        return View(vms);
    }

    [HttpGet("BdeBookings/Edit/{id:int}")]
    [RequireBdeAdminAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var b = await _repo.GetByIdAsync(id);
        if (b == null) return NotFound();

        var vm = new BdeBookingEditViewModel
        {
            Id = b.Id,
            BdeOperatorId = b.BdeOperatorId,
            ProductionWorkplaceId = b.ProductionWorkplaceId,
            StartedAt = b.StartedAt,
            EndedAt = b.EndedAt,
            Status = b.Status,
            Quantities = b.Quantities.Select(q => new BdeBookingQuantityEditRow
            {
                Id = q.Id,
                GoodQuantity = q.GoodQuantity,
                ScrapQuantity = q.ScrapQuantity,
                IsFinal = q.IsFinal,
                ReportedAt = q.ReportedAt,
                Delete = false
            }).ToList()
        };

        await LoadEditSelectLists();
        return View(vm);
    }

    [HttpPost("BdeBookings/Edit/{id:int}")]
    [RequireBdeAdminAccess]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BdeBookingEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadEditSelectLists();
            return View(vm);
        }

        var b = await _repo.GetByIdAsync(id);
        if (b == null) return NotFound();

        b.BdeOperatorId = vm.BdeOperatorId;
        b.ProductionWorkplaceId = vm.ProductionWorkplaceId;
        b.StartedAt = vm.StartedAt;
        b.EndedAt = vm.EndedAt;
        b.Status = vm.Status;
        b.ModifiedAt = DateTime.Now;
        b.ModifiedBy = _userSvc.GetDisplayName();
        b.ModifiedByWindows = _userSvc.GetWindowsUserName();

        foreach (var row in vm.Quantities)
        {
            var q = b.Quantities.FirstOrDefault(x => x.Id == row.Id);
            if (q == null) continue;
            if (row.Delete)
            {
                b.Quantities.Remove(q);
                continue;
            }
            q.GoodQuantity = row.GoodQuantity;
            q.ScrapQuantity = row.ScrapQuantity;
            q.IsFinal = row.IsFinal;
            q.ReportedAt = row.ReportedAt;
            q.ModifiedAt = DateTime.Now;
            q.ModifiedBy = _userSvc.GetDisplayName();
            q.ModifiedByWindows = _userSvc.GetWindowsUserName();
        }

        await _repo.UpdateAsync(b);
        TempData["SuccessMessage"] = "Buchung aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireBdeAdminAccess]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason)
    {
        var b = await _repo.GetByIdAsync(id);
        if (b == null) return NotFound();
        b.IsCancelled = true;
        b.CancellationReason = reason;
        b.ModifiedAt = DateTime.Now;
        b.ModifiedBy = _userSvc.GetDisplayName();
        b.ModifiedByWindows = _userSvc.GetWindowsUserName();
        await _repo.UpdateAsync(b);
        TempData["SuccessMessage"] = "Buchung storniert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireBdeAdminAccess]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualClose(int id, DateTime endedAt)
    {
        var b = await _repo.GetByIdAsync(id);
        if (b == null) return NotFound();
        if (b.EndedAt != null)
        {
            TempData["WarningMessage"] = "Buchung bereits beendet.";
            return RedirectToAction(nameof(Index));
        }
        b.EndedAt = endedAt;
        b.Status = BdeBookingStatus.Finished;
        b.ModifiedAt = DateTime.Now;
        b.ModifiedBy = _userSvc.GetDisplayName();
        b.ModifiedByWindows = _userSvc.GetWindowsUserName();
        await _repo.UpdateAsync(b);
        TempData["SuccessMessage"] = "Buchung manuell geschlossen.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadEditSelectLists()
    {
        ViewBag.Operators = await _ops.GetAllAsync();
        ViewBag.Workplaces = await _workplaces.GetAllOrderedAsync();
    }
}
