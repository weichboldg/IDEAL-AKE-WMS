using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// DRIFT-WAECHTER (kein Verhaltenstest) fuer die vier Spaltenschluessel-Quellen der beiden
/// FaHierarchyKommissionierListen-Views (Bildschirm + Druck, Einzeln + Summiert).
///
/// Eine Spalte lebt an vier Stellen, die von Hand synchron gehalten werden muessen:
///   A) die data-col-key-Attribute der &lt;th&gt; im Bildschirm-View (table-filter.js/Spaltenfilter),
///   B) das inline #column-config-JSON im selben View (column-preferences.js),
///   C) <see cref="ColumnDefinitions"/> (Server-seitige Spalten-Registrierung/Preferences-API),
///   D) die ShowCol(...)-Aufrufe in der zugehoerigen Print.cshtml (druckt nur sichtbare Spalten).
///
/// Was dieser Test beweist: dass A, B und C dieselbe Schluesselmenge enthalten (B zusaetzlich in
/// derselben Reihenfolge wie C), und dass D genau C minus "hauptfa" ist (HauptFA steht in der
/// Druckvorlage immer fest, nie hinter einem ShowCol-Gate).
/// Was er NICHT beweist: dass die Spalten fachlich richtig befuellt sind oder der Druck korrekt
/// aussieht — das ist Manual-UAT (docs/TESTSZENARIEN.md).
/// </summary>
public class KommissionierSpaltenKonsistenzTests
{
    private static readonly string ViewsDir;

    static KommissionierSpaltenKonsistenzTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;

        var root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        ViewsDir = Path.Combine(root, "IdealAkeWms", "Views", "FaHierarchyKommissionierListen");
    }

    public static IEnumerable<object[]> ViewPairs()
    {
        yield return new object[] { "Index.cshtml", "Print.cshtml", ColumnDefinitions.FaHierarchyKommissionierListen };
        yield return new object[] { "Summiert.cshtml", "PrintSummiert.cshtml", ColumnDefinitions.FaHierarchyKommissionierSummiert };
    }

    [Theory]
    [MemberData(nameof(ViewPairs))]
    public void VierQuellen_HabenDieselbenSpaltenschluessel(string screenFile, string printFile, ViewConfig viewConfig)
    {
        var screenHtml = File.ReadAllText(Path.Combine(ViewsDir, screenFile));
        var printHtml = File.ReadAllText(Path.Combine(ViewsDir, printFile));

        var thKeys = ExtractThDataColKeys(screenHtml);
        var configKeys = ExtractColumnConfigKeys(screenHtml);
        var definitionKeys = viewConfig.Columns.Select(c => c.Key).ToList();
        var showColKeys = ExtractShowColKeys(printHtml);

        var thSet = thKeys.ToHashSet();
        var configSet = configKeys.ToHashSet();
        var definitionSet = definitionKeys.ToHashSet();
        var showColSet = showColKeys.ToHashSet();
        var expectedShowColSet = definitionSet.Where(k => k != "hauptfa").ToHashSet();

        AssertSameSet(thSet, definitionSet, $"<th data-col-key> in {screenFile}", "ColumnDefinitions");
        AssertSameSet(configSet, definitionSet, $"#column-config in {screenFile}", "ColumnDefinitions");

        configKeys.Should().Equal(definitionKeys,
            $"die Reihenfolge im #column-config-JSON von {screenFile} muss der Reihenfolge in " +
            $"ColumnDefinitions.{viewConfig.ViewKey}.Columns entsprechen (column-preferences.js reiht danach)");

        AssertSameSet(showColSet, expectedShowColSet, $"ShowCol(...) in {printFile}", "ColumnDefinitions minus \"hauptfa\"");
    }

    private static void AssertSameSet(HashSet<string> actual, HashSet<string> expected, string actualLabel, string expectedLabel)
    {
        var missing = expected.Except(actual).ToList();
        var extra = actual.Except(expected).ToList();

        (missing.Count == 0 && extra.Count == 0).Should().BeTrue(
            $"{actualLabel} soll dieselben Spaltenschluessel wie {expectedLabel} enthalten. " +
            $"Fehlend in {actualLabel}: [{string.Join(", ", missing)}]. " +
            $"Zusaetzlich in {actualLabel}: [{string.Join(", ", extra)}].");
    }

    private static readonly Regex ThDataColKeyRegex = new(
        """<th\b[^>]*\bdata-col-key="([^"]+)"[^>]*>""", RegexOptions.Compiled);

    private static List<string> ExtractThDataColKeys(string html) =>
        ThDataColKeyRegex.Matches(html).Select(m => m.Groups[1].Value).ToList();

    private static readonly Regex ColumnConfigScriptRegex = new(
        """<script type="application/json" id="column-config">(.*?)</script>""",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static List<string> ExtractColumnConfigKeys(string html)
    {
        var match = ColumnConfigScriptRegex.Match(html);
        match.Success.Should().BeTrue("jeder Bildschirm-View braucht ein #column-config-Script fuer column-preferences.js");

        using var doc = JsonDocument.Parse(match.Groups[1].Value);
        return doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("key").GetString()!)
            .ToList();
    }

    private static readonly Regex ShowColRegex = new(
        """ShowCol\("([^"]+)"\)""", RegexOptions.Compiled);

    private static List<string> ExtractShowColKeys(string html) =>
        ShowColRegex.Matches(html).Select(m => m.Groups[1].Value).Distinct().ToList();
}
