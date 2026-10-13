using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Spaltenfilter-Suchsyntax, Etappe A (Spec 2026-09-23-suchsyntax-spaltenfilter-erweitern, v1.58.0): EINE Testtabelle
/// (<c>IdealAkeWms.Tests/Js/column-filter-cases.json</c>) gegen den C#-Speicher-Auswerter, den SQL-Bauer (per LINQ-to-Objects
/// kompiliert, Zelle kleingeschrieben = CI-Kollation) und — per Node — den JS-Spiegel (AK 5, gleiche Treffer client/server).
/// </summary>
public class ColumnFilterSyntaxTests
{
    private static readonly string Root = FindRoot();
    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static readonly string CasesPath = Path.Combine(Root, "IdealAkeWms.Tests", "Js", "column-filter-cases.json");

    public sealed record Case(string Id, string Type, string Filter, string Cell, bool Match, bool Invalid = false, bool Sql = false);

    private static List<Case> Load() => JsonSerializer.Deserialize<List<Case>>(File.ReadAllText(CasesPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    public static IEnumerable<object[]> Ids() => Load().Select(c => new object[] { c.Id });

    private static ColumnFilterType TypeOf(string t) => t switch
    {
        "number" => ColumnFilterType.Number, "date" => ColumnFilterType.Date, _ => ColumnFilterType.Text
    };

    [Theory]
    [MemberData(nameof(Ids))]
    public void Tabelle_SpeicherAuswerter(string id)
    {
        var c = Load().Single(x => x.Id == id);
        var type = TypeOf(c.Type);
        var parsed = ColumnFilterSyntax.Parse(c.Filter, type);

        ColumnFilterSyntax.Matches(c.Cell, parsed, type).Should().Be(c.Match, id);
        (parsed.Invalid.Count > 0).Should().Be(c.Invalid, id);
    }

    [Fact]
    public void Tabelle_SqlBauer_GleicheTreffer()
    {
        var sqlCases = Load().Where(c => c.Sql).ToList();
        sqlCases.Should().HaveCountGreaterThan(15);
        foreach (var c in sqlCases)
        {
            var pred = ColumnFilterSql.Predicate<Zeile>(ColumnFilterSyntax.Parse(c.Filter, ColumnFilterType.Text), z => z.Wert);
            pred.Should().NotBeNull(c.Id);
            pred!.Compile()(new Zeile(c.Cell.ToLowerInvariant())).Should().Be(c.Match, c.Id);
        }
    }

    public sealed record Zeile(string? Wert, string? Zweit = null);

    [Fact]
    public void SqlBauer_MehrereSpalten_EinTeilTrifftEine_LeerNurWennAlleLeer()
    {
        var p = ColumnFilterSql.Predicate<Zeile>(ColumnFilterSyntax.Parse("=4711", ColumnFilterType.Text), z => z.Wert, z => z.Zweit)!.Compile();
        p(new Zeile("100", "4711")).Should().BeTrue();
        var leer = ColumnFilterSql.Predicate<Zeile>(ColumnFilterSyntax.Parse("(leer)", ColumnFilterType.Text), z => z.Wert, z => z.Zweit)!.Compile();
        leer(new Zeile(null, "x")).Should().BeFalse();
        leer(new Zeile(" ", null)).Should().BeTrue();
        var neg = ColumnFilterSql.Predicate<Zeile>(ColumnFilterSyntax.Parse("!x", ColumnFilterType.Text), z => z.Wert)!.Compile();
        neg(new Zeile(null)).Should().BeTrue("Zeilen ohne Wert treffen bei !x (Regel 10)");
    }

    [Fact]
    public void SqlBauer_NurUngueltigeTeile_KeinPraedikat()
        => ColumnFilterSql.Predicate<Zeile>(ColumnFilterSyntax.Parse("1...5", ColumnFilterType.Text), z => z.Wert).Should().BeNull();

    [Fact]
    public void Apply_AlteEingabenUnveraendert_TypenOptional()
    {
        var rows = new[] { "960", "886", "123", "" };
        var map = new Dictionary<string, Func<string, string?>> { ["k"] = r => r };
        ColumnFilterHelper.Apply(rows, new Dictionary<string, string> { ["k"] = "960,886" }, map).Should().Equal("960", "886");
        ColumnFilterHelper.Apply(rows, new Dictionary<string, string> { ["k"] = "!960" }, map).Should().Equal("886", "123", "");
        ColumnFilterHelper.Apply(rows, new Dictionary<string, string> { ["k"] = ">200" }, map,
            new Dictionary<string, ColumnFilterType> { ["k"] = ColumnFilterType.Number }).Should().Equal("960", "886");
        ColumnFilterHelper.Apply(rows, new Dictionary<string, string> { ["k"] = ">200" }, map)
            .Should().HaveCount(4, "in einer Textspalte ist ein Vergleich ungueltig und damit wirkungslos (Antwort 6)");
    }

    [SkippableFact]
    public void Kern_NodeSelbstpruefung_GleicheTabelle()
    {
        var script = Path.Combine(Root, "IdealAkeWms.Tests", "Js", "column-filter-syntax.check.js");
        var module = Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", "column-filter-syntax.js");
        Process? p = null;
        try
        {
            p = Process.Start(new ProcessStartInfo("node", $"\"{script}\" \"{module}\" \"{CasesPath}\"")
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
            output.Should().Contain("column-filter-syntax core ok");
        }
    }
}
