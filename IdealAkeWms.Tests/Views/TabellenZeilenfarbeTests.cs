using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// v1.99.62 (Spec 2026-11-23-tabellen-streifen-hover-ci-hellblau, ADR 0035): zentraler QUELLTEXT-WAECHTER der globalen Tabellen-Zeilenfarben.
/// Pixel sieht er nicht (Sichtpruefung TS-187), er nagelt die Muster fest: nur Bootstrap-Zustandsvariablen (Fallstrick 61),
/// --bs-table-active-bg unberuehrt (Gruppenkoepfe), keine Kontextklasse am table, Ausnahmeliste table-plain exakt.
/// </summary>
public class TabellenZeilenfarbeTests
{
    private static DirectoryInfo WebDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return new DirectoryInfo(Path.Combine(dir!.FullName, "IdealAkeWms"));
    }

    private static string Css() => File.ReadAllText(Path.Combine(WebDir().FullName, "wwwroot", "css", "site.css"));

    /// <summary>Alle Regeln, deren Selektor die globale Tabellenregel traegt (ohne Kommentare).</summary>
    private static List<(string Sel, string Body)> GlobaleRegeln()
    {
        var css = Regex.Replace(Css(), @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Matches(css, @"([^{}]*\.table:not\(\.table-plain\)[^{}]*)\{([^}]*)\}")
            .Select(m => (m.Groups[1].Value.Trim(), m.Groups[2].Value)).ToList();
    }

    [Fact]
    public void SiteCss_GlobaleRegeln_NurZustandsvariablen()
    {
        var regeln = GlobaleRegeln();
        regeln.Should().OnlyContain(r => !Regex.IsMatch(r.Body, @"\bbackground"), "nie background am td – table-danger & Co. bleiben (Fallstrick 61)");
        regeln.Should().Contain(r => r.Sel.Contains("tr:nth-of-type(odd)") && r.Body.Contains("--bs-table-bg-type: var(--bs-table-striped-bg)"));
        regeln.Should().Contain(r => r.Sel.EndsWith("tr:hover > *") && r.Body.Contains("--bs-table-bg-state: var(--bs-table-hover-bg)"));
        var fokus = regeln.Where(r => r.Sel.Contains("tr:has(")).ToList();
        fokus.Should().HaveCount(2);
        fokus.Should().OnlyContain(r => r.Sel.Contains(":focus-visible") && r.Sel.Contains("[type=checkbox]") && r.Sel.Contains("select, textarea):focus"));
        fokus[0].Body.Should().Contain("--bs-table-bg-state: var(--wms-table-focus-bg)");
        fokus[1].Sel.Should().EndWith("> :first-child");
        fokus[1].Body.Should().Contain("inset 4px 0 0 var(--bs-primary-text-emphasis)").And.Contain("var(--bs-table-bg-state, var(--bs-table-bg-type");
        regeln.Should().Contain(r => r.Sel.Contains("a:not(.btn, .badge, .vault-link, .dms-link)") && r.Body.Contains("var(--bs-primary-text-emphasis)"));
        foreach (var farbe in new[] { "primary", "success", "danger", "warning", "info" })
            regeln.Should().Contain(r => r.Sel.EndsWith($".text-{farbe}") && r.Body.Contains($"var(--bs-{farbe}-text-emphasis) !important"));
        regeln.Should().Contain(r => r.Sel.EndsWith(".vault-link") && r.Body.Contains("opacity: 1"));
        regeln.Should().NotContain(r => r.Sel.Contains("@media (hover"), "Freigabe-Antwort 5: Hover auch auf Touch");

        var css = Css();
        css.Should().Contain("--bs-table-hover-bg: var(--ake-light-blue)").And.Contain("--wms-table-focus-bg: var(--ake-very-light-blue)");
        Regex.IsMatch(css, @"@media print\s*\{\s*\.table > tbody > tr > \*\s*\{[^}]*--bs-table-bg-type: initial !important;[^}]*box-shadow: none !important")
            .Should().BeTrue("Druck ohne Farbflaechen");
    }

    [Fact]
    public void SiteCss_ActiveBgUnberuehrt_AlteRegelnWeg()
    {
        var css = Regex.Replace(Css(), @"/\*.*?\*/", "", RegexOptions.Singleline);
        css.Should().NotMatchRegex(@"--bs-table-active-bg\s*:", "Gruppenkoepfe tr.table-active wuerden sonst hellblau");
        css.Should().NotContain(".we-positionen").And.NotMatchRegex(@"\.table tbody tr:hover\s*\{");
    }

    private static IEnumerable<(string Datei, string Tag)> TableTags()
    {
        var web = WebDir().FullName;
        var dateien = Directory.GetFiles(Path.Combine(web, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(web, "wwwroot", "js"), "*.js"));
        foreach (var f in dateien)
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"<table\b[^>]*>"))
                yield return (Path.GetRelativePath(web, f).Replace('\\', '/'), m.Value);
    }

    [Fact]
    public void KeinTable_TraegtKontextklasse()
        => TableTags().Where(t => Regex.IsMatch(t.Tag, @"\btable-(primary|secondary|success|info|warning|danger|light|dark)\b"))
            .Should().BeEmpty("die globale .table-Variable wuerde Bootstraps Kontextwert am table ueberschreiben");

    [Fact]
    public void Ausnahmen_TablePlain_GenauDieFreigegebenen()
        => TableTags().Where(t => t.Tag.Contains("table-plain")).Select(t => t.Datei).OrderBy(d => d, StringComparer.Ordinal).Should().Equal(
            "Views/Articles/Info.cshtml",        // Schluessel-Wert-Tabelle Artikelinfo
            "Views/FaHierarchy/Index.cshtml",    // #faTree
            "Views/FaHierarchy/_FaHierarchyNode.cshtml", // fa-head-table (seit v1.99.83 im Partial, D-3)
            "Views/Tracking/Index.cshtml",       // #trackingTree
            "Views/Tracking/OseonIndex.cshtml"); // #oseonTree
}
