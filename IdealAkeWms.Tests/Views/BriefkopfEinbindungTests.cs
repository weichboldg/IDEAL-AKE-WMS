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

    [Theory]
    [InlineData("Print.cshtml")]
    [InlineData("PrintSummiert.cshtml")]
    public void KommissionierDruck_HatKeinenBriefkopf(string file)
        => View("FaHierarchyKommissionierListen", file).Should().NotContain("_PrintBriefkopf");
}
