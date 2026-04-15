using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[ApiController]
[Route("api/bde")]
[RequireBdeUserAccess]
public class BdeApiController : ControllerBase
{
    private readonly IBdeOperatorRepository _ops;
    private readonly IBdeActivityRepository _activities;
    private readonly IBdeBookingRepository _bookings;
    private readonly IWorkOperationRepository _workOps;

    public BdeApiController(IBdeOperatorRepository ops, IBdeActivityRepository activities,
        IBdeBookingRepository bookings, IWorkOperationRepository workOps)
    {
        _ops = ops;
        _activities = activities;
        _bookings = bookings;
        _workOps = workOps;
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
        var b = await _bookings.GetActiveForOperatorAsync(id);
        if (b == null) return Ok(new { booking = (object?)null });
        return Ok(new
        {
            booking = new
            {
                id = b.Id,
                bookingType = b.BookingType.ToString(),
                status = b.Status.ToString(),
                startedAt = b.StartedAt,
                workOperationId = b.WorkOperationId,
                bdeActivityId = b.BdeActivityId
            }
        });
    }

    [HttpGet("workoperation/{id:int}/latest-paused")]
    public async Task<IActionResult> GetLatestPaused(int id)
    {
        var b = await _bookings.GetLatestPausedForWorkOperationAsync(id);
        if (b == null) return NotFound();
        return Ok(new { id = b.Id, bookingType = b.BookingType.ToString(), operatorName = b.BdeOperator.DisplayName });
    }
}
