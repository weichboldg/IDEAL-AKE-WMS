namespace IdealAkeWms.Services;

/// <summary>
/// Vorkommissioniertage je FA-Zeile: <c>ProductionWorkplace.OverridePrePickingDays</c>
/// („Abweichende Vorkommissioniertage") schlaegt den globalen AppSetting-Wert
/// <c>VorkommissionierTage</c>, sobald er gesetzt ist — <c>0</c> eingeschlossen.
/// Gleiches Muster wie <c>ProductionWorkplace.BdeDefaultArbeitsgang</c>.
///
/// Die Regel steht bewusst NUR hier, damit FA-Abarbeitungsliste, Leitstand und
/// FA-Abarbeitungsliste je Arbeitsgang fuer dieselbe FA garantiert denselben
/// Vorkommissioniertermin (und damit denselben Beschichtungstermin) zeigen.
/// </summary>
public static class PrePickingDaysResolver
{
    /// <param name="workplaceOverride">Werkbank-Override, <c>null</c> = kein Override.</param>
    /// <param name="globalDays">Globaler Settings-Wert <c>VorkommissionierTage</c>.</param>
    public static int Resolve(int? workplaceOverride, int globalDays) => workplaceOverride ?? globalDays;

    /// <summary>
    /// True, wenn fuer die Zeile tatsaechlich ein Werkbank-Override griff (strikt nach
    /// <c>HasValue</c>, nicht nach <c>&gt; 0</c>) — Basis fuer die UI-Rueckmeldung.
    /// </summary>
    public static bool IsOverrideActive(int? workplaceOverride) => workplaceOverride.HasValue;
}
