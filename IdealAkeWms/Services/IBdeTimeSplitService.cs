namespace IdealAkeWms.Services;

public interface IBdeTimeSplitService
{
    Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day);
    Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId);
    Task<TimeSpan> ComputeCumulativeEffectiveDurationAsync(int bookingId);
}

public record BookingSplit(int BookingId, TimeSpan EffectiveDuration);
