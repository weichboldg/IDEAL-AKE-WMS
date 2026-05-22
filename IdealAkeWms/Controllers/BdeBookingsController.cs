using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdealAkeWms.Data;
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
    private readonly IBdeTimeSplitService _timeSplitSvc;
    private readonly ApplicationDbContext _ctx;

    public BdeBookingsController(
        IBdeBookingRepository repo,
        IBdeOperatorRepository ops,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService userSvc,
        IBdeTimeSplitService timeSplitSvc,
        ApplicationDbContext ctx)
    {
        _repo = repo;
        _ops = ops;
        _workplaces = workplaces;
        _userSvc = userSvc;
        _timeSplitSvc = timeSplitSvc;
        _ctx = ctx;
    }

    public async Task<IActionResult> Index(
        int page = 1, int? pageSize = null,
        int? operatorId = null, int? workplaceId = null, DateTime? from = null, DateTime? to = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _userSvc.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);
        var skip = (page - 1) * effectivePageSize;

        // Default to today if no date filter provided
        if (!from.HasValue && !to.HasValue)
        {
            from = DateTime.Today;
            to = DateTime.Today.AddDays(1);
        }

        var list = await _repo.GetHistoryAsync(skip, effectivePageSize, operatorId, workplaceId, from, to);
        var bookingVms = list.Select(b => new BdeBookingListViewModel
        {
            Id = b.Id,
            BdeOperatorId = b.BdeOperatorId,
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

        var vm = new BdeBookingsIndexViewModel { Bookings = bookingVms };

        // Compute effective durations for all visible bookings
        var operatorDays = new HashSet<(int OperatorId, DateTime Day)>();
        foreach (var b in vm.Bookings)
        {
            var startDay = b.StartedAt.Date;
            var endDay = (b.EndedAt ?? DateTime.Now).Date;
            for (var d = startDay; d <= endDay; d = d.AddDays(1))
                operatorDays.Add((b.BdeOperatorId, d));
        }

        foreach (var (opId, day) in operatorDays)
        {
            var splits = await _timeSplitSvc.ComputeForOperatorDayAsync(opId, day);
            foreach (var s in splits)
            {
                if (!vm.EffectiveDurations.ContainsKey(s.BookingId))
                    vm.EffectiveDurations[s.BookingId] = TimeSpan.Zero;
                vm.EffectiveDurations[s.BookingId] += s.EffectiveDuration;
            }
        }

        // Fuer Terminal-Rows (keine weiteren Children): kumulative Zeit statt eigene Zeit
        var visibleIds = vm.Bookings.Select(b => b.Id).ToList();
        var parentIdsWithChildren = await _ctx.BdeBookings
            .AsNoTracking()
            .Where(b => b.ParentBookingId != null && visibleIds.Contains(b.ParentBookingId.Value))
            .Select(b => b.ParentBookingId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var b in vm.Bookings)
        {
            if (!parentIdsWithChildren.Contains(b.Id))
            {
                // Terminal-Row: kumulative Zeit (eigene + alle Vorlaeufer)
                var cumulative = await _timeSplitSvc.ComputeCumulativeEffectiveDurationAsync(b.Id);
                vm.EffectiveDurations[b.Id] = cumulative;
            }
            // else: Vorlaeufer-Row behaelt seine eigene Zeit (bereits im Dict)
        }

        ViewBag.Operators = await _ops.GetAllActiveAsync();
        ViewBag.Workplaces = await _workplaces.GetBdeActiveAsync();
        ViewBag.Filter = new { operatorId, workplaceId, from, to };

        // Total fuer Pagination (separater Call)
        var totalCount = await _repo.GetHistoryCountAsync(operatorId, workplaceId, from, to);
        vm.Pagination = new PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = totalCount
        };

        return View(vm);
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

        ViewBag.EffectiveDuration = await _timeSplitSvc.ComputeEffectiveDurationAsync(b.Id);

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
        ViewBag.Workplaces = await _workplaces.GetBdeActiveAsync();
    }
}
