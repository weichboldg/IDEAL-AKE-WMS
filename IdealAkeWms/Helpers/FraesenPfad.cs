namespace IdealAkeWms.Helpers;

/// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F3/F4): Pruefregel und Link des Fraesen-Pfads
/// (<c>Articles.CamPath</c>) an EINEM Ort – genutzt vom einzigen Schreibweg <c>ArticlesController.FraesenPfad</c> und vom Baustein
/// <c>Views/Shared/_FraesenPfad.cshtml</c>.</summary>
public static class FraesenPfad
{
    public const int MaxLaenge = 500;

    public const string Fehlertext =
        @"Bitte als UNC-Pfad eintragen (\\server\freigabe\…). Laufwerk Y: an der Fräse = \\idealfs1\Daten$\Produktion\CAM";

    /// <summary>Getrimmt; leer = kein Pfad (null, gueltig). Sonst muss er mit <c>\\</c> beginnen, darf keine Steuerzeichen enthalten
    /// und hoechstens <see cref="MaxLaenge"/> Zeichen lang sein.</summary>
    public static (string? Wert, string? Fehler) Pruefen(string? eingabe)
    {
        var w = eingabe?.Trim();
        if (string.IsNullOrEmpty(w)) return (null, null);
        if (!IstUnc(w) || w.Length > MaxLaenge || w.Any(char.IsControl)) return (null, Fehlertext);
        return (w, null);
    }

    /// <summary>UNC → <c>file://server/freigabe/…</c> (je Segment <see cref="Uri.EscapeDataString"/>, <c>$</c> bleibt). null, wenn kein UNC.</summary>
    public static string? FileUri(string? unc)
    {
        if (string.IsNullOrWhiteSpace(unc) || !IstUnc(unc.Trim())) return null;
        var segmente = unc.Trim()[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Uri.EscapeDataString(s).Replace("%24", "$"));
        return "file://" + string.Join('/', segmente);
    }

    private static bool IstUnc(string w) => w.Length > 2 && w.StartsWith(@"\\", StringComparison.Ordinal) && w[2] != '\\';
}
