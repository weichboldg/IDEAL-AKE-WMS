using System.Text.RegularExpressions;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.9 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe, Freigabe-Nachtrag 14:20): System-Kürzel in den Sage-Kennungen
/// (<c>WE#…#</c> Sage-Wareneingang, <c>SM#…#</c> Sage-Lagerbuchung). Mehrere WMS-Datenbanken (DEV, AKE-Test, AKE Test-IDEAL) schreiben in
/// DIESELBE Sage-Testdatenbank, ihre Ids überschneiden sich; ohne Kürzel fände die Memo-Suche vor dem erneuten Senden den Beleg eines anderen
/// WMS und sendete nie. Produktion: leer = bisheriges Format. EIN Ort für Format und Prüfung (Web und Dienst).
/// </summary>
public static class SageSystemKuerzel
{
    /// <summary>ServiceSetting (Default leer).</summary>
    public const string Key = "Sync:SageSystemKuerzel";

    /// <summary>Großbuchstabe vorne, dann Großbuchstaben/Ziffern, höchstens 5 Zeichen; leer erlaubt. Der Buchstabe vorne ist Pflicht: ein
    /// Kürzel aus Ziffern („12“) ergäbe <c>WE#12#7#</c>, und die Suche eines Systems ohne Kürzel nach <c>%WE#12#%</c> träfe es.</summary>
    public const string Muster = "^([A-Z][A-Z0-9]{0,4})?$";

    public const string MusterText = "leer oder 1 bis 5 Zeichen, Großbuchstaben und Ziffern, beginnend mit einem Buchstaben (z. B. DEV, AT, ATI, IT)";

    public static bool Gueltig(string? roh) => Regex.IsMatch(roh?.Trim() ?? "", Muster);

    /// <summary>Für den Dienst: getrimmtes Kürzel (leer → null). Ungültig (von Hand in der DB) → Ausnahme, damit nichts mit einer falschen
    /// Kennung gesendet wird (ein <c>%</c> wäre in der LIKE-Suche ein Platzhalter).</summary>
    public static string? Pruefen(string? roh) => Gueltig(roh)
        ? (string.IsNullOrWhiteSpace(roh) ? null : roh.Trim())
        : throw new InvalidOperationException($"ServiceSetting {Key} = „{roh}“ ist ungültig – erlaubt: {MusterText}. Nichts gesendet.");

    /// <summary>Die EINE Formatfunktion: <c>&lt;praefix&gt;#&lt;id&gt;#</c> ohne Kürzel, <c>&lt;praefix&gt;#&lt;K&gt;#&lt;id&gt;#</c> mit Kürzel.</summary>
    public static string Kennung(string praefix, string? kuerzel, int id) =>
        string.IsNullOrWhiteSpace(kuerzel) ? $"{praefix}#{id}#" : $"{praefix}#{kuerzel.Trim()}#{id}#";
}
