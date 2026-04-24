using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Controllers;

public record FinishGroupEntry(int BookingId, decimal? GoodQty, decimal? ScrapQty, bool IsFinal);

[RequireBdeActive]
[RequireBdeUserAccess]
public class BdeTerminalController : Controller
{
    private readonly IBdeTerminalRepository _terminals;
    private readonly IBdeBookingService _bookingSvc;
    private readonly ICurrentUserService _userSvc;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IBdeDefaultWorkOperationService _defaultWoService;
    private readonly IAppSettingRepository _settings;
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<BdeTerminalController> _logger;

    public BdeTerminalController(IBdeTerminalRepository terminals, IBdeBookingService bookingSvc,
        ICurrentUserService userSvc, IProductionWorkplaceRepository workplaces,
        IBdeDefaultWorkOperationService defaultWoService, IAppSettingRepository settings,
        ApplicationDbContext ctx, ILogger<BdeTerminalController> logger)
    {
        _terminals = terminals; _bookingSvc = bookingSvc; _userSvc = userSvc; _workplaces = workplaces;
        _defaultWoService = defaultWoService; _settings = settings; _ctx = ctx; _logger = logger;
    }

    public async Task<IActionResult> Index(int? workplaceId)
    {
        var appUserId = _userSvc.GetCurrentAppUserId();
        if (appUserId == null) return RedirectToAction("Login", "Account");

        var terminal = await _terminals.GetByUserIdAsync(appUserId.Value);
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }

        var activeWorkplaceId = workplaceId ?? terminal.DefaultProductionWorkplaceId;
        ViewBag.TerminalId = terminal.Id;
        ViewBag.WorkplaceId = activeWorkplaceId;
        ViewBag.WorkplaceName = (await _workplaces.GetByIdAsync(activeWorkplaceId))?.Name;
        ViewBag.AllWorkplaces = await _workplaces.GetBdeActiveAsync();
        ViewBag.DefaultWorkplaceId = terminal.DefaultProductionWorkplaceId;

        var nurFa = (await _settings.GetValueAsync("BdeNurFaMeldung"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.NurFaMode = nurFa;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProductionForOrder(int operatorId, int productionOrderId, int workplaceId, int terminalId)
    {
        var workOperationId = await _defaultWoService.FindOrCreateDefaultAsync(productionOrderId, workplaceId);
        var result = await _bookingSvc.StartProductionAsync(operatorId, workOperationId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartSetup(int operatorId, int workOperationId, int workplaceId, int terminalId)
    {
        var result = await _bookingSvc.StartSetupAsync(operatorId, workOperationId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProduction(int operatorId, int workOperationId, int workplaceId, int terminalId)
    {
        var result = await _bookingSvc.StartProductionAsync(operatorId, workOperationId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartActivity(int operatorId, int activityId, int workplaceId, int terminalId)
    {
        var result = await _bookingSvc.StartActivityAsync(operatorId, activityId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pause(int bookingId, decimal? goodQty, decimal? scrapQty)
    {
        var result = await _bookingSvc.PauseAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId)
    {
        try
        {
            var result = await _bookingSvc.ResumeAsync(pausedBookingId, operatorId, resumeAs, workplaceId, terminalId);
            return Json(MapResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume failed for bookingId={BookingId} operatorId={OperatorId} resumeAs={ResumeAs}",
                pausedBookingId, operatorId, resumeAs);
            return Json(new
            {
                outcome = "Exception",
                message = $"Serverfehler: {ex.GetType().Name} — {ex.Message}"
            });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int bookingId, decimal? goodQty, decimal? scrapQty)
    {
        try
        {
            // GroupFinishRequired check: only for Production bookings
            var booking = await _ctx.BdeBookings.FindAsync(bookingId);
            if (booking != null && booking.BookingType == BdeBookingType.Production)
            {
                var groupSetting = (await _settings.GetValueAsync(AppSettingKeys.BdeGleichzeitigerAbschlussBeiMehrfachStart))
                    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
                var multiOp = (await _settings.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProOperator))
                    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

                if (groupSetting && multiOp)
                {
                    var activeProductions = await _ctx.BdeBookings
                        .Include(b => b.WorkOperation)
                            .ThenInclude(wo => wo!.ProductionOrder)
                        .Where(b => b.BdeOperatorId == booking.BdeOperatorId
                                 && b.BookingType == BdeBookingType.Production
                                 && b.Status == BdeBookingStatus.Running
                                 && b.EndedAt == null
                                 && !b.IsCancelled)
                        .OrderBy(b => b.StartedAt)
                        .ToListAsync();

                    if (activeProductions.Count >= 2)
                    {
                        return Json(new
                        {
                            outcome = "GroupFinishRequired",
                            bookings = activeProductions.Select(b => new
                            {
                                bookingId = b.Id,
                                orderNumber = b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
                                operationNumber = b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
                                operationName = b.WorkOperation != null ? b.WorkOperation.Name : "",
                                targetQuantity = b.WorkOperation != null ? (decimal?)b.WorkOperation.ProductionOrder!.Quantity : null,
                                startedAt = b.StartedAt
                            }).ToArray()
                        });
                    }
                }
            }

            // Normal single-finish flow
            var result = await _bookingSvc.FinishAsync(bookingId, goodQty, scrapQty);

            object[] otherActiveBookings = Array.Empty<object>();

            if (result.Outcome == BdeBookingOutcome.Success
                && (goodQty.HasValue || scrapQty.HasValue)
                && result.Booking?.WorkOperationId is int woId)
            {
                var multiMa = (await _settings.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
                    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

                if (multiMa)
                {
                    otherActiveBookings = await _ctx.BdeBookings
                        .Include(b => b.BdeOperator)
                        .Where(b => b.WorkOperationId == woId
                                 && b.BdeOperatorId != result.Booking.BdeOperatorId
                                 && b.EndedAt == null
                                 && !b.IsCancelled)
                        .Select(b => (object)new {
                            operatorId = b.BdeOperatorId,
                            operatorName = b.BdeOperator!.FirstName + " " + b.BdeOperator.LastName,
                            startedAt = b.StartedAt
                        })
                        .ToArrayAsync();
                }
            }

            return Json(MapResultWithOthers(result, otherActiveBookings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finish failed for bookingId={BookingId}", bookingId);
            return Json(new { outcome = "Exception", message = $"Serverfehler: {ex.GetType().Name} — {ex.Message}" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FinishGroup(int operatorId, string entriesJson)
    {
        try
        {
            var entries = System.Text.Json.JsonSerializer.Deserialize<List<FinishGroupEntry>>(entriesJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            var serviceEntries = entries.Select(e =>
                new GroupFinishEntry(e.BookingId, e.GoodQty, e.ScrapQty, e.IsFinal)).ToList();
            var result = await _bookingSvc.FinishGroupAsync(operatorId, serviceEntries);

            if (!result.IsSuccess)
                return Json(new { outcome = "Error", message = result.ErrorMessage });

            return Json(new { outcome = "Success", closedCount = result.ClosedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FinishGroup failed for operatorId={OperatorId}", operatorId);
            return Json(new { outcome = "Exception", message = $"Serverfehler: {ex.GetType().Name} — {ex.Message}" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportPartial(int bookingId, decimal goodQty, decimal scrapQty)
    {
        var result = await _bookingSvc.ReportPartialQuantityAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
    }

    [HttpGet]
    public async Task<IActionResult> PausedBookings(int operatorId)
    {
        var paused = await _ctx.BdeBookings
            .Include(b => b.WorkOperation)
                .ThenInclude(wo => wo!.ProductionOrder)
            .Where(b => b.BdeOperatorId == operatorId
                     && b.Status == BdeBookingStatus.Paused
                     && !b.IsCancelled
                     && !_ctx.BdeBookings.Any(child => child.ParentBookingId == b.Id))
            .OrderBy(b => b.StartedAt)
            .Select(b => new {
                bookingId = b.Id,
                orderNumber = b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
                operationNumber = b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
                operationName = b.WorkOperation != null ? b.WorkOperation.Name : "",
                pausedAt = b.EndedAt
            })
            .ToListAsync();

        return Ok(paused);
    }

    [HttpGet]
    public async Task<IActionResult> ActiveBookings(int operatorId)
    {
        var active = await _ctx.BdeBookings
            .Include(b => b.WorkOperation)
                .ThenInclude(wo => wo!.ProductionOrder)
            .Include(b => b.BdeActivity)
            .Include(b => b.ProductionWorkplace)
            .Where(b => b.BdeOperatorId == operatorId
                     && b.Status == BdeBookingStatus.Running
                     && b.EndedAt == null
                     && !b.IsCancelled)
            .OrderBy(b => b.StartedAt)
            .Select(b => new {
                bookingId = b.Id,
                bookingType = b.BookingType.ToString(),
                startedAt = b.StartedAt,
                orderNumber = b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
                operationNumber = b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
                operationName = b.WorkOperation != null ? b.WorkOperation.Name : "",
                activityName = b.BdeActivity != null ? b.BdeActivity.Name : "",
                workplaceName = b.ProductionWorkplace != null ? b.ProductionWorkplace.Name : "",
                targetQuantity = b.WorkOperation != null ? (decimal?)b.WorkOperation.ProductionOrder!.Quantity : null
            })
            .ToListAsync();

        return Ok(active);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOthers(int workOperationId, int operatorId)
    {
        var result = await _bookingSvc.CloseOtherBookingsOnWorkOperationAsync(workOperationId, exceptOperatorId: operatorId);
        return Ok(new { closedCount = result.ClosedCount });
    }

    private static object MapResult(BdeBookingResult r) => new
    {
        outcome = r.Outcome.ToString(),
        bookingId = r.Booking?.Id,
        collidingOperator = r.CollidingBooking?.BdeOperator?.DisplayName,
        collidingWorkplace = r.CollidingBooking?.ProductionWorkplace?.Name,
        collidingSince = r.CollidingBooking?.StartedAt,
        message = r.Message
    };

    private static object MapResultWithOthers(BdeBookingResult r, object[] otherActiveBookings) => new
    {
        outcome = r.Outcome.ToString(),
        bookingId = r.Booking?.Id,
        workOperationId = r.Booking?.WorkOperationId,
        collidingOperator = r.CollidingBooking?.BdeOperator?.DisplayName,
        collidingWorkplace = r.CollidingBooking?.ProductionWorkplace?.Name,
        collidingSince = r.CollidingBooking?.StartedAt,
        message = r.Message,
        otherActiveBookings
    };
}
