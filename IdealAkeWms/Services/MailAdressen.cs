using System.Net.Mail;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug F3, ADR 0020): EINE Regel fuer Mail-Adressen – Pruefung beim Speichern
/// (<c>SettingValidator</c>) und vor dem Versand der Lieferverzug-Mails im Dienst. Gueltig ist nur die nackte Adresse (kein Anzeigename,
/// keine Liste): <c>abc@</c>, <c>a@b.at; c@d.at</c> und <c>Name &lt;a@b.at&gt;</c> sind ungueltig.
/// </summary>
public static class MailAdressen
{
    public static bool IstGueltig(string? adresse)
    {
        var t = adresse?.Trim();
        return !string.IsNullOrEmpty(t) && MailAddress.TryCreate(t, out var m) && m.Address == t && string.IsNullOrEmpty(m.DisplayName);
    }

    /// <summary>Adressliste aus Komma, Semikolon oder Zeilenumbruch, getrimmt, ohne Leere und Doppelte (Gross/Klein egal).</summary>
    public static List<string> Liste(string? roh) => (roh ?? "")
        .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
