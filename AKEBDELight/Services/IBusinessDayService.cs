namespace AKEBDELight.Services;

public interface IBusinessDayService
{
    DateTime SubtractBusinessDays(DateTime date, int days, HashSet<DateTime> holidays);
}
