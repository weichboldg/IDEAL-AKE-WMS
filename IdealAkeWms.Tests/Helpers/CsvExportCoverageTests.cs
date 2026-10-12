using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>
/// CSV-Export fuer alle Tabellen (Spec 2026-10-06-csv-export-alle-tabellen, v1.53.0, AK 13).
///
/// QUELLTEXT-WAECHTER (Muster <see cref="TableScriptScopedSelectorTests"/>): Layout bindet <c>csv-export.js</c> ein, jede
/// View mit <c>filterable-table</c> nutzt das Layout, jede paginierte Tabelle ist Server-Mode (sonst exportierte sie nur die
/// Seite), <c>csv-export.js</c> iteriert Zeilen/Zellen nur skopiert, <c>table-filter.js</c> stellt den Sortiervergleich EINMAL
/// bereit, Stueckliste und Pagination tragen ihre Attribute. Beweist NICHT, dass der Export im Browser richtig aussieht — das
/// ist Manual-UAT (TS-86).
///
/// VERHALTEN des reinen Kerns (Injection, Zahlen-Ausnahme, KW-Kuerzung, RFC 4180, BOM/CRLF, Dateiname): <see cref="Kern_NodeSelbstpruefung"/>
/// fuehrt <c>IdealAkeWms.Tests/Js/csv-export.check.js</c> mit Node aus; ohne Node auf dem Rechner wird er uebersprungen.
/// </summary>
public class CsvExportCoverageTests
{
    private static readonly string Root;
    private static readonly string Views;

    static CsvExportCoverageTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
        Views = Path.Combine(Root, "IdealAkeWms", "Views");
    }

    private static string Js(string name) => File.ReadAllText(Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", name));

    private static readonly Regex TableTag = new(@"<table\b[^>]*\bfilterable-table\b[^>]*>", RegexOptions.Compiled);

    public static IEnumerable<object[]> TableViews() =>
        Directory.GetFiles(Views, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => TableTag.IsMatch(File.ReadAllText(f)))
            .Select(f => new object[] { Path.GetRelativePath(Views, f) });

    [Fact]
    public void Layout_BindetCsvExportEin_NachSiteJs_VorSeitenSkripten()
    {
        var layout = File.ReadAllText(Path.Combine(Views, "Shared", "_Layout.cshtml"));
        var site = layout.IndexOf("~/js/site.js", StringComparison.Ordinal);
        var csv = layout.IndexOf("<script src=\"~/js/csv-export.js\" asp-append-version=\"true\"></script>", StringComparison.Ordinal);
        var scripts = layout.IndexOf("RenderSectionAsync(\"Scripts\"", StringComparison.Ordinal);
        csv.Should().BeGreaterThan(site).And.BeLessThan(scripts);
    }

    [Fact]
    public void Inventar_MindestensDieSpecTabellen()
        => TableViews().Count().Should().BeGreaterThanOrEqualTo(41, "Spec-Inventar 39 Views + Beschichtungsauftraege Index/Details (v1.51.0)");

    [Theory]
    [MemberData(nameof(TableViews))]
    public void JedeTabellenView_NutztDasLayout_OderOptOut(string view)
    {
        var html = File.ReadAllText(Path.Combine(Views, view));
        var optOut = TableTag.Matches(html).All(m => m.Value.Contains("data-csv-export=\"false\""));
        if (!optOut) html.Should().NotMatchRegex(@"Layout\s*=\s*null", "ohne Layout fehlt csv-export.js — Knopf nie da");
    }

    [Theory]
    [MemberData(nameof(TableViews))]
    public void PaginierteTabelle_IstServerMode(string view)
    {
        var html = File.ReadAllText(Path.Combine(Views, view));
        if (!html.Contains("_Pagination")) return;
        TableTag.Matches(html).Should().OnlyContain(m => m.Value.Contains("data-server-column-filter=\"true\"") || m.Value.Contains("data-csv-export=\"false\""),
            "eine paginierte Client-Mode-Tabelle exportierte nur die aktuelle Seite");
    }

    [Fact]
    public void CsvExportJs_IteriertNurSkopiert()
    {
        var js = Js("csv-export.js");
        js.Should().Contain("':scope > tbody'").And.Contain("':scope > tr'").And.Contain("':scope > thead > tr'");
        foreach (var verboten in new[] { "querySelectorAll('tbody')", "querySelectorAll('tr')", "querySelectorAll('td')", "querySelectorAll('th')",
                     ".rows", ".cells", "getElementsByTagName" })
            js.Should().NotContain(verboten);
    }

    [Fact]
    public void TableFilter_StelltSortiervergleichBereit_UndNutztIhnSelbst()
    {
        var tf = Js("table-filter.js");
        tf.Should().Contain("window.tableFilterCompareValues = compareValues;");
        tf.Should().Contain("return compareValues(cellA.textContent.trim(), cellB.textContent.trim(), dir);");
        Regex.Matches(tf, @"localeCompare\(").Count.Should().Be(1, "der Vergleich steht genau einmal (keine Kopie)");
        Js("csv-export.js").Should().Contain("root.tableFilterCompareValues(").And.NotContain("localeCompare(");
    }

    [Fact]
    public void Stueckliste_SetztFiltertrefferUnabhaengigVomKlappzustand()
    {
        var bom = File.ReadAllText(Path.Combine(Views, "Picking", "Bom.cshtml"));
        bom.Should().Contain("row.setAttribute('data-csv-filter-match', filterVisible ? 'true' : 'false');");
        bom.Should().Contain("id=\"bomTable\" data-view-key=\"Bom\" data-csv-filter-source=\"attribute\"");
    }

    [Fact]
    public void Pagination_TraegtKappungUndGesamtzahl()
    {
        var p = File.ReadAllText(Path.Combine(Views, "Shared", "_Pagination.cshtml"));
        p.Should().Contain("data-capped=\"@(Model.IsCappedAtAll ? \"true\" : \"false\")\"").And.Contain("data-total-count=\"@Model.TotalCount\"");
    }

    [Fact]
    public void Warntext_EnthaeltDenSortierSatz()
        => Js("csv-export.js").Should().Contain("Export auf 5.000 Treffer begrenzt, bitte Filter eingrenzen. Sortierung gilt nur innerhalb der exportierten 5.000.");

    [SkippableFact]
    public void Kern_NodeSelbstpruefung()
    {
        var script = Path.Combine(Root, "IdealAkeWms.Tests", "Js", "csv-export.check.js");
        var module = Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", "csv-export.js");
        Process? p = null;
        try
        {
            p = Process.Start(new ProcessStartInfo("node", $"\"{script}\" \"{module}\"")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Skip.If(true, "Node ist auf diesem Rechner nicht installiert — Kern-Pruefung uebersprungen.");
        }
        using (p!)
        {
            var output = p!.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(30_000).Should().BeTrue();
            p.ExitCode.Should().Be(0, output);
            output.Should().Contain("csv-export core ok");
        }
    }
}
