using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung AK 19 (ADR 0030): QUELLTEXT-WAECHTER. Welche Werkbank meldet, entscheidet
/// allein ihr Arbeitsschritt-Kuerzel (Stammdaten) - kein Kuerzel als Konstante im Produktivcode der Teileliste, der Teilmeldung und der
/// Struktur-Erkennung. Die Liste der Kuerzel stammt aus P-TL1 (IDEAL); ein neues Kuerzel gehoert in die Stammdaten, nicht hierher.
/// </summary>
public class TeilelisteKuerzelKonstantenWaechterTests
{
    private static readonly string[] Dateien =
    {
        "IdealAkeWms/Services/BdeTeilelisteService.cs",
        "IdealAkeWms/Controllers/BdeTeilelisteController.cs",
        "IdealAkeWms/Controllers/BdeBookingsController.cs",
        "IdealAkeWms/Services/BdeBookingService.cs",
        "IdealAkeWms/Services/WerkbankKuerzelFamilie.cs",
        "IdealAkeWms/Views/BdeTeileliste/",
        "IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml",
        "IdealAkeWms/wwwroot/js/bde-teileliste.js",
        "IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs",
    };

    private static readonly Regex Muster = new(@"[""'](KA|ST|SL|SM|PU|ZU|IF|LA|LS|PG|VM)[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    public void Keine_Kuerzel_Konstante_im_Produktivcode_AK19()
    {
        var root = FindRoot();
        var treffer = Dateien
            .SelectMany(d => Directory.Exists(Path.Combine(root, d))
                ? Directory.EnumerateFiles(Path.Combine(root, d), "*", SearchOption.AllDirectories)
                : new[] { Path.Combine(root, d) })
            .SelectMany(f => File.ReadLines(f).Select((text, i) => (Datei: Path.GetRelativePath(root, f), Zeile: i + 1, Text: text)))
            .Where(t => Muster.IsMatch(t.Text))
            .Select(t => $"{t.Datei}:{t.Zeile}: {t.Text.Trim()}")
            .ToList();

        treffer.Should().BeEmpty("das Kuerzel kommt aus ProductionWorkplace.ArbeitsschrittCode, nie aus dem Code");
    }

    [Fact]
    public void Dateien_existieren_noch()
    {
        var root = FindRoot();
        foreach (var d in Dateien)
            (File.Exists(Path.Combine(root, d)) || Directory.Exists(Path.Combine(root, d))).Should().BeTrue(d);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }
}
