namespace IdealAkeWms.Services;

/// <summary>
/// Beschichtungstermin = VorkommissionierTermin - BeschichtungTage (Arbeitstage), dann auf den
/// vorherigen Abholtag. Backward-Compat: Feature inaktiv (<paramref name="featureActive"/>=false)
/// => Termin fuer ALLE Auftraege; aktiv => nur wenn <paramref name="hasCoatingParts"/>.
/// Gemeinsam genutzt von Leitstand + FA-Abarbeitungsliste.
/// </summary>
public static class CoatingDateCalculator
{
    public static DateTime? Compute(
        DateTime? vorkommissionierTermin,
        int beschichtungTage,
        HashSet<DateTime> holidays,
        HashSet<DayOfWeek> pickupDays,
        bool hasCoatingParts,
        bool featureActive,
        IBusinessDayService businessDayService)
    {
        if (!vorkommissionierTermin.HasValue) return null;
        if (featureActive && !hasCoatingParts) return null;

        var raw = businessDayService.SubtractBusinessDays(vorkommissionierTermin.Value, beschichtungTage, holidays);
        return businessDayService.FindPreviousPickupDay(raw, pickupDays);
    }
}
