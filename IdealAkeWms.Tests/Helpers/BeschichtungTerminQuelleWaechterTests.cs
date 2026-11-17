using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// Spec 2026-11-16-beschichtungslogik-je-standort AK 9 (E2): QUELLTEXT-WAECHTER. Kein Controller und kein View liest die
/// Termin-Eingaben (<c>LackierteilKategorieName</c>, <c>BeschichtungTage</c>, <c>BeschichtungAbholtage</c>,
/// <c>Start_Beschichtung</c>) selbst — einzige Stelle ist <c>BeschichtungResolver</c> (Services). Ausgenommen sind nur die
/// Kopfdaten-Anzeigen der Vorschlagsliste/des Auftrags (zeigen die Sage-Kopfdaten als solche) und der Hilfe-Text.
/// </summary>
public class BeschichtungTerminQuelleWaechterTests
{
    private static readonly string[] Ausnahmen =
    {
        "IdealAkeWms/Views/FaHierarchyBeschichtung/_BeschichtungKopf.cshtml",
        "IdealAkeWms/Views/FaHierarchyBeschichtung/_BeschichtungKopfDruck.cshtml",
        "IdealAkeWms/Views/Help/",
    };

    private static readonly Regex Muster = new(@"LackierteilKategorieName|BeschichtungTage|BeschichtungAbholtage|Start_Beschichtung",
        RegexOptions.Compiled);

    [Fact]
    public void Kein_Controller_und_kein_View_liest_die_Termin_Eingaben_direkt_AK9()
    {
        var root = FindRoot();
        var treffer = new[] { ("Controllers", "*.cs"), ("Views", "*.cshtml") }
            .SelectMany(x => Directory.EnumerateFiles(Path.Combine(root, "IdealAkeWms", x.Item1), x.Item2, SearchOption.AllDirectories))
            .Select(f => (Datei: Path.GetRelativePath(root, f).Replace('\\', '/'), Pfad: f))
            .Where(f => !Ausnahmen.Any(a => f.Datei.StartsWith(a, StringComparison.Ordinal)))
            .SelectMany(f => File.ReadLines(f.Pfad).Select((text, i) => (f.Datei, Zeile: i + 1, Text: text)))
            .Where(t => Muster.IsMatch(t.Text))
            .Select(t => $"{t.Datei}:{t.Zeile}: {t.Text.Trim()}")
            .ToList();

        treffer.Should().BeEmpty("Beschichtungstermin und Aktiv-Kennung kommen nur aus BeschichtungResolver (LadenAsync/Bestimmen)");
    }

    [Fact]
    public void Ausnahmen_existieren_noch()
    {
        var root = FindRoot();
        foreach (var a in Ausnahmen)
            (File.Exists(Path.Combine(root, a)) || Directory.Exists(Path.Combine(root, a))).Should().BeTrue(a);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }
}
