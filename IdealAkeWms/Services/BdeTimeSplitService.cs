using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

public class BdeTimeSplitService : IBdeTimeSplitService
{
    private readonly ApplicationDbContext _ctx;

    public BdeTimeSplitService(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day)
    {
        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);

        var now = DateTime.Now;
        var bookings = await _ctx.BdeBookings
            .Include(b => b.WorkOperation)
                .ThenInclude(wo => wo!.ProductionOrder)
            .Include(b => b.Quantities)
            .Where(b => b.BdeOperatorId == operatorId
                     && !b.IsCancelled
                     && b.StartedAt < dayEnd
                     && (b.EndedAt == null || b.EndedAt > dayStart))
            .ToListAsync();

        if (bookings.Count == 0)
            return Array.Empty<BookingSplit>();

        // Clip each booking to the day window
        var intervals = bookings
            .Select(b => new
            {
                Booking = b,
                Start = b.StartedAt > dayStart ? b.StartedAt : dayStart,
                End = (b.EndedAt ?? now) < dayEnd ? (b.EndedAt ?? now) : dayEnd
            })
            .Where(x => x.End > x.Start)
            .ToList();

        if (intervals.Count == 0)
            return Array.Empty<BookingSplit>();

        // Collect all distinct boundary timepoints
        var timepoints = intervals
            .SelectMany(i => new[] { i.Start, i.End })
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var accum = intervals.ToDictionary(i => i.Booking.Id, _ => TimeSpan.Zero);

        for (int i = 0; i < timepoints.Count - 1; i++)
        {
            var segStart = timepoints[i];
            var segEnd = timepoints[i + 1];
            var segDuration = segEnd - segStart;
            if (segDuration <= TimeSpan.Zero) continue;

            var activeInSeg = intervals
                .Where(iv => iv.Start <= segStart && iv.End >= segEnd)
                .ToList();

            if (activeInSeg.Count == 0) continue;
            if (activeInSeg.Count == 1)
            {
                accum[activeInSeg[0].Booking.Id] += segDuration;
                continue;
            }

            // N > 1 → apportion by weights
            var weights = ComputeWeights(activeInSeg.Select(a => a.Booking).ToList());
            var weightSum = weights.Values.Sum();

            if (weightSum <= 0)
            {
                // Fallback: equal split
                var share = segDuration / activeInSeg.Count;
                foreach (var iv in activeInSeg)
                    accum[iv.Booking.Id] += share;
                continue;
            }

            foreach (var iv in activeInSeg)
            {
                var w = weights[iv.Booking.Id];
                accum[iv.Booking.Id] += TimeSpan.FromTicks((long)(segDuration.Ticks * (double)w / (double)weightSum));
            }
        }

        return accum.Select(kv => new BookingSplit(kv.Key, kv.Value)).ToList();
    }

    public async Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId)
    {
        var booking = await _ctx.BdeBookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return TimeSpan.Zero;

        var start = booking.StartedAt.Date;
        var end = (booking.EndedAt ?? DateTime.Now).Date;

        var total = TimeSpan.Zero;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var splits = await ComputeForOperatorDayAsync(booking.BdeOperatorId, day);
            var s = splits.FirstOrDefault(x => x.BookingId == bookingId);
            if (s != null)
                total += s.EffectiveDuration;
        }

        return total;
    }

    /// <summary>
    /// Computes weight per booking.
    /// Primary: sum of GoodQuantity entries.
    /// Fallback 1: FA Sollmenge (ProductionOrder.Quantity).
    /// Fallback 2 (caller): equal split when all weights are zero.
    /// </summary>
    private Dictionary<int, decimal> ComputeWeights(IReadOnlyList<BdeBooking> bookings)
    {
        var gutmengeWeights = bookings.ToDictionary(
            b => b.Id,
            b => b.Quantities?.Sum(q => q.GoodQuantity) ?? 0m);

        if (gutmengeWeights.Values.Sum() > 0m)
            return gutmengeWeights;

        // Fallback 1: Sollmenge
        var sollmengeWeights = bookings.ToDictionary(
            b => b.Id,
            b => b.WorkOperation?.ProductionOrder?.Quantity ?? 0m);

        if (sollmengeWeights.Values.Sum() > 0m)
            return sollmengeWeights;

        return bookings.ToDictionary(b => b.Id, _ => 0m);
    }
}
