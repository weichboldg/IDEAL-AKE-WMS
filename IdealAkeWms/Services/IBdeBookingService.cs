using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public interface IBdeBookingService
{
    Task<BdeBookingResult> StartSetupAsync(int operatorId, int workOperationId, int workplaceId, int terminalId);
    Task<BdeBookingResult> StartProductionAsync(int operatorId, int workOperationId, int workplaceId, int terminalId);
    Task<BdeBookingResult> StartActivityAsync(int operatorId, int activityId, int workplaceId, int terminalId);
    Task<BdeBookingResult> PauseAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null);
    Task<BdeBookingResult> ResumeAsync(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId);
    Task<BdeBookingResult> FinishAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null);
    /// <summary>Fertigmeldung ohne Start/Stop an einer Werkbank „nur AG-Fertigmeldung" (v1.63.0).</summary>
    Task<BdeBookingResult> ReportCompletionAsync(int operatorId, int workOperationId, int workplaceId, int terminalId,
        decimal? goodQty, decimal? scrapQty);
    /// <summary>v1.99.32: wie <see cref="ReportCompletionAsync"/>, aber ohne eigene Transaktion (Aufrufer stellt sie).
    /// v1.99.75: <paramref name="istFinal"/> false = Teil-Buchung (Menge mit IsFinal = false, AG bleibt offen; BDE-Teileliste „Teilfertig“).</summary>
    Task<BdeBookingResult> ReportCompletionCoreAsync(int operatorId, int workOperationId, int workplaceId, int terminalId,
        decimal? goodQty, decimal? scrapQty, bool istFinal = true);
    Task<BdeBookingResult> ReportPartialQuantityAsync(int bookingId, decimal goodQty, decimal scrapQty);
    Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(int workOperationId, int exceptOperatorId);
    Task<GroupFinishResult> FinishGroupAsync(int operatorId, IReadOnlyList<GroupFinishEntry> entries);
    /// <summary>v1.99.81 (Entscheid 9): warum ein pausierter AG an dieser Werkbank nicht fortsetzbar ist (null = fortsetzbar).
    /// Dieselbe Pruefung wie <see cref="ResumeAsync"/> – eine Methode, zwei Leser (Resume und Kopf der FA-Sicht).</summary>
    Task<string?> FortsetzenSperreAsync(int workOperationId, int workplaceId);
    /// <summary>v1.99.81 (Entscheid 9): haengende Pause beenden, ohne Mengen (die Zeit ist schon gebucht); Audit-Felder.</summary>
    Task<BdeBookingResult> ClosePausedAsync(int bookingId);
}

public record CloseOthersResult(int ClosedCount);
public record GroupFinishEntry(int BookingId, decimal? GoodQty, decimal? ScrapQty, bool IsFinal);
public record GroupFinishResult(int ClosedCount, string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorMessage == null;
}
