using System.Globalization;

namespace IdealAkeWms.Services;

/// <summary>Dateiname eines FaHierarchy-PDF: <c>&lt;Dokumentart&gt;_&lt;HauptFA&gt;_&lt;yyyyMMdd-HHmm&gt;.pdf</c>.
/// Reine Funktion; die Dokumentart liefern die Aufrufer als ASCII-Konstante (kein Sanitizing noetig).</summary>
public static class PdfFileNameBuilder
{
    public static string Build(string documentArt, int hauptFa, DateTime timestamp)
        => Build(documentArt, hauptFa.ToString(CultureInfo.InvariantCulture), timestamp);

    /// <summary>Variante mit Text-Kennung (Auftragsnummer <c>BA-JJJJ-NNNNN</c>, systemvergeben und ASCII).</summary>
    public static string Build(string documentArt, string kennung, DateTime timestamp)
        => $"{documentArt}_{kennung}_{timestamp.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.pdf";
}
