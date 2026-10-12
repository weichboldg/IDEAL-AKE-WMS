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
}
