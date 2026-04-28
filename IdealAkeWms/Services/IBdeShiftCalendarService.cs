using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public interface IBdeShiftCalendarService
{
    /// <summary>
    /// Liefert das relevante Schichtende fuer eine Buchung — null wenn keine Auto-Pause greift
    /// (Master-Toggle aus, Wochenende, Feiertag, ausserhalb aller Schichten oder leerer Override-Plan).
    /// </summary>
    Task<DateTime?> GetShiftEndForBookingAsync(int workplaceId, DateTime startedAt);

    Task<IReadOnlyList<BdeShift>> GetShiftsAsync(int workplaceId, DayOfWeek day);
}
