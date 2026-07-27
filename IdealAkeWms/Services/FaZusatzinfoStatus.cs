namespace IdealAkeWms.Services;

/// <summary>
/// Fold 2 (v1.26.0, Spec §10.6): gemeinsame Status-Wahrheit fuer den Sage-Status
/// "verpackt"/"abgeholt". Genutzt vom FaZusatzinfoSyncService (Auto-Erledigt,
/// Service-Projekt via ProjectReference) UND vom BdeBookingService (BDE-Sperre).
/// ACHTUNG: EF-Listen-Filter koennen den statischen Helper NICHT verwenden (nicht
/// EF-uebersetzbar) — dort die Konstanten inline mit dem ausgeschriebenen
/// Null-Guard-Praedikat vergleichen (.Trim().ToLower() != Verpackt/Abgeholt).
/// </summary>
public static class FaZusatzinfoStatus
{
    public const string Verpackt = "verpackt";
    public const string Abgeholt = "abgeholt";

    public static bool IstVerpacktOderAbgeholt(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim();
        return s.Equals(Verpackt, StringComparison.OrdinalIgnoreCase)
            || s.Equals(Abgeholt, StringComparison.OrdinalIgnoreCase);
    }
}
