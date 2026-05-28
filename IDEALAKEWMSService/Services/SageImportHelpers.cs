using System.Globalization;

namespace IDEALAKEWMSService.Services;

internal static class SageImportHelpers
{
    /// <summary>
    /// Parsed den als nvarchar(20) gelieferten Meldebestand-Wert aus Sage.
    /// </summary>
    /// <remarks>
    /// Regeln: NULL/Empty/Whitespace -> null. Parse-Fehler still als null.
    /// Erkannter Wert 0 -> null (Sage liefert "0" oder "0.0000" fuer "kein Meldebestand").
    /// </remarks>
    internal static decimal? ParseReorderLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)) return null;
        return value == 0m ? null : value;
    }
}
