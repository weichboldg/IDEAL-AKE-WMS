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

    // v1.99.81 (Entscheid 4, U-4): Gut Pflicht, Kopfzeile traegt die bisher gemeldete Menge, Fertig belegt mit dem Rest vor
    [Fact]
    public void FertigDialog_GutPflicht_RestVorbelegt()
    {
        Read("Views", "Shared", "_BdeDialoge.cshtml").Should().MatchRegex(@"id=""inputGood""[^>]*required");
        Read("Views", "BdeTeileliste", "_KopfBuchungen.cshtml").Should().Contain("data-bisher=");
        var js = Read("wwwroot", "js", "bde-fa-sicht.js");
        js.Should().Contain("Math.max(0, soll - bisher)").And.Contain("über der Sollmenge");
    }

    // v1.99.81 (K-1): Kopf einklappbar (Bootstrap-collapse), Kurzzeile im Knopf, Zustand je Geraet im localStorage mit try/catch;
    // die Meldungsbox liegt AUSSERHALB des einklappbaren Bereichs (Meldungen bleiben sichtbar).
    [Fact]
    public void Kopf_Einklappbar_MeldungAusserhalb()
    {
        var kopf = Read("Views", "BdeTeileliste", "_Kopf.cshtml");
        kopf.Should().Contain("id=\"bdeKopfBereich\"").And.Contain("data-bs-target=\"#bdeKopfBereich\"").And.Contain("@kurzKopf");
        kopf.IndexOf("id=\"faMeldung\"", StringComparison.Ordinal).Should().BeLessThan(kopf.IndexOf("id=\"bdeKopfBereich\"", StringComparison.Ordinal));
        Read("Views", "BdeTeileliste", "_KopfBuchungen.cshtml").Should().NotContain("id=\"faMeldung\"");
        var js = Read("wwwroot", "js", "bde-teileliste.js");
        var k1 = js[js.IndexOf("(K-1)", StringComparison.Ordinal)..];
        k1.Should().Contain("try {").And.Contain("localStorage.getItem(KOPF)");
    }

    // v1.99.81 (Task 4b, U-13): Werkbank-Auswahl und Teilmeldungen-Link nur fuer BDE-Admin; Wechsel behaelt den Werker
    [Fact]
    public void Kopf_WerkbankwechselUndWaisenLink_NurBdeAdmin()
    {
        var kopf = Read("Views", "BdeTeileliste", "_Kopf.cshtml");
        var admin = kopf.IndexOf("@if (Model.BdeAdmin)", StringComparison.Ordinal);
        admin.Should().BeGreaterThan(0);
        kopf.IndexOf("id=\"teilelisteWerkbank\"", StringComparison.Ordinal).Should().BeGreaterThan(admin);
        kopf.Should().Contain("data-operator-id=\"@Model.Werker.Id\"");
        var waisen = kopf[kopf.IndexOf("@if (Model.Waisen > 0)", StringComparison.Ordinal)..];
        waisen.Should().Contain("<details").And.Contain("BDE-Admin informieren");
        waisen.IndexOf("asp-action=\"Teilmeldungen\"", StringComparison.Ordinal).Should().BeGreaterThan(waisen.IndexOf("@if (Model.BdeAdmin)", StringComparison.Ordinal));
        Read("wwwroot", "js", "bde-teileliste.js").Should().Contain("'&operatorId=' + encodeURIComponent(sel.dataset.operatorId)");
    }

    // v1.99.81 (U-1, U-6, U-9, U-10, U-11)
    [Fact]
    public void FaSicht_LaufendeProduktion_Markiert_SperreNachDialog_Enter()
    {
        var js = Read("wwwroot", "js", "bde-fa-sicht.js");
        js.Should().Contain("case 'QuantityRequired': laufendZeigen(json.bookingId)").And.Contain("bde-kopf-markiert")
            .And.Contain("Solange sie läuft, kann kein zweiter Arbeitsgang starten");
        js.Should().Contain("ev.key === 'Enter'");
        js.Should().Contain("var frei = mitMenge ? function () { } : window.wmsSperren(btn);");
        System.Text.RegularExpressions.Regex.Matches(js, @"box\.replaceChildren\(\);").Count.Should().Be(3);
        Read("wwwroot", "css", "bde.css").Should().Contain("td.tl-zelle-aktion { order: 0; }");
    }
}
