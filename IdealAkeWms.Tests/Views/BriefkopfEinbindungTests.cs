using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// Briefkopf (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-2, v1.49.0): Empfaengertext des Kopf-Modells und
/// QUELLTEXT-WAECHTER fuer die Einbindung — genau ein Baustein (AK 7), beide Beschichtung-Drucke binden Styles-Partial
/// im head und Kopf-Partial direkt nach &lt;div class="fa-group"&gt; ein (Logozone vor allen Bannern), die
/// Kommissionierlisten nicht (AK 13). Ob der Kopf im Browser/PDF richtig aussieht, ist Manual-UAT (TS-82).
/// </summary>
public class BriefkopfEinbindungTests
{
    private static readonly string ViewsRoot;

    static BriefkopfEinbindungTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        ViewsRoot = Path.Combine(dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden"), "IdealAkeWms", "Views");
    }

    private static string View(params string[] p) => File.ReadAllText(Path.Combine(new[] { ViewsRoot }.Concat(p).ToArray()));

    private static FaHierarchyOrderInfo Info(string? dienstleister) => new() { HauptFA = 1, Dienstleister = dienstleister };

    [Fact]
    public void Empfaenger_OneHead_IsDienstleister()
        => new PrintBriefkopfModel("Beschichtungsauftrag", 1, new[] { Info("Pulverbeschichtung Huber") })
            .Empfaenger.Should().Be("Pulverbeschichtung Huber");

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Empfaenger_MissingDienstleister_IsVisibleHint(string? dienstleister)
        => new PrintBriefkopfModel("Beschichtungsauftrag", 1, new[] { Info(dienstleister) })
            .Empfaenger.Should().Be(PrintBriefkopfModel.EmpfaengerFehlt);

    [Fact]
    public void Empfaenger_NoHead_IsVisibleHint()
        => new PrintBriefkopfModel("Beschichtungsauftrag", 1, Array.Empty<FaHierarchyOrderInfo>())
            .Empfaenger.Should().Be(PrintBriefkopfModel.EmpfaengerFehlt);

    [Fact]
    public void Empfaenger_Kombigeraet_IsAmbiguousHint()
        => new PrintBriefkopfModel("Beschichtungsauftrag", 1, new[] { Info("A"), Info("B") })
            .Empfaenger.Should().Be(PrintBriefkopfModel.EmpfaengerMehrdeutig);

    [Theory]
    [InlineData("Print.cshtml")]
    [InlineData("PrintSummiert.cshtml")]
    public void BeschichtungDruck_BindetBeidePartialsEin_KopfDirektNachFaGroup(string file)
    {
        var html = View("FaHierarchyBeschichtung", file);

        var head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];
        head.Should().Contain("<partial name=\"_PrintBriefkopfStyles\"");

        Regex.IsMatch(html, @"<div class=""fa-group"">\s*<partial name=""_PrintBriefkopf"" ")
            .Should().BeTrue("der Briefkopf steht als erstes Element jedes HauptFA-Dokuments (Logozone vor Bannern)");
        Regex.Matches(html, "<partial name=\"_PrintBriefkopf\" ").Count.Should().Be(1, "ein Baustein, keine Kopie");
        html.Should().NotContain("class=\"briefkopf\"", "das Markup steht nur im Partial (AK 7)");
    }

    // Fusszeile (Spec 2026-10-13-briefkopf-fusszeile-word-vorlage, v1.59.0): EIN Fuss-Partial je Druckseite, HINTER allen
    // Dokumenten (nicht in .fa-group — sonst je HauptFA wiederholt); Seitenzaehlung nur bei genau einem Dokument.
    [Theory]
    [InlineData("FaHierarchyBeschichtung", "Print.cshtml", "Model.Groups.Count == 1")]
    [InlineData("FaHierarchyBeschichtung", "PrintSummiert.cshtml", "Model.Rows.Select(r => r.HauptFA).Distinct().Count() == 1")]
    [InlineData("Beschichtungsauftraege", "Print.cshtml", "true")]
    public void BeschichtungDruck_BindetFussEinmalHinterDenDokumentenEin(string folder, string file, string einDokument)
    {
        var html = View(folder, file);

        Regex.Matches(html, "<partial name=\"_PrintBriefkopfFuss\"").Count.Should().Be(1);
        html.Should().Contain($"<partial name=\"_PrintBriefkopfFuss\" model=\"{einDokument}\" />");
        html.IndexOf("_PrintBriefkopfFuss", StringComparison.Ordinal)
            .Should().BeGreaterThan(html.LastIndexOf("</table>", StringComparison.Ordinal), "hinter dem letzten Dokument");
        html.Should().NotContain("briefkopf-fussbereich", "das Fuss-Markup steht nur im Partial");
    }

    [Fact]
    public void FussPartial_NurMitFussbereich_ZaehlerNurBeiEinemDokument()
    {
        var p = View("Shared", "_PrintBriefkopfFuss.cshtml");
        p.Should().Contain("@if (vorlage.HatFuss)").And.Contain("<footer class=\"briefkopf briefkopf-fussbereich\">");
        p.IndexOf("counter(pages)", StringComparison.Ordinal).Should().BeGreaterThan(p.IndexOf("@if (Model)", StringComparison.Ordinal));

        // Abwaertskompatibel: die Druck-Regeln (@page, Polster) stehen nur im HatFuss-Zweig der Styles.
        var s = View("Shared", "_PrintBriefkopfStyles.cshtml");
        var zweig = s.IndexOf("@if (vorlage.HatFuss)", StringComparison.Ordinal);
        zweig.Should().BeGreaterThan(-1);
        s.IndexOf("@@page", StringComparison.Ordinal).Should().BeGreaterThan(zweig);
        s.IndexOf("padding-bottom: 19.75mm", StringComparison.Ordinal).Should().BeGreaterThan(zweig);
    }

    [Theory]
    [InlineData("Print.cshtml")]
    [InlineData("PrintSummiert.cshtml")]
    public void KommissionierDruck_HatKeinenBriefkopf(string file)
        => View("FaHierarchyKommissionierListen", file).Should().NotContain("_PrintBriefkopf");
}
