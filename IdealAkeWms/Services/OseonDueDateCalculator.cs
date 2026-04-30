namespace IdealAkeWms.Services;

public static class OseonDueDateCalculator
{
    /// <summary>
    /// Berechnet das relevante AG-Termin-Datum auf Basis von Auftrags-Termin + Offset (Werktage).
    /// Offset 0 → Auftrags-Termin (Date). Sonst Werktage via IBusinessDayService.
    /// </summary>
    public static DateTime Calculate(DateTime baseDate, int offsetDays, IBusinessDayService businessDays, HashSet<DateTime> holidays)
    {
        if (offsetDays == 0) return baseDate.Date;
        return businessDays.AddBusinessDays(baseDate, offsetDays, holidays);
    }
}
