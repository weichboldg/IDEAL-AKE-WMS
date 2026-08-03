namespace IdealAkeWms.Services;

/// <summary>
/// Reine Policy: soll das TLS-Zertifikat des Sage-SData-Servers geprueft werden?
/// Steuert ausschliesslich den Sage-Lagerbuchungs-Client (kein globaler Effekt).
///
/// <b>Fail-safe:</b> Fehlt der Wert oder ist er nicht parsebar, wird GEPRUEFT (true) — niemals
/// stillschweigend die Pruefung abschalten. Nur ein explizit parsebares <c>false</c> deaktiviert sie.
/// Bewusst NICHT <c>ServiceSettings.GetBoolAsync</c> (dessen Semantik waere fail-open: jeder
/// Nicht-"true"-Wert -> false).
/// </summary>
public static class SageTlsPolicy
{
    /// <summary>ServiceSettings-Key (Kategorie „Sage-Lagerbuchung").</summary>
    public const string SettingKey = "SageLagerbuchungSslZertifikatPruefen";

    /// <summary>Warntext bei deaktivierter Pruefung (Log + UI).</summary>
    public const string DisabledWarning =
        "TLS-Zertifikatspruefung fuer Sage-Lagerbuchungen ist DEAKTIVIERT - nur fuer Testsysteme zulaessig";

    /// <summary>True (pruefen), sofern der Wert nicht explizit als <c>false</c> parsebar ist.</summary>
    public static bool ShouldVerifyCertificate(string? rawSetting)
        => !bool.TryParse(rawSetting, out var verify) || verify;
}
