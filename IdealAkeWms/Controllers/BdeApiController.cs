using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Controllers;

[RequireBdeActive]
[ApiController]
[Route("api/bde")]
[RequireBdeUserAccess]
public class BdeApiController : ControllerBase
{
    private readonly IBdeOperatorRepository _ops;
    private readonly IBdeActivityRepository _activities;
    private readonly IBdeBookingRepository _bookings;
    private readonly IWorkOperationRepository _workOps;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IAppSettingRepository _settings;
    private readonly ApplicationDbContext _ctx;

    public BdeApiController(IBdeOperatorRepository ops, IBdeActivityRepository activities,
        IBdeBookingRepository bookings, IWorkOperationRepository workOps,
        IProductionWorkplaceRepository workplaces, IAppSettingRepository settings,
        ApplicationDbContext ctx)
    {
        _ops = ops;
        _activities = activities;
        _bookings = bookings;
        _workOps = workOps;
        _workplaces = workplaces;
        _settings = settings;
        _ctx = ctx;
    }

    [HttpGet("operator/{personnelNumber}")]
    public async Task<IActionResult> GetOperator(string personnelNumber)
    {
        var op = await _ops.GetByPersonnelNumberAsync(personnelNumber);
        if (op == null) return NotFound();
        return Ok(new { id = op.Id, displayName = op.DisplayName, personnelNumber = op.PersonnelNumber });
    }

    [HttpGet("workoperation")]
    public async Task<IActionResult> GetWorkOperation([FromQuery] string faNumber, [FromQuery] string opNumber)
    {
        // FA-Nummer kann Komma-Suffix haben (QR-Code-Konvention) -> splitten
        faNumber = faNumber?.Split(',')[0].Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(faNumber) || string.IsNullOrEmpty(opNumber)) return BadRequest();
        var wo = await _workOps.GetByFaAndOperationAsync(faNumber, opNumber);
        if (wo == null) return NotFound();
        return Ok(new
        {
            id = wo.Id,
            operationNumber = wo.OperationNumber,
            name = wo.Name,
            orderNumber = wo.ProductionOrder.OrderNumber,
            articleNumber = wo.ProductionOrder.ArticleNumber,
            description = wo.ProductionOrder.Description1,
            workplaceId = wo.ProductionWorkplaceId,
            workplaceName = wo.ProductionWorkplace?.Name
        });
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities() =>
        Ok((await _activities.GetAllActiveAsync()).Select(a => new { id = a.Id, code = a.Code, name = a.Name }));

    [HttpGet("operator/{id:int}/active-booking")]
    public async Task<IActionResult> GetActiveBooking(int id)
    {
        var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        var b = await _bookings.GetActiveForOperatorAsync(id);
        if (b == null) return Ok(new { booking = (object?)null, nurFaMode = nurFa });
        var target = b.WorkOperation != null
            ? $"{b.WorkOperation.ProductionOrder.OrderNumber}/{b.WorkOperation.OperationNumber} — {b.WorkOperation.Name}"
            : b.BdeActivity?.Name;
        return Ok(new
        {
            booking = new
            {
                id = b.Id,
                bookingType = b.BookingType.ToString(),
                status = b.Status.ToString(),
                startedAt = b.StartedAt,
                workOperationId = b.WorkOperationId,
                bdeActivityId = b.BdeActivityId,
                target,
                workplaceName = b.ProductionWorkplace?.Name,
                nurFaMode = nurFa
            }
        });
    }

    [HttpGet("operator/{id:int}/today-history")]
    public async Task<IActionResult> GetOperatorTodayHistory(int id)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var bookings = await _bookings.GetHistoryAsync(0, 50, operatorId: id, workplaceId: null, from: today, to: tomorrow);
        var items = bookings.Select(b => new
        {
            id = b.Id,
            bookingType = b.BookingType.ToString(),
            status = b.Status.ToString(),
            startedAt = b.StartedAt,
            endedAt = b.EndedAt,
            target = b.WorkOperation != null
                ? $"{b.WorkOperation.ProductionOrder.OrderNumber}/{b.WorkOperation.OperationNumber} — {b.WorkOperation.Name}"
                : b.BdeActivity?.Name,
            totalGood = b.Quantities.Sum(q => q.GoodQuantity),
            totalScrap = b.Quantities.Sum(q => q.ScrapQuantity),
            durationMinutes = b.EndedAt.HasValue
                ? (int)(b.EndedAt.Value - b.StartedAt).TotalMinutes
                : (int)(DateTime.Now - b.StartedAt).TotalMinutes
        });
        return Ok(items);
    }

    [HttpGet("cockpit")]
    [RequireBdeShiftleadAccess]
    public async Task<IActionResult> GetCockpit()
    {
        var workplaces = await _workplaces.GetBdeActiveAsync();
        var activeBookings = await _bookings.GetActiveCockpitAsync();

        var tiles = workplaces.Select(wp =>
        {
            var bookingsAtWp = activeBookings.Where(x => x.ProductionWorkplaceId == wp.Id).ToList();
            return new
            {
                workplaceId = wp.Id,
                workplaceName = wp.Name,
                status = bookingsAtWp.Any() ? "Active" : "Idle",
                bookings = bookingsAtWp.Select(b => new
                {
                    bookingType = b.BookingType.ToString(),
                    operatorName = b.BdeOperator.DisplayName,
                    orderNumber = b.WorkOperation?.ProductionOrder.OrderNumber,
                    operationNumber = b.WorkOperation?.OperationNumber,
                    operationName = b.WorkOperation?.Name,
                    activityName = b.BdeActivity?.Name,
                    startedAt = b.StartedAt,
                    runtimeSeconds = (int)(DateTime.Now - b.StartedAt).TotalSeconds
                })
            };
        });

        return Ok(new { workplaces = tiles, serverTime = DateTime.Now });
    }

    [HttpGet("available-operations/{workplaceId:int}")]
    public async Task<IActionResult> GetAvailableOperations(int workplaceId)
    {
        var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
        if (workplace == null || !workplace.BdeAktiv)
        {
            return Ok(new { productive = Array.Empty<object>(), unplanned = Array.Empty<object>(), nurFaMode = false });
        }

        var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (nurFa)
        {
            // Im NurFA-Modus: offene ProductionOrders an dieser Werkbank
            var orders = await _ctx.ProductionOrders
                .Where(po => po.ProductionWorkplaceId == workplaceId && !po.IsDone
                    && !_ctx.BdeBookingQuantities.Any(q =>
                        q.IsFinal
                        && q.BdeBooking!.WorkOperation!.ProductionOrderId == po.Id
                        && !q.BdeBooking.IsCancelled))
                .OrderBy(po => po.ProductionDate)
                .Select(po => new
                {
                    id = po.Id,
                    label = $"{po.OrderNumber} — {po.Description1}",
                    type = "fa"
                })
                .ToListAsync();

            return Ok(new { productive = orders, unplanned = Array.Empty<object>(), nurFaMode = true });
        }

        // Open WorkOperations at this workplace (not reported, not already in active booking)
        var workOps = await _workOps.GetOpenByWorkplaceIdAsync(workplaceId);
        var activeWoIds = (await _bookings.GetActiveCockpitAsync())
            .Where(b => b.WorkOperationId.HasValue)
            .Select(b => b.WorkOperationId!.Value)
            .ToHashSet();

        // Ermittle WorkOperation-IDs, die bereits als fertig gemeldet sind (IsFinal=true, nicht storniert)
        var finishedWoIds = await _ctx.BdeBookingQuantities
            .Where(q => q.IsFinal && !q.BdeBooking!.IsCancelled && q.BdeBooking.WorkOperationId.HasValue)
            .Select(q => q.BdeBooking!.WorkOperationId!.Value)
            .Distinct()
            .ToListAsync();
        var finishedWoIdSet = finishedWoIds.ToHashSet();

        var productive = workOps
            .Where(wo => !activeWoIds.Contains(wo.Id) && !finishedWoIdSet.Contains(wo.Id))
            .Select(wo => new
            {
                id = wo.Id,
                label = $"{wo.ProductionOrder.OrderNumber} / {wo.OperationNumber} — {wo.Name}",
                type = "productive"
            });

        // Unplanned activities
        var activities = (await _activities.GetAllActiveAsync())
            .Select(a => new { id = a.Id, label = $"{a.Code} — {a.Name}", type = "unplanned" });

        return Ok(new { productive, unplanned = activities, nurFaMode = false });
    }

    [HttpGet("workoperation/{id:int}/latest-paused")]
    public async Task<IActionResult> GetLatestPaused(int id)
    {
        var b = await _bookings.GetLatestPausedForWorkOperationAsync(id);
        if (b == null) return NotFound();
        return Ok(new { id = b.Id, bookingType = b.BookingType.ToString(), operatorName = b.BdeOperator.DisplayName });
    }
}
