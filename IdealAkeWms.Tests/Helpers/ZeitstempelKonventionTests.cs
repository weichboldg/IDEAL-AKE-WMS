using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// v1.99.18 (Spec 2026-11-13-utc-schreiber-auf-lokalzeit, ADR 0025): QUELLTEXT-WAECHTER der Zeitstempel-Konvention.
/// Fachliche und Audit-Zeitstempel werden in Lokalzeit des schreibenden Rechners geschrieben (Web <c>DateTime.Now</c>,
/// Dienst <c>DateTime.Now</c> als SQL-Parameter). Ein UTC-Ausdruck ausserhalb der Ausnahmeliste laesst den Test fehlschlagen.
/// </summary>
public class ZeitstempelKonventionTests
{
    // Datei (relativ zum Repository-Root) -> erlaubte Trefferzahl + Begruendung. Neue Ausnahme nur mit ADR-0025-Nachtrag.
    private static readonly Dictionary<string, (int Treffer, string Grund)> Ausnahmen = new()
    {
        ["IdealAkeWms/Services/PdfRenderService.cs"] = (1, "U1: Vergleich mit Directory.GetCreationTimeUtc, Dateisystem-Zeit, nicht gespeichert"),
        ["IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs"] = (1, "U2: DateTimeOffset im Speicher, Anzeige rechnet um"),
        ["IDEALAKEWMSService/Services/BomCacheSyncService.cs"] = (4, "U3: CachedBomHeaders.CachedAt intern konsistent UTC (2x schreiben, 2x vergleichen), nie angezeigt"),
        ["IdealAkeWms/Services/SyncLogger/SyncRun.cs"] = (1, "nur Kommentar (bewusst nicht UTC)"),
        ["IDEALAKEWMSService/Services/PartRequisitionEmailService.cs"] = (1, "nur Kommentar (bewusst nicht die SQL-Server-Uhr)"),
        ["IdealAkeWms/Views/Help/Changelog.cshtml"] = (1, "historischer Anwender-Changelog-Text, kein Code"),
    };

    private static readonly Regex Utc = new(@"UtcNow|GETUTCDATE|SYSUTCDATETIME", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void Kein_UTC_Schreiber_ausserhalb_der_Ausnahmeliste()
    {
        var root = FindRoot();
        var treffer = Scan(root);

        var verstoesse = treffer
            .GroupBy(t => t.Datei)
            .Where(g => !Ausnahmen.TryGetValue(g.Key, out var a) || g.Count() > a.Treffer)
            .SelectMany(g => g)
            .Select(t => $"{t.Datei}:{t.Zeile}: {t.Text}")
            .ToList();

        verstoesse.Should().BeEmpty(
            "Zeitstempel in Lokalzeit schreiben (DateTime.Now bzw. SQL-Parameter @Jetzt = DateTime.Now), siehe ADR 0025; " +
            "bewusste UTC-Stellen nur ueber die Ausnahmeliste in ZeitstempelKonventionTests");
    }

    [Fact]
    public void Ausnahmeliste_ist_nicht_veraltet()
    {
        // Faellt eine Ausnahme weg (Stelle umgestellt oder Datei verschoben), soll die Liste mitschrumpfen.
        var gezaehlt = Scan(FindRoot()).GroupBy(t => t.Datei).ToDictionary(g => g.Key, g => g.Count());
        foreach (var (datei, (anzahl, grund)) in Ausnahmen)
            gezaehlt.GetValueOrDefault(datei).Should().Be(anzahl, $"Ausnahme {datei} ({grund}) muss genau so oft treffen");
    }

    internal static List<(string Datei, int Zeile, string Text)> Scan(string root)
    {
        var sep = Path.DirectorySeparatorChar;
        bool Ausgeschlossen(string f) =>
            f.Contains($"{sep}bin{sep}") || f.Contains($"{sep}obj{sep}") || f.Contains($"{sep}Migrations{sep}")
            || f.Contains($"{sep}wwwroot{sep}lib{sep}");

        return new[] { "IdealAkeWms", "IDEALAKEWMSService" }
            .SelectMany(p => new[] { "*.cs", "*.cshtml" }
                .SelectMany(m => Directory.EnumerateFiles(Path.Combine(root, p), m, SearchOption.AllDirectories)))
            .Where(f => !Ausgeschlossen(f))
            .SelectMany(f => File.ReadLines(f).Select((text, i) => (f, i + 1, text)))
            .Where(t => Utc.IsMatch(t.text))
            .Select(t => (Path.GetRelativePath(root, t.f).Replace('\\', '/'), t.Item2, t.text.Trim()))
            .ToList();
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }
}
