namespace IdealAkeWms.Services;

/// <summary>
/// Erkennt, ob ein User-Agent ein Windows-Desktop-Browser ist (fuer das SSO-UA-Gate).
/// Konservativ: nur eindeutige Windows-Desktops -> true; im Zweifel false (Formular + Button-Fallback).
/// </summary>
public static class UserAgentHelper
{
    // Reihenfolge wichtig: Mobile-Marker VOR dem "Windows NT"-Check pruefen,
    // damit z. B. ein "Windows Phone ... Mobile"-UA nie als Desktop gilt.
    private static readonly string[] MobileMarkers =
        { "Android", "iPhone", "iPad", "iPod", "Windows Phone", "Mobile" };

    /// <summary>null/leer/mobil/Mac/Linux/unbekannt -> false; enthaelt "Windows NT" (und keinen Mobile-Marker) -> true.</summary>
    public static bool IsWindowsDesktop(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;

        foreach (var marker in MobileMarkers)
            if (userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return false;

        return userAgent.Contains("Windows NT", StringComparison.OrdinalIgnoreCase);
    }
}
