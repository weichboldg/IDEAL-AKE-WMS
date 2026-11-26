using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>Eine laufende oder pausierte Buchung eines Werkers (Kopf der FA-Sicht, Panels des Terminals).</summary>
public sealed record BdeKopfBuchung(int BookingId, BdeBookingType Typ, BdeBookingStatus Status, DateTime StartedAt, DateTime? PausedAt,
    int? WorkOperationId, string OrderNumber, string OperationNumber, string OperationName, string ActivityName, string WorkplaceName,
    decimal? TargetQuantity, string TerminalName, string SubOrderNumber = "", int? WorkplaceId = null,
    decimal BisherGut = 0);

/// <summary>v1.99.81 (Entscheid 9): Zeile der Admin-Liste „Hängende Pausen“.</summary>
public sealed record HaengendePause(int BookingId, DateTime StartedAt, DateTime? PausedAt, string Werker, string Werkbank, string Ziel,
    string Art, string Status);

/// <summary>
/// v1.99.77 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-4): Abfragen der Kopf-Buchungen an EINEM Ort; die FA-Sicht rendert sie
/// serverseitig im Kopf. v1.99.81 (F-29): die frueheren BdeTerminal-GETs ActiveBookings/PausedBookings sind geloescht.
/// </summary>
public static class BdeKopfBuchungen
{
    public static Task<List<BdeKopfBuchung>> LaufendeAsync(ApplicationDbContext ctx, int operatorId) =>
        Projektion(ctx, ctx.BdeBookings.Where(b => b.BdeOperatorId == operatorId && b.Status == BdeBookingStatus.Running && b.EndedAt == null
            && !b.IsCancelled));

    /// <summary>Pausiert/AutoPausiert, ohne bereits fortgesetzte (Kind-Buchung mit ParentBookingId vorhanden).</summary>
    public static Task<List<BdeKopfBuchung>> PausierteAsync(ApplicationDbContext ctx, int operatorId) =>
        Projektion(ctx, Haengend(ctx).Where(b => b.BdeOperatorId == operatorId));

    /// <summary>v1.99.81 (Entscheid 9): EIN Praedikat fuer „pausiert, nie fortgesetzt“ – Kopf, Abschliessen und Admin-Liste „Hängende Pausen“.</summary>
    public static IQueryable<BdeBooking> Haengend(ApplicationDbContext ctx) => ctx.BdeBookings.Where(b =>
        (b.Status == BdeBookingStatus.Paused || b.Status == BdeBookingStatus.AutoPaused) && !b.IsCancelled
        && !ctx.BdeBookings.Any(child => child.ParentBookingId == b.Id));

    private static Task<List<BdeKopfBuchung>> Projektion(ApplicationDbContext ctx, IQueryable<BdeBooking> q) => q
        .OrderBy(b => b.StartedAt)
        .Select(b => new BdeKopfBuchung(b.Id, b.BookingType, b.Status, b.StartedAt, b.EndedAt, b.WorkOperationId,
            b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.OrderNumber : "",
            b.WorkOperation != null ? b.WorkOperation.OperationNumber : "",
            b.WorkOperation != null ? b.WorkOperation.Name : "",
            b.BdeActivity != null ? b.BdeActivity.Name : "",
            b.ProductionWorkplace != null ? b.ProductionWorkplace.Name : "",
            b.WorkOperation != null ? (decimal?)b.WorkOperation.ProductionOrder!.Quantity : null,
            b.BdeTerminal != null ? (b.BdeTerminal.Description ?? b.BdeTerminal.User.Name) : "",
            b.WorkOperation != null ? b.WorkOperation.ProductionOrder!.SubOrderNumber : "", // v1.99.79 F-35
            b.ProductionWorkplaceId, // v1.99.81 (3A): Gruppen-Abschluss nur je Werkbank
            // v1.99.81 (Entscheid 4): bisher Gut am AG (alle nicht stornierten Mengen) – Fertig-Dialog belegt mit Rest vor
            ctx.BdeBookingQuantities.Where(m => !m.BdeBooking!.IsCancelled && m.BdeBooking.WorkOperationId == b.WorkOperationId)
                .Sum(m => (decimal?)m.GoodQuantity) ?? 0m))
        .ToListAsync();

    /// <summary>v1.99.81 (Entscheid 9): Admin-Liste „Hängende Pausen“ aller Werker; <paramref name="pausiertBis"/> = Pause vor diesem Tag (einschl.).</summary>
    public static async Task<List<HaengendePause>> HaengendeListeAsync(ApplicationDbContext ctx, DateTime? pausiertBis)
    {
        var q = Haengend(ctx);
        if (pausiertBis is { } bis) q = q.Where(b => (b.EndedAt ?? b.StartedAt) < bis.Date.AddDays(1));
        return await q.OrderBy(b => b.EndedAt ?? b.StartedAt)
            .Select(b => new HaengendePause(b.Id, b.StartedAt, b.EndedAt, b.BdeOperator.DisplayName, b.ProductionWorkplace.Name,
                b.WorkOperation != null
                    ? b.WorkOperation.ProductionOrder.OrderNumber
                      + (b.WorkOperation.ProductionOrder.SubOrderNumber == null || b.WorkOperation.ProductionOrder.SubOrderNumber == ""
                         || b.WorkOperation.ProductionOrder.SubOrderNumber == b.WorkOperation.ProductionOrder.OrderNumber
                          ? "" : " · Sub-FA " + b.WorkOperation.ProductionOrder.SubOrderNumber)
                      + " / " + b.WorkOperation.OperationNumber.Trim()
                    : (b.BdeActivity != null ? b.BdeActivity.Name : "Tätigkeit"),
                b.BookingType == BdeBookingType.Setup ? "Rüsten" : b.BookingType == BdeBookingType.Production ? "Produktion" : "Tätigkeit",
                b.Status == BdeBookingStatus.AutoPaused ? "automatisch pausiert" : "pausiert"))
            .ToListAsync();
    }

    /// <summary>Spaltenfilter-Getter = Zelltext der View (Register <c>BdeBookings/HaengendePausen</c>).</summary>
    public static readonly IReadOnlyDictionary<string, Func<HaengendePause, string?>> HaengendeSpalten =
        new Dictionary<string, Func<HaengendePause, string?>>
        {
            ["pausiert-seit"] = p => p.PausedAt?.ToString("dd.MM.yyyy HH:mm"),
            ["start"] = p => p.StartedAt.ToString("dd.MM.yyyy HH:mm"),
            ["werker"] = p => p.Werker,
            ["werkbank"] = p => p.Werkbank,
            ["ziel"] = p => p.Ziel,
            ["art"] = p => p.Art,
            ["status"] = p => p.Status,
        };

    /// <summary>Laufzeit „h:mm h“ seit Start (Stand Seitenaufbau; nie negativ).</summary>
    public static string Laufzeit(DateTime start, DateTime jetzt)
    {
        var d = jetzt > start ? jetzt - start : TimeSpan.Zero;
        return $"{(int)d.TotalHours}:{d.Minutes:D2} h";
    }
}
