using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>Eine laufende oder pausierte Buchung eines Werkers (Kopf der FA-Sicht, Panels des Terminals).</summary>
public sealed record BdeKopfBuchung(int BookingId, BdeBookingType Typ, BdeBookingStatus Status, DateTime StartedAt, DateTime? PausedAt,
    int? WorkOperationId, string OrderNumber, string OperationNumber, string OperationName, string ActivityName, string WorkplaceName,
    decimal? TargetQuantity, string TerminalName, string SubOrderNumber = "");

/// <summary>
/// v1.99.77 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-4): die Abfragen hinter BdeTerminal <c>ActiveBookings</c>/<c>PausedBookings</c>
/// an EINEM Ort – das Terminal liefert sie weiter als JSON, die FA-Sicht rendert sie serverseitig im Kopf.
/// </summary>
public static class BdeKopfBuchungen
{
    public static Task<List<BdeKopfBuchung>> LaufendeAsync(ApplicationDbContext ctx, int operatorId) =>
        Projektion(ctx.BdeBookings.Where(b => b.BdeOperatorId == operatorId && b.Status == BdeBookingStatus.Running && b.EndedAt == null
            && !b.IsCancelled));

    /// <summary>Pausiert/AutoPausiert, ohne bereits fortgesetzte (Kind-Buchung mit ParentBookingId vorhanden).</summary>
    public static Task<List<BdeKopfBuchung>> PausierteAsync(ApplicationDbContext ctx, int operatorId) =>
        Projektion(ctx.BdeBookings.Where(b => b.BdeOperatorId == operatorId
            && (b.Status == BdeBookingStatus.Paused || b.Status == BdeBookingStatus.AutoPaused) && !b.IsCancelled
            && !ctx.BdeBookings.Any(child => child.ParentBookingId == b.Id)));

    private static Task<List<BdeKopfBuchung>> Projektion(IQueryable<BdeBooking> q) => q
        .OrderBy(b => b.StartedAt)
        .Select(b => new BdeKopfBuchung(b.Id, b.BookingType, b.Status, b.StartedAt, b.EndedAt, b.WorkOperationId,
            b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
            b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
            b.WorkOperation != null ? b.WorkOperation.Name : "",
            b.BdeActivity != null ? b.BdeActivity.Name : "",
            b.ProductionWorkplace != null ? b.ProductionWorkplace.Name : "",
            b.WorkOperation != null ? (decimal?)b.WorkOperation.ProductionOrder!.Quantity : null,
            b.BdeTerminal != null ? (b.BdeTerminal.Description ?? b.BdeTerminal.User.Name) : "",
            b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.SubOrderNumber : "")) // v1.99.79 F-35
        .ToListAsync();

    /// <summary>Laufzeit „h:mm h“ seit Start (Stand Seitenaufbau; nie negativ).</summary>
    public static string Laufzeit(DateTime start, DateTime jetzt)
    {
        var d = jetzt > start ? jetzt - start : TimeSpan.Zero;
        return $"{(int)d.TotalHours}:{d.Minutes:D2} h";
    }
}
