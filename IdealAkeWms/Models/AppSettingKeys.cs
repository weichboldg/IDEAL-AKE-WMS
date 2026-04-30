namespace IdealAkeWms.Models;

/// <summary>
/// Typed access to well-known AppSettings keys. Prevents silent typo-fallbacks.
/// </summary>
public static class AppSettingKeys
{
    // BDE
    public const string BdeMehrfachBuchungProOperator = "BdeMehrfachBuchungProOperator";
    public const string BdeMehrfachBuchungProArbeitsgang = "BdeMehrfachBuchungProArbeitsgang";
    public const string BdeGleichzeitigerAbschlussBeiMehrfachStart = "BdeGleichzeitigerAbschlussBeiMehrfachStart";
    public const string BdeSchichtkalenderAktiv = "BdeSchichtkalenderAktiv";

    // OSEON / Tracking / Reporting
    public const string OseonReportingHorizonDays = "OseonReportingHorizonDays";
    public const string OseonReportingOverdueLookbackDays = "OseonReportingOverdueLookbackDays";
}
