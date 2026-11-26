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
            dialoge.Should().Contain($"id=\"{id}\"");
        // v1.99.78 (Etappe 5): Terminal-Seite abgeloest – View und bde-terminal.js sind geloescht, die Partial nutzt nur noch die FA-Sicht.
        File.Exists(Path.Combine(Root, "IdealAkeWms", "Views", "BdeTerminal", "Index.cshtml")).Should().BeFalse();
        File.Exists(Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", "bde-terminal.js")).Should().BeFalse();

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

    // v1.99.81 (3A, 10A): Gruppen-Abschluss breit, Werkbank-Spalte, ganze Zahlen, Pflichtfelder, eine Sammel-Rueckfrage unter Soll
    [Fact]
    public void GruppenAbschluss_Breit_WerkbankSpalte_PflichtUndGanzzahlig()
    {
        var dialoge = Read("Views", "Shared", "_BdeDialoge.cshtml");
        var gruppe = dialoge[dialoge.IndexOf("id=\"group-finish-modal\"")..dialoge.IndexOf("id=\"close-others-modal\"")];
        gruppe.Should().Contain("modal-xl").And.Contain(">Werkbank<").And.Contain("btn-outline-secondary");

        var js = Read("wwwroot", "js", "bde-fa-sicht.js");
        var fn = js[js.IndexOf("function gruppe(")..js.IndexOf("var start = function")];
        fn.Should().Contain("i.required = true").And.Contain("i.step = '1'").And.Contain("subOrderNumber").And.Contain("workplaceName")
            .And.Contain("window.bdeBestaetigen").And.Contain("letzteFertig");
    }
}
