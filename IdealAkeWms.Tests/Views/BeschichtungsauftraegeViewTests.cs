using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models.ViewModels;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// QUELLTEXT-WAECHTER der Teil-4-Views (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4, v1.51.0): Spaltenschluessel-
/// Quellen (fallstricke §3: th, #column-config, ColumnDefinitions, Server-Filter-Map, Druck-ShowCol), Zahnrad-Host vor
/// .table-responsive (§19), Briefkopf-Einbindung im Auftragsdruck, Gruppenkopf-Partial in beiden Vorschlagslisten.
/// Aussehen und Bedienung sind Manual-UAT (TS-84).
/// </summary>
public class BeschichtungsauftraegeViewTests
{
    private static readonly string ViewsRoot;

    static BeschichtungsauftraegeViewTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "table-filter.js")))
            dir = dir.Parent;
        ViewsRoot = Path.Combine(dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden"), "IdealAkeWms", "Views");
    }

    private static string View(params string[] p) => File.ReadAllText(Path.Combine(new[] { ViewsRoot }.Concat(p).ToArray()));

    [Fact]
    public void Liste_VierQuellen_Gleich()
    {
        var html = View("Beschichtungsauftraege", "Index.cshtml");
        var definition = ColumnDefinitions.Beschichtungsauftraege.Columns.Select(c => c.Key).ToList();

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(definition);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(definition);
        BeschichtungsauftragListRow.ColumnMap.Keys.Should().BeEquivalentTo(definition, "jede Spalte ist server-filterbar (ADR 0005)");
        ColumnDefinitions.GetByViewKey("Beschichtungsauftraege").Should().BeSameAs(ColumnDefinitions.Beschichtungsauftraege);
        html.Should().Contain("data-server-column-filter=\"true\"").And.Contain("<partial name=\"_Pagination\"");
    }

    [Fact]
    public void Detail_Positionen_DreiQuellen_Gleich_DruckKenntNurDieseSpaltenPlusAnzahl()
    {
        var html = View("Beschichtungsauftraege", "Details.cshtml");
        var definition = ColumnDefinitions.BeschichtungsauftragPositionen.Columns.Select(c => c.Key).ToList();

        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Should().Equal(definition);
        KommissionierSpaltenKonsistenzTests.ExtractColumnConfigKeys(html).Should().Equal(definition);
        ColumnDefinitions.GetByViewKey("BeschichtungsauftragPositionen").Should().BeSameAs(ColumnDefinitions.BeschichtungsauftragPositionen);

        // Druck: summiert, daher ohne Herkunftsspalte, dafuer „Positionen" (immer gedruckt, siehe ShowCol).
        KommissionierSpaltenKonsistenzTests.ExtractShowColKeys(View("Beschichtungsauftraege", "Print.cshtml"))
            .Should().BeEquivalentTo(definition.Where(k => k != "herkunft").Append("positionen"));
    }

    [Theory]
    [InlineData("Index.cshtml")]
    [InlineData("Details.cshtml")]
    public void ZahnradHost_DirektVorTableResponsive(string file)
        => BomZahnradHostTests.TextBeforeTableWrapper("Beschichtungsauftraege", file).Should().EndWith(BomZahnradHostTests.GearHost);

    [Fact]
    public void Druck_BriefkopfAlsErstesElement_EmpfaengerVomAuftrag()
    {
        var html = View("Beschichtungsauftraege", "Print.cshtml");

        html[..html.IndexOf("</head>", StringComparison.Ordinal)].Should().Contain("<partial name=\"_PrintBriefkopfStyles\"");
        Regex.IsMatch(html, @"<div class=""fa-group"">\s*<partial name=""_PrintBriefkopf"" model=""PrintBriefkopfModel\.FuerAuftrag\(")
            .Should().BeTrue("Briefkopf zuerst, Empfaenger = Dienstleister des Auftrags (Antwort 5)");
    }

    [Theory]
    [InlineData("Index.cshtml")]
    [InlineData("Summiert.cshtml")]
    public void Vorschlagsliste_Gruppenkopf_BindetAuftragsPartialEin(string file)
        => Regex.Matches(View("FaHierarchyBeschichtung", file), "<partial name=\"_VorschlagAuftraege\" ").Count.Should().Be(1);

    [Fact]
    public void Briefkopf_FuerAuftrag_EmpfaengerIstDienstleister_OderSichtbarerHinweis()
    {
        var mit = PrintBriefkopfModel.FuerAuftrag("BA-2026-00001", 4711, " Huber GmbH ");
        (mit.Empfaenger, mit.EmpfaengerIstWarnung, mit.Dokument).Should().Be(("Huber GmbH", false, "Beschichtungsauftrag BA-2026-00001"));

        var ohne = PrintBriefkopfModel.FuerAuftrag("BA-2026-00001", 4711, null);
        (ohne.Empfaenger, ohne.EmpfaengerIstWarnung).Should().Be((PrintBriefkopfModel.EmpfaengerFehltAmAuftrag, true));
    }
    // ===== Menü „Beschichtung“ + Hilfe (Spec 2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen, v1.56.0) =====
    // Grenze: Der Wächter beweist die Markup-Struktur, nicht die Sichtbarkeit je Rolle zur Laufzeit — die decken
    // CurrentUserServiceRoleTests (Rollenlogik) und Manual-UAT TS-89 ab.

    private const string Bedingung = "@if (hasBeschichtungsauftragAccess && faBeschichtungAktiv)";

    [Fact]
    public void Navigation_EinDropdownBeschichtung_ZweiEintraege_ActiveFuerBeideController()
    {
        var layout = View("Shared", "_Layout.cshtml");

        Regex.Matches(layout, Regex.Escape(Bedingung)).Count.Should().Be(1, "die Bedingung bleibt wortgleich und einmalig");
        var start = layout.IndexOf(Bedingung, StringComparison.Ordinal);
        var block = layout.Substring(start, layout.IndexOf("</ul>", start, StringComparison.Ordinal) - start);

        var toggle = Regex.Matches(layout, @"<a class=""nav-link dropdown-toggle[^>]*>\s*Beschichtung\s*</a>");
        toggle.Should().ContainSingle();
        block.Should().Contain(toggle[0].Value, "der Toggle steht unter der Bedingung");
        toggle[0].Value.Should().Contain("\"FaHierarchyBeschichtung\"").And.Contain("\"Beschichtungsauftraege\"");

        Regex.Matches(block, @"<a class=""dropdown-item"" asp-controller=""(\w+)"" asp-action=""(\w+)"">([^<]+)</a>")
            .Select(m => $"{m.Groups[1].Value}/{m.Groups[2].Value}={m.Groups[3].Value}")
            .Should().Equal("FaHierarchyBeschichtung/Summiert=Vorschlagsliste", "Beschichtungsauftraege/Index=Aufträge");
        block.Should().NotContain("dropdown-divider", "Nachtrag: keine Trennlinie");
    }

    [Fact]
    public void Navigation_KeineTopLevelLinksMehrAufBeschichtung()
    {
        var layout = View("Shared", "_Layout.cshtml");
        Regex.IsMatch(layout, @"class=""nav-link[^""]*""[^>]*asp-controller=""(FaHierarchyBeschichtung|Beschichtungsauftraege)""",
            RegexOptions.Singleline).Should().BeFalse();
        layout.Should().NotContain(">Beschichtungsauftrag</a>").And.NotContain(">Beschichtungsaufträge</a>");
    }

    [Fact]
    public void Vorschlagslisten_TitelVorschlagsliste_DruckUnveraendert()
    {
        foreach (var v in new[] { "Index.cshtml", "Summiert.cshtml" })
        {
            var html = View("FaHierarchyBeschichtung", v);
            html.Should().Contain("ViewData[\"Title\"] = \"Vorschlagsliste Beschichtung (IDEAL)\";")
                .And.Contain("<h2 class=\"page-header\">Vorschlagsliste Beschichtung (IDEAL)</h2>");
        }
        View("FaHierarchyBeschichtung", "Print.cshtml").Should().Contain("<title>Beschichtungsauftrag (IDEAL)</title>");
    }

    [Fact]
    public void Hilfe_KarteBeschichtung_NachFertigungsauftraegen_AlterMenuewegWeg()
    {
        var help = View("Help", "Index.cshtml");
        var fa = help.IndexOf("<strong>Fertigungsaufträge</strong>", StringComparison.Ordinal);
        var karte = help.IndexOf("<strong>Beschichtung (IDEAL)</strong>", StringComparison.Ordinal);
        var naechste = help.IndexOf("card-header", fa + 1, StringComparison.Ordinal);
        fa.Should().BePositive();
        karte.Should().Be(help.IndexOf("<strong>", naechste, StringComparison.Ordinal), "die Karte folgt direkt auf „Fertigungsaufträge“");

        help.Should().NotContain("Der Menuepunkt <strong>Beschichtungsauftrag</strong>");
        foreach (var alt in new[] { "Beschichtungsauftrag &mdash; Summiert als Standard (seit v1.48.0)",
                     "Briefkopf auf dem Beschichtungsauftrag (seit v1.49.0)", "<dt>IDEAL: Beschichtungsaufträge (seit v1.51.0)</dt>" })
            help.Should().NotContain(alt, "die v1.48/v1.49/v1.51-Abschnitte sind in die neue Karte ueberfuehrt");
        var body = help.Substring(karte, help.IndexOf("card-header", karte + 1, StringComparison.Ordinal) - karte);
        foreach (var h in new[] { "Kurzanleitung", "Vorschlagsliste", "Status", "Reklamation", "Druck und PDF",
                     "Wenn etwas nicht klappt", "Einrichtung (Admin)" })
            body.Should().Contain($"<h6 class=\"mt-3\">{h}");
        body.Should().Contain("<ol");
    }

    [Fact]
    public void Hilfe_KuerzelUndCsv_BeginnenMitNummeriertenSchritten()
    {
        var help = View("Help", "Index.cshtml");
        foreach (var dt in new[] { "<dt>IDEAL: Arbeitsgaenge als Kuerzel im FA-Struktur-Baum (seit v1.52.0)</dt>",
                     "<dt>Liste als CSV exportieren (seit v1.53.0)</dt>" })
        {
            var i = help.IndexOf(dt, StringComparison.Ordinal);
            i.Should().BePositive();
            var dd = help.Substring(i, help.IndexOf("</dd>", i, StringComparison.Ordinal) - i);
            dd.IndexOf("<ol", StringComparison.Ordinal).Should().BeInRange(0, dd.IndexOf("<ul", StringComparison.Ordinal),
                "die Kurzanleitung steht vor den bisherigen Punkten");
        }
    }
}
