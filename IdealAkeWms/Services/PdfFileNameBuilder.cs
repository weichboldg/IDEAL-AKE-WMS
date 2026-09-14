using System.Globalization;

namespace IdealAkeWms.Services;

/// <summary>Dateiname eines FaHierarchy-PDF: <c>&lt;Dokumentart&gt;_&lt;HauptFA&gt;_&lt;yyyyMMdd-HHmm&gt;.pdf</c>.
/// Reine Funktion; die Dokumentart liefern die Aufrufer als ASCII-Konstante (kein Sanitizing noetig).</summary>
public static class PdfFileNameBuilder
{
    public static string Build(string documentArt, int hauptFa, DateTime timestamp)
        => $"{documentArt}_{hauptFa.ToString(CultureInfo.InvariantCulture)}_{timestamp.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.pdf";
}
