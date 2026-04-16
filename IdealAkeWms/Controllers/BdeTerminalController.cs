using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireBdeActive]
[RequireBdeUserAccess]
public class BdeTerminalController : Controller
{
    private readonly IBdeTerminalRepository _terminals;
    private readonly IBdeBookingService _bookingSvc;
    private readonly ICurrentUserService _userSvc;
    private readonly IProductionWorkplaceRepository _workplaces;

    public BdeTerminalController(IBdeTerminalRepository terminals, IBdeBookingService bookingSvc,
        ICurrentUserService userSvc, IProductionWorkplaceRepository workplaces)
    {
        _terminals = terminals; _bookingSvc = bookingSvc; _userSvc = userSvc; _workplaces = workplaces;
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
        ViewBag.AllWorkplaces = await _workplaces.GetAllOrderedAsync();
        ViewBag.DefaultWorkplaceId = terminal.DefaultProductionWorkplaceId;
        return View();
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
        var result = await _bookingSvc.ResumeAsync(pausedBookingId, operatorId, resumeAs, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int bookingId, decimal? goodQty, decimal? scrapQty)
    {
        var result = await _bookingSvc.FinishAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportPartial(int bookingId, decimal goodQty, decimal scrapQty)
    {
        var result = await _bookingSvc.ReportPartialQuantityAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
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
}
