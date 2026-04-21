namespace IdealAkeWms.Services;

public interface IBdeTimeSplitService
{
    Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day);
    Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId);
}

public record BookingSplit(int BookingId, TimeSpan EffectiveDuration);
