namespace IdealAkeWms.Services;

public interface IBusinessDayService
{
    DateTime SubtractBusinessDays(DateTime date, int days, HashSet<DateTime> holidays);
    DateTime AddBusinessDays(DateTime date, int days, HashSet<DateTime> holidays);
    DateTime FindPreviousPickupDay(DateTime date, HashSet<DayOfWeek> pickupDays);
    HashSet<DayOfWeek> ParsePickupDays(string setting);
}
