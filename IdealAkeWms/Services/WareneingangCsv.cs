using System.Text.RegularExpressions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export): gemeinsame Regeln der Wareneingang-CSV fuer Web (Maske, Liste,
/// Hinweise) und Dienst (Schreiben). Ein Ort fuer Keys, Barcode-Regel und Anzeigetexte (Muster <see cref="SageWareneingang"/>).
/// </summary>
public static class WareneingangCsv
{
    /// <summary>ServiceSetting (Default aus): Dienst schreibt die CSV-Dateien.</summary>
    public const string AktivKey = "WareneingangCsvAktiv";
    /// <summary>ServiceSetting (Default leer = nichts schreiben): UNC-Zielordner.</summary>
    public const string PfadKey = "WareneingangCsvPfad";
    /// <summary>ServiceSetting: automatische Wiederholungen nach einem Schreibfehler (Freigabe-Antwort 1d).</summary>
    public const string MaxWiederholungenKey = "Sync:WareneingangCsvMaxWiederholungen";
    /// <summary>ServiceSetting: Mindestabstand in Minuten zwischen Fehler und automatischer Wiederholung.</summary>
    public const string WiederholAbstandKey = "Sync:WareneingangCsvWiederholAbstandMinuten";

    /// <summary>Audit-Kennung des Dienstes (Anlage und Statuswechsel).</summary>
    public const string ServiceActor = "system:wareneingang-csv";

    /// <summary>Freigabe-Antwort 1f: hoechstens so viele Dateien je Worker-Takt (ein haengender UNC-Zugriff haelt den Takt auf).</summary>
    public const int MaxDateienJeTakt = 10;

    public const int BarcodeMaxLaenge = 50;

    /// <summary>Freigabe-Antwort 5: nur A-Z, a-z, 0-9, - und _, 1 bis 50 Zeichen, keine Umformung – der Wert ist der Dateiname. Reservierte
    /// Windows-Geraetenamen (CON, NUL, COM1 …) sind ausgeschlossen, sonst liesse sich die Datei nicht anlegen.</summary>
    private static readonly Regex BarcodeRegex =
        new(@"^(?!(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$)[A-Za-z0-9_-]{1,50}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IstGueltigerBarcode(string? barcode) => barcode != null && BarcodeRegex.IsMatch(barcode);

    public const string BarcodeFehlerText = "Barcode: nur Buchstaben, Ziffern, - und _, höchstens 50 Zeichen (wird als Dateiname verwendet).";

    /// <summary>Pfad-Regel fuer <see cref="PfadKey"/> (ServiceSettings-Muster, SettingValidator): leer oder UNC-Pfad <c>\\server\freigabe[\ordner…]</c>
    /// ohne ungueltige Zeichen. Die Erreichbarkeit prueft der Server nicht (Web-App-Pool ist ein anderes Konto als der Dienst).</summary>
    public const string PfadMuster = @"^(\\\\[^\\/:*?""<>|\r\n]+(\\[^\\/:*?""<>|\r\n]+)+\\?)?$";
    public const string PfadMusterText = @"nur ein UNC-Pfad wie \\server\freigabe\ordner oder leer";

    // ---- Fehlertexte ohne automatische Wiederholung (Freigabe-Antwort 1a/1d) ----
    public const string KollisionText = "Datei existiert bereits – nicht überschrieben.";
    public const string UngueltigText = "Barcode ist als Dateiname ungültig – nicht geschrieben.";
    /// <summary>Diese Fehler wiederholt der Dienst nie automatisch, nur „CSV erneut schreiben“ von Hand.</summary>
    public static readonly string[] NieWiederholen = { KollisionText, UngueltigText };

    public const string AusSageWeText = "Sage-Wareneingang deaktiviert – CSV-Übergabe wartet";

    /// <summary>F4: Hinweise der Liste und von /ServiceSettings (Zeichen und Text im View). Leer = kein Hinweis.</summary>
    /// <param name="wartend">Vorgaenge mit Barcode ohne Ausgangszeile.</param>
    public static List<string> Hinweise(bool csvAn, bool sageWeAn, string? pfad, int wartend)
    {
        var h = new List<string>();
        if (!csvAn) return h;
        if (!sageWeAn) h.Add(AusSageWeText + (wartend > 0 ? $" ({wartend} {(wartend == 1 ? "Wareneingang" : "Wareneingänge")} mit Barcode)." : "."));
        if (string.IsNullOrWhiteSpace(pfad)) h.Add("CSV-Übergabe an, aber kein Pfad gesetzt – es wird nichts geschrieben.");
        return h;
    }

    /// <summary>F4: Text der Spalte „CSV“ je Receipt (nie nur Farbe); <paramref name="export"/> = Ausgangszeile des Vorgangs oder null.</summary>
    public static string StatusText(PurchaseOrderReceipt r, WareneingangCsvExport? export, bool csvAn, bool sageWeAn, string? pfad)
    {
        if (string.IsNullOrEmpty(r.Barcode)) return "–";
        if (r.SageWeVorgang == null) return "keine CSV (nur in Sage)";
        if (export != null)
            return export.Status switch
            {
                WareneingangCsvStatus.Offen => "offen",
                WareneingangCsvStatus.Geschrieben => $"geschrieben {export.GeschriebenAm:dd.MM.yyyy HH:mm}",
                _ => $"Fehler: {export.Fehler}",
            };
        if (r.SageWeStatus != SageWareneingangStatus.Uebergeben) return "wartet auf Sage-Übergabe";
        if (!sageWeAn) return AusSageWeText;
        if (!csvAn) return "CSV-Übergabe aus – wartet";
        if (string.IsNullOrWhiteSpace(pfad)) return "kein CSV-Pfad gesetzt – wartet";
        return "wartet auf den Dienst (nächster Takt)";
    }
}
