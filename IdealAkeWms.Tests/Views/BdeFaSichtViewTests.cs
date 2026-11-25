using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>v1.99.77 (Spec 2026-11-25-bde-masken-vereinheitlichung Etappe 4b): FA-Sicht bucht ueber die bestehenden Terminal-Aktionen,
/// Dialoge EINMAL (_BdeDialoge fuer beide Seiten), keine Browser-Dialoge, ein sichtbares Scan-Ziel.</summary>
public class BdeFaSichtViewTests
{
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "bde-fa-sicht.js")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root, "IdealAkeWms" }.Concat(parts).ToArray()));

    [Fact]
    public void Dialoge_EinmalGemeinsam_KeineBrowserDialoge_ScanZiel()
    {
        var dialoge = Read("Views", "Shared", "_BdeDialoge.cshtml");
        foreach (var id in new[] { "quantityModal", "collisionModal", "group-finish-modal", "close-others-modal" })
        {
            dialoge.Should().Contain($"id=\"{id}\"");
            Read("Views", "BdeTerminal", "Index.cshtml").Should().NotContain($"id=\"{id}\"", "das Terminal nutzt die gemeinsame Partial");
        }
        Read("Views", "BdeTerminal", "Index.cshtml").Should().Contain("<partial name=\"_BdeDialoge\" />");

        var fa = Read("Views", "BdeTeileliste", "Fa.cshtml");
        fa.Should().Contain("<partial name=\"_BdeDialoge\" />").And.Contain("bde-fa-sicht.js");
        System.Text.RegularExpressions.Regex.Matches(fa, @"\bdata-hw-scan-ziel\b").Count.Should().Be(1, "Regel (d): genau ein Scan-Ziel");

        var js = Read("wwwroot", "js", "bde-fa-sicht.js");
        js.Should().NotContainAny("confirm(", "alert(", "prompt(");  // auch nicht im Kommentar
        js.Should().Contain("resolve-scan?faSicht=true", "G5: die FA-Sicht liest BdeNurFaMeldung nicht");
        foreach (var aktion in new[] { "StartSetup", "StartProduction", "StartActivity", "Pause", "Resume", "Finish", "ReportPartial",
                     "ReportCompletion", "FinishGroup", "CloseOthers" })
            js.Should().Contain($"'/BdeTerminal/{aktion}'", "bestehende JSON-Aktionen statt neuer Buchungswege");
    }
}
