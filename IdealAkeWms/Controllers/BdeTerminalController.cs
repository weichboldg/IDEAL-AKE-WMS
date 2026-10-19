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
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<BdeTerminalController> _logger;

    public BdeTerminalController(IBdeTerminalRepository terminals, IBdeBookingService bookingSvc,
        ICurrentUserService userSvc, IProductionWorkplaceRepository workplaces,
        IBdeDefaultWorkOperationService defaultWoService, IAppSettingRepository settings,
        IServiceSettingRepository serviceSettings, ApplicationDbContext ctx, ILogger<BdeTerminalController> logger)
    {
        _terminals = terminals; _bookingSvc = bookingSvc; _userSvc = userSvc; _workplaces = workplaces;
        _defaultWoService = defaultWoService; _settings = settings; _serviceSettings = serviceSettings; _ctx = ctx; _logger = logger;
    }

    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"
                   || v.Equals("on", StringComparison.OrdinalIgnoreCase));

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
        var activeWorkplace = await _workplaces.GetByIdAsync(activeWorkplaceId);
        ViewBag.TerminalId = terminal.Id;
        ViewBag.WorkplaceId = activeWorkplaceId;
        ViewBag.WorkplaceName = activeWorkplace?.Name;
        ViewBag.AllWorkplaces = await _workplaces.GetBdeActiveAsync();
        ViewBag.DefaultWorkplaceId = terminal.DefaultProductionWorkplaceId;

        var nurFa = (await _settings.GetValueAsync(AppSettingKeys.BdeNurFaMeldung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.NurFaMode = nurFa;
        // v1.63.0 (FA-c2/c3): Werkbank meldet Arbeitsgaenge nur fertig (ohne Start/Stop); NurFA global gewinnt.
        ViewBag.NurAgFertigmeldung = activeWorkplace != null && WerkbankKuerzelFamilie.NurAgFertigmeldungWirksam(activeWorkplace, nurFa);
        // v1.64.0 (FA-1.1/1.3): Liste nach Haupt-FA gruppiert nur bei Master an (IDEAL); Anzeige-Grenze, ungueltig/< 1 → 5.
        ViewBag.Hierarchisch = IsTrue(await _serviceSettings.GetValueAsync(
            IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master));
        var maxHauptFa = await _settings.GetIntValueAsync(AppSettingKeys.BdeTerminalMaxHauptFa, 5);
        ViewBag.MaxHauptFa = maxHauptFa < 1 ? 5 : maxHauptFa;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProductionForOrder(int operatorId, int productionOrderId, int workplaceId, int terminalId)
    {
        // QA v1.63.0 (FA-a9/Antwort 12): VOR dem Anlegen des Default-AG „01" pruefen, deckungsgleich mit dem Angebot
        // (nur NurFA-Modus, nur FAs der Terminal-Werkbank) — sonst legt ein manipulierter POST einen „01"-AG an einem
        // fremden FA an der Terminal-Werkbank an und besteht danach die Familien-Pruefung im Normal-Modus.
        var nurFa = (await _settings.GetValueAsync(AppSettingKeys.BdeNurFaMeldung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        var orderWorkplaceId = await _ctx.ProductionOrders.Where(po => po.Id == productionOrderId)
            .Select(po => po.ProductionWorkplaceId).FirstOrDefaultAsync();
        if (!nurFa || orderWorkplaceId != workplaceId)
            return Json(MapResult(BdeBookingResult.Invalid("Dieser Arbeitsgang gehört nicht zu dieser Werkbank.")));

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

            var otherActiveBookings = goodQty.HasValue || scrapQty.HasValue
                ? await OtherActiveBookingsAsync(result)
                : Array.Empty<object>();

            return Json(MapResultWithOthers(result, otherActiveBookings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finish failed for bookingId={BookingId}", bookingId);
            return Json(new { outcome = "Exception", message = $"Serverfehler: {ex.GetType().Name} — {ex.Message}" });
        }
    }

    /// <summary>v1.63.0 (FA-c4): Fertigmeldung ohne Start/Stop. Bei erlaubter Mehrfachbuchung je Arbeitsgang traegt die
    /// Antwort die anderen Laufenden wie <see cref="Finish"/> (Andere-schliessen-Dialog).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportCompletion(int operatorId, int workOperationId, int workplaceId, int terminalId,
        decimal? goodQty, decimal? scrapQty)
    {
        var result = await _bookingSvc.ReportCompletionAsync(operatorId, workOperationId, workplaceId, terminalId, goodQty, scrapQty);
        return Json(MapResultWithOthers(result, await OtherActiveBookingsAsync(result)));
    }

    /// <summary>Nach einer Ende-Menge: bei BdeMehrfachBuchungProArbeitsgang die noch laufenden Buchungen anderer Werker.</summary>
    private async Task<object[]> OtherActiveBookingsAsync(BdeBookingResult result)
    {
        if (result.Outcome != BdeBookingOutcome.Success || result.Booking?.WorkOperationId is not int woId)
            return Array.Empty<object>();
        var multiMa = (await _settings.GetValueAsync(AppSettingKeys.BdeMehrfachBuchungProArbeitsgang))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!multiMa) return Array.Empty<object>();

        return await _ctx.BdeBookings
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
                     && (b.Status == BdeBookingStatus.Paused || b.Status == BdeBookingStatus.AutoPaused)
                     && !b.IsCancelled
                     && !_ctx.BdeBookings.Any(child => child.ParentBookingId == b.Id))
            .OrderBy(b => b.StartedAt)
            .Select(b => new {
                bookingId = b.Id,
                orderNumber = b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
                operationNumber = b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
                operationName = b.WorkOperation != null ? b.WorkOperation.Name : "",
                pausedAt = b.EndedAt,
                status = b.Status.ToString()
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
