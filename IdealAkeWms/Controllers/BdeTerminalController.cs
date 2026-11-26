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
    private readonly IBdeDefaultWorkOperationService _defaultWoService;
    private readonly IAppSettingRepository _settings;
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<BdeTerminalController> _logger;

    public BdeTerminalController(IBdeTerminalRepository terminals, IBdeBookingService bookingSvc,
        ICurrentUserService userSvc, IBdeDefaultWorkOperationService defaultWoService, IAppSettingRepository settings,
        ApplicationDbContext ctx, ILogger<BdeTerminalController> logger)
    {
        _terminals = terminals; _bookingSvc = bookingSvc; _userSvc = userSvc;
        _defaultWoService = defaultWoService; _settings = settings; _ctx = ctx; _logger = logger;
    }

    /// <summary>
    /// v1.99.78 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 5): die Terminal-Seite ist abgeloest. Index leitet an beiden Standorten
    /// IMMER auf die BDE-Seite weiter (alte Lesezeichen, Startseite/Menue, Hilfe-Links); Werkbank aus der Query, sonst die Terminal-Vorgabe.
    /// Die JSON-Aktionen unten bleiben – die FA-Sicht (bde-fa-sicht.js) bucht darueber.
    /// </summary>
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
        return RedirectToAction("Index", "BdeTeileliste", new { workplaceId = workplaceId ?? terminal.DefaultProductionWorkplaceId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProductionForOrder(int operatorId, int productionOrderId, int workplaceId, int terminalId)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
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
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
        var result = await _bookingSvc.StartSetupAsync(operatorId, workOperationId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProduction(int operatorId, int workOperationId, int workplaceId, int terminalId)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
        var result = await _bookingSvc.StartProductionAsync(operatorId, workOperationId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartActivity(int operatorId, int activityId, int workplaceId, int terminalId)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
        var result = await _bookingSvc.StartActivityAsync(operatorId, activityId, workplaceId, terminalId);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pause(int bookingId, int operatorId, int terminalId, decimal? goodQty, decimal? scrapQty)
    {
        if (await FremdeBuchungAsync(bookingId, operatorId, terminalId) is { } fremd) return fremd;
        var result = await _bookingSvc.PauseAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId)
    {
        if (await FremdeBuchungAsync(pausedBookingId, operatorId, terminalId) is { } fremd) return fremd;
        try
        {
            var result = await _bookingSvc.ResumeAsync(pausedBookingId, operatorId, resumeAs, workplaceId, terminalId);
            return Json(MapResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume failed for bookingId={BookingId} operatorId={OperatorId} resumeAs={ResumeAs}",
                pausedBookingId, operatorId, resumeAs);
            return Json(ServerfehlerAntwort());
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int bookingId, int operatorId, int terminalId, decimal? goodQty, decimal? scrapQty)
    {
        try
        {
            if (await FremdeBuchungAsync(bookingId, operatorId, terminalId) is { } fremd) return fremd;
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
            return Json(ServerfehlerAntwort());
        }
    }

    /// <summary>v1.63.0 (FA-c4): Fertigmeldung ohne Start/Stop. Bei erlaubter Mehrfachbuchung je Arbeitsgang traegt die
    /// Antwort die anderen Laufenden wie <see cref="Finish"/> (Andere-schliessen-Dialog).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportCompletion(int operatorId, int workOperationId, int workplaceId, int terminalId,
        decimal? goodQty, decimal? scrapQty)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
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
    public async Task<IActionResult> FinishGroup(int operatorId, int terminalId, string entriesJson)
    {
        try
        {
            // Besitz je Zeile prueft FinishGroupAsync (alle Buchungen muessen operatorId gehoeren)
            if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
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
            return Json(ServerfehlerAntwort());
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportPartial(int bookingId, int operatorId, int terminalId, decimal goodQty, decimal scrapQty)
    {
        if (await FremdeBuchungAsync(bookingId, operatorId, terminalId) is { } fremd) return fremd;
        var result = await _bookingSvc.ReportPartialQuantityAsync(bookingId, goodQty, scrapQty);
        return Json(MapResult(result));
    }

    // v1.99.81 (F-29/15A): die GETs PausedBookings/ActiveBookings sind geloescht (keine Aufrufer, kein Waechter); die FA-Sicht rendert
    // dieselben Abfragen serverseitig (BdeKopfBuchungen).

    /// <summary>v1.99.81 (2A): andere Buchungen des AG beenden darf nur, wer auf diesem AG gerade selbst fertig gemeldet hat.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOthers(int workOperationId, int operatorId, int terminalId)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
        if (!await _ctx.BdeBookings.AnyAsync(b => b.WorkOperationId == workOperationId && b.BdeOperatorId == operatorId
                && b.Status == BdeBookingStatus.Finished && !b.IsCancelled))
            return Json(MapResult(BdeBookingResult.Invalid(
                "Andere Buchungen kann nur beenden, wer diesen Arbeitsgang selbst fertig gemeldet hat.")));
        var result = await _bookingSvc.CloseOtherBookingsOnWorkOperationAsync(workOperationId, exceptOperatorId: operatorId);
        return Ok(new { closedCount = result.ClosedCount });
    }

    /// <summary>v1.99.81 (2A, ADR 0039): Terminal-Waechter (F-28) + aktiver Werker. Die Personalnummer identifiziert, authentisiert nicht –
    /// Schutzgrenze ist das Terminal-Konto. null = ok.</summary>
    private async Task<JsonResult?> WaechterAsync(int? operatorId, int terminalId)
    {
        if (await FremdesTerminalAsync(terminalId) is { } fremd) return fremd;
        var op = operatorId is int id ? await _ctx.BdeOperators.FindAsync(id) : null;
        return op is { IsActive: true } ? null
            : Json(MapResult(BdeBookingResult.Invalid("Dieser Werker ist nicht (mehr) aktiv – bitte neu anmelden.")));
    }

    /// <summary>v1.99.81 (2A): <see cref="WaechterAsync"/> + die Buchung gehoert dem Werker. Das Start-Terminal der Buchung zaehlt bewusst
    /// nicht – an Terminal A gestartet, an Terminal B vom selben Werker beendet bleibt erlaubt. null = ok.</summary>
    private async Task<JsonResult?> FremdeBuchungAsync(int bookingId, int? operatorId, int terminalId)
    {
        if (await WaechterAsync(operatorId, terminalId) is { } fremd) return fremd;
        var besitzer = await _ctx.BdeBookings.Where(b => b.Id == bookingId).Select(b => (int?)b.BdeOperatorId).FirstOrDefaultAsync();
        return besitzer == operatorId ? null
            : Json(MapResult(BdeBookingResult.Invalid("Diese Buchung gehört nicht dem angemeldeten Werker – Seite neu laden.")));
    }

    /// <summary>H-B: fester Text statt Exception-Typ/ex.Message beim Werker; Details stehen im Log (Aufrufer loggt).</summary>
    private static object ServerfehlerAntwort() => new
    {
        outcome = "Exception",
        message = $"Serverfehler – nichts sicher gebucht. Seite neu laden und Buchung prüfen; Details im Protokoll ({DateTime.Now:HH:mm})."
    };

    /// <summary>v1.99.79 F-28: <c>terminalId</c> kommt vom Client. Gueltig ist nur das Terminal des angemeldeten Benutzers – dasselbe,
    /// das die BDE-Seite in data-terminal-id rendert (BdeTeilelisteController). Sonst Ablehnung statt Buchung auf fremdes Terminal.</summary>
    private async Task<JsonResult?> FremdesTerminalAsync(int terminalId)
    {
        var userId = _userSvc.GetCurrentAppUserId();
        var eigenes = userId == null ? null : await _terminals.GetByUserIdAsync(userId.Value);
        return eigenes?.Id == terminalId ? null
            : Json(MapResult(BdeBookingResult.Invalid("Dieses Terminal ist nicht dem angemeldeten Benutzer zugeordnet – Seite neu laden.")));
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
