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

    public DateTime FindPreviousPickupDay(DateTime date, HashSet<DayOfWeek> pickupDays)
    {
        if (pickupDays.Count == 0)
            return date;

        var result = date.Date;

        // Wenn der Tag selbst ein Abholtag ist, diesen nehmen
        if (pickupDays.Contains(result.DayOfWeek))
            return result;

        // Rückwärts suchen (max 7 Tage)
        for (int i = 0; i < 7; i++)
        {
            result = result.AddDays(-1);
            if (pickupDays.Contains(result.DayOfWeek))
                return result;
        }

        return date; // Fallback
    }

    private static readonly Dictionary<string, DayOfWeek> GermanDayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Montag"] = DayOfWeek.Monday,
        ["Dienstag"] = DayOfWeek.Tuesday,
        ["Mittwoch"] = DayOfWeek.Wednesday,
        ["Donnerstag"] = DayOfWeek.Thursday,
        ["Freitag"] = DayOfWeek.Friday,
        ["Samstag"] = DayOfWeek.Saturday,
        ["Sonntag"] = DayOfWeek.Sunday
    };

    public HashSet<DayOfWeek> ParsePickupDays(string setting)
    {
        var result = new HashSet<DayOfWeek>();
        if (string.IsNullOrWhiteSpace(setting))
            return result;

        foreach (var part in setting.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (GermanDayNames.TryGetValue(part, out var day))
                result.Add(day);
        }

        return result;
    }
}
