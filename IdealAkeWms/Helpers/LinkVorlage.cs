namespace IdealAkeWms.Helpers;

/// <summary>
/// Konfigurierbare Fremdsystem-Links (v1.67.0, Spec 2026-10-21-vault-links-per-schalter): EINE Regel fuer Vault und enaio.
/// Schalter mit Default-an-Lesart (nur "false" sperrt, fehlende Zeile = an), leere Vorlage oder leerer Wert = kein Link,
/// Wert immer URL-kodiert. Gelesen in den Partials _VaultLink und _EnaioDmsBadges.
/// </summary>
public static class LinkVorlage
{
    public const string PlatzhalterArtikelnummer = "{Artikelnummer}";
    public const string PlatzhalterObjektId = "{ObjektId}";

    /// <summary>Fertige URL oder <c>null</c> (dann rendert die Stelle kein &lt;a&gt;). Ohne Platzhalter wird der Wert angehaengt.</summary>
    public static string? Build(string? aktivRaw, string? vorlage, string platzhalter, string? wert)
    {
        if (string.Equals(aktivRaw?.Trim(), "false", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(vorlage) || string.IsNullOrWhiteSpace(wert))
            return null;
        var kodiert = Uri.EscapeDataString(wert.Trim());
        var v = vorlage.Trim();
        return v.Contains(platzhalter, StringComparison.Ordinal) ? v.Replace(platzhalter, kodiert, StringComparison.Ordinal) : v + kodiert;
    }
}
