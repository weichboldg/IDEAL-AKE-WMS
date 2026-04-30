namespace IdealAkeWms.Models;

/// <summary>
/// Typed access to well-known AppSettings keys. Prevents silent typo-fallbacks.
/// </summary>
public static class AppSettingKeys
{
    // BDE
    public const string BdeAktiv = "BdeAktiv";
    public const string BdeNurFaMeldung = "BdeNurFaMeldung";
    public const string BdeDefaultArbeitsgang = "BdeDefaultArbeitsgang";
    public const string BdeMehrfachBuchungProOperator = "BdeMehrfachBuchungProOperator";
    public const string BdeMehrfachBuchungProArbeitsgang = "BdeMehrfachBuchungProArbeitsgang";
    public const string BdeGleichzeitigerAbschlussBeiMehrfachStart = "BdeGleichzeitigerAbschlussBeiMehrfachStart";
    public const string BdeSchichtkalenderAktiv = "BdeSchichtkalenderAktiv";

    // Picking / Leitstand
    public const string LeitstandAktiv = "LeitstandAktiv";
    public const string KommissionierungMitZuweisung = "KommissionierungMitZuweisung";
    public const string BestellungenAktiv = "BestellungenAktiv";

    // Stock / Buchung
    public const string NegativeBuchungErlaubt = "NegativeBuchungErlaubt";
    public const string NegativeBuchungLagerplatz = "NegativeBuchungLagerplatz";
    public const string QrMitFaNummer = "QrMitFaNummer";

    // Tracking / Beschichtung
    public const string TeileverfolgungAktiv = "TeileverfolgungAktiv";
    public const string LackierteilKategorieName = "LackierteilKategorieName";
    public const string BeschichtungAbholtage = "BeschichtungAbholtage";

    // OSEON / Reporting
    public const string OseonReportingHorizonDays = "OseonReportingHorizonDays";
    public const string OseonReportingOverdueLookbackDays = "OseonReportingOverdueLookbackDays";
}
