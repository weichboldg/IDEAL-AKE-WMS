using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

/// <summary>FA-Liste, C#-Postfilter (M1): Datumsspalten inkl. KO-Termin und Prio als Zahl (AK 2, 3).</summary>
public class FaListePostfilterSyntaxTests
{
    private static ParsedColumnFilter D(string f) => ColumnFilterSyntax.Parse(f, ColumnFilterType.Date);
    private static ParsedColumnFilter N(string f) => ColumnFilterSyntax.Parse(f, ColumnFilterType.Number);

    [Fact]
    public void Datum_VergleichBereichUndKwWieBisher_AK2()
    {
        var item = new IdealAkeWms.Models.ViewModels.ProductionOrderListItem
        {
            ProductionDate = new DateTime(2026, 10, 15), KonstruktionsTermin = new DateTime(2026, 9, 30)
        };
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "production-date", D(">=01.10.2026")).Should().BeTrue();
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "production-date", D("01.10.2026...31.10.2026")).Should().BeTrue();
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "konstruktions-termin", D("<01.10.2026")).Should().BeTrue();
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "production-date", D("kw42")).Should().BeTrue();
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "production-date", D("10.2026")).Should().BeTrue();
        IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListDateFilter(item, "delivery-date", D("(leer)")).Should().BeTrue();
    }

    [Theory]
    [InlineData(">2", 3, true)]
    [InlineData(">2", 2, false)]
    [InlineData("1...3", 3, true)]
    [InlineData("=1", 10, false)]
    [InlineData("=1", 1, true)]
    public void Prio_Numerisch_AK3(string filter, int prio, bool erwartet)
        => IdealAkeWms.Controllers.ProductionOrdersController.MatchFaListComputedFilter(
            new IdealAkeWms.Models.ViewModels.ProductionOrderListItem { Prio = prio }, "prio", N(filter)).Should().Be(erwartet);
}

/// <summary>Quelltext-Waechter (Etappe A + B): keine eigene Filtersyntax ausserhalb des Kerns, Hilfe + aria-describedby,
/// Kern im Layout vor den Seiten-Skripten, Zwischenzustand aus A zurueckgebaut. Bedienung/Kontrast: Manual-UAT TS-91.</summary>
public class ColumnFilterSyntaxQuelltextTests
{
    internal static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js"))) dir = dir.Parent;
        return dir!.FullName;
    }
    internal static string Read(params string[] p) => File.ReadAllText(Path.Combine(new[] { Root(), "IdealAkeWms" }.Concat(p).ToArray()));

    [Fact]
    public void TableFilter_NutztKern_TypImmerAusTh_KeinZwischenzustand()
    {
        var js = Read("wwwroot", "js", "table-filter.js");
        js.Should().NotContain("function matchesFilter").And.NotContain("split(',')").And.NotContain("syntaxMode").And.NotContain("data-filter-syntax");
        js.Should().Contain("ColumnFilterSyntax.matches(").And.Contain("ColumnFilterSyntax.parse(").And.Contain("ColumnFilterSyntax.attachHelp(")
            .And.Contain("isDateCol ? 'date' : (th.getAttribute('data-filter-type') || 'text')");
        var core = Read("wwwroot", "js", "column-filter-syntax.js");
        core.Should().Contain("aria-describedby").And.Contain("Hilfe zur Filtersyntax").And.Contain("is-invalid")
            .And.Contain("MutationObserver", "der ?-Knopf wandert in die erste sichtbare Filterzelle, wenn Spalten ausgeblendet werden");
    }

    [Fact]
    public void KeineView_TraegtNochDenZwischenzustandAusEtappeA()
    {
        var views = Path.Combine(Root(), "IdealAkeWms", "Views");
        Directory.GetFiles(views, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("data-filter-syntax=")).Select(Path.GetFileName).Should().BeEmpty();
    }

    [Fact]
    public void Layout_BindetKernVorSeitenSkriptenEin()
    {
        var layout = Read("Views", "Shared", "_Layout.cshtml");
        layout.IndexOf("column-filter-syntax.js", StringComparison.Ordinal).Should().BePositive()
            .And.BeLessThan(layout.IndexOf("RenderSectionAsync(\"Scripts\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Stueckliste_UndFaBaum_OhneEigeneSyntax_AK11_AK14_AK15()
    {
        var bom = Read("Views", "Picking", "Bom.cshtml");
        var start = bom.IndexOf("function bomMatchesFilter(", StringComparison.Ordinal);
        var body = bom.Substring(start, bom.IndexOf("function bomColKeyToIndex", start, StringComparison.Ordinal) - start);
        body.Should().Contain("ColumnFilterSyntax.matches(").And.NotContain("split(").And.NotContain("startsWith('!')");
        var tree = Read("wwwroot", "js", "fa-hierarchy-tree.js");
        var rm = tree.Substring(tree.IndexOf("function rowMatchesAll(", StringComparison.Ordinal), 400);
        rm.Should().Contain("ColumnFilterSyntax.matches(").And.NotContain("indexOf(f.val)");
        tree.Should().Contain("ColumnFilterSyntax.attachHelp(");
    }

    [Fact]
    public void Server_KeinEfFunctionsLikeMitEingabe_KeineAlteParseSignatur_AK11()
    {
        var src = Path.Combine(Root(), "IdealAkeWms");
        var cs = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            .ToList();
        cs.Where(f => File.ReadAllText(f).Contains("EF.Functions.Like(")).Select(Path.GetFileName).Should().BeEmpty();
        cs.Where(f => File.ReadAllText(f).Contains("ColumnFilterHelper.Parse(")).Select(Path.GetFileName).Should().BeEmpty();
        cs.Where(f => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"\(tokens, negate\)")).Select(Path.GetFileName).Should().BeEmpty();
    }
}

/// <summary>Drift-Waechter (AK 13): je Liste stimmen die typisierten <c>&lt;th&gt;</c> (data-date-filter = Datum,
/// data-filter-type="number" = Zahl) mit dem Server-Register <see cref="ColumnFilterTypes"/> ueberein, und jede Server-Liste mit
/// Typen reicht ihr Register an den Filter weiter — sonst filtern Browser und Server verschieden, ohne Meldung.</summary>
public class ColumnFilterTypesDriftTests
{
    private static readonly string Views = Path.Combine(ColumnFilterSyntaxQuelltextTests.Root(), "IdealAkeWms", "Views");

    private static Dictionary<string, ColumnFilterType> TypedThs(string html)
        => System.Text.RegularExpressions.Regex.Matches(html, @"<th\b[^>]*data-filterable[^>]*>")
            .Select(m => m.Value)
            .Where(t => t.Contains("data-date-filter") || t.Contains("data-filter-type=\"number\""))
            .ToDictionary(
                t => System.Text.RegularExpressions.Regex.Match(t, "data-col-key=\"([^\"]+)\"").Groups[1].Value,
                t => t.Contains("data-date-filter") ? ColumnFilterType.Date : ColumnFilterType.Number);

    public static IEnumerable<object[]> FilterViews() => Directory.GetFiles(Views, "*.cshtml", SearchOption.AllDirectories)
        .Where(f => File.ReadAllText(f).Contains("data-filterable"))
        .Select(f => new object[] { Path.GetRelativePath(Views, f).Replace('\\', '/').Replace(".cshtml", "") });

    [Theory]
    [MemberData(nameof(FilterViews))]
    public void View_UndRegister_GleicheTypen(string view)
    {
        var html = File.ReadAllText(Path.Combine(Views, view + ".cshtml"));
        TypedThs(html).Should().BeEquivalentTo(ColumnFilterTypes.For(view), view);
    }

    [Fact]
    public void Register_ZeigtNurAufVorhandeneViews()
        => ColumnFilterTypes.Lists.Keys.Where(k => !File.Exists(Path.Combine(Views, k + ".cshtml"))).Should().BeEmpty();

    [Fact]
    public void JedeServerListeMitTypen_ReichtIhrRegisterWeiter()
    {
        var src = Path.Combine(ColumnFilterSyntaxQuelltextTests.Root(), "IdealAkeWms");
        var code = string.Join("\n", Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText));
        var clientOnly = new[] { "Beschichtungsauftraege/Details", "Picking/Bom", "Tracking/ByWorkplace" }; // v1.85.0 „Creditors/Details“ ist Server-Mode (QA Q-1)
        ColumnFilterTypes.Lists.Keys.Except(clientOnly)
            .Where(k => !code.Contains($"ColumnFilterTypes.For(\"{k}\")") && !code.Contains($"ColumnFilterTypes.Of(\"{k}\""))
            .Should().BeEmpty("jede Server-Liste mit Zahl-/Datumsspalten muss ihre Typen an Apply bzw. den Postfilter geben");
    }
}

/// <summary>Artikelliste (S2, AK 12 auf Code-Ebene): „beginnt mit“ erzeugt ein indexfaehiges LIKE ohne LOWER; die Laufzeitmessung
/// mit SET STATISTICS laeuft am Testsystem (TS-91.B1).</summary>
public class ArticleColumnFilterSqlTests
{
    private static IdealAkeWms.Data.ApplicationDbContext SqlCtx() => new(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<IdealAkeWms.Data.ApplicationDbContext>()
        .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1").Options);

    [Fact]
    public void BeginntMit_LikeOhneLower_EnthaeltMitEscape()
    {
        using var ctx = SqlCtx();
        string Sql(string key, string f) => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToQueryString(
            IdealAkeWms.Data.Repositories.ArticleRepository.ApplyArticleColumnFilter(ctx.Articles, key, ColumnFilterSyntax.Parse(f, ColumnFilterType.Text)));

        Sql("article-number", "5000%").Should().Contain("LIKE N'5000%'").And.NotContain("LOWER(");
        Sql("description", "a_1").Should().Contain("ESCAPE");
        Sql("category", "(leer)").Should().Contain("IS NULL");
    }
}

/// <summary>QA Epic I-1: HasAny kennt die Typen der Liste — ein Vergleich in einer Zahlspalte filtert per Apply und muss daher auch
/// das Druck-Banner „Gefilterte Ansicht“ ausloesen.</summary>
public class ColumnFilterHelperHasAnyTests
{
    [Fact]
    public void HasAny_VergleichInZahlspalte_GiltAlsFilter()
    {
        var filters = new Dictionary<string, string> { ["sollmenge"] = ">10" };
        ColumnFilterHelper.HasAny(filters, ColumnFilterTypes.For("FaHierarchyBeschichtung/Index")).Should().BeTrue();
        ColumnFilterHelper.HasAny(filters).Should().BeFalse("ohne Typen ist >10 in einer Textspalte wirkungslos");
        ColumnFilterHelper.HasAny(new Dictionary<string, string> { ["artnr"] = ">10" }, ColumnFilterTypes.For("FaHierarchyBeschichtung/Index"))
            .Should().BeFalse("artnr ist Text, der Vergleich wirkt dort nicht");
    }
}
