namespace AKEBDELight.Services;

public class BusinessDayService : IBusinessDayService
{
    public DateTime SubtractBusinessDays(DateTime date, int days, HashSet<DateTime> holidays)
    {
        var result = date.Date;
        var remaining = Math.Abs(days);

        while (remaining > 0)
        {
            result = result.AddDays(-1);

            if (result.DayOfWeek != DayOfWeek.Saturday &&
                result.DayOfWeek != DayOfWeek.Sunday &&
                !holidays.Contains(result))
            {
                remaining--;
            }
        }

        return result;
    }
}
