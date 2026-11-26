using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using IdealAkeWms.Tests.Views;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// v1.99.82 (Spec 2026-11-26 FA-Liste nur HauptFA, Teil 1): Repository nurHauptFa + Sub-FA-Suche + Banner-Zaehlung,
/// Kopf-/Wurzelfelder mit Matchcode-Rueckfall, Postfilter je Typ, Drift-Waechter Katalog/View (AK 11/13/14).
/// Die FA-Nr.-SPALTE (LIKE-Praedikat) ist InMemory nicht testbar — Manual-UAT TS-203.
/// </summary>
public class FaListeNurHauptFaTests
{
    private static void Seed(ApplicationDbContext ctx, int id, string order, string sub, bool isDone = false) =>
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = id, OrderNumber = order, SubOrderNumber = sub, Quantity = 1, IsDone = isDone,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });

    private static async Task<ProductionOrderRepository> Repo()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, 1, "100", "100");      // HauptFA-Zeile
        Seed(ctx, 2, "100", "100-1");    // Sub-FA
        Seed(ctx, 3, "100", "100-2");
        Seed(ctx, 4, "200", "");          // HauptFA-Zeile (leere SubOrderNumber)
        Seed(ctx, 5, "300", "300-1");    // Auftrag OHNE HauptFA-Zeile
        Seed(ctx, 6, "400", "400-1", isDone: true); // erledigt, ohne HauptFA-Zeile
        await ctx.SaveChangesAsync();
        return new ProductionOrderRepository(ctx);
    }

    [Fact]
    public async Task NurHauptFa_JeAuftragEineZeile_SubFaAusgeschlossen()
    {
        var page = await (await Repo()).GetForLeitstandGroupedAsync(null, null, null, false, 1, 50, nurHauptFa: true);
        page.Groups.Select(g => g.OrderNumber).Should().Equal("100", "200");
        page.Groups.SelectMany(g => g.Rows).Select(r => r.Id).Should().Equal(1, 4);
        page.TotalGroupCount.Should().Be(2);
    }

    [Theory]
    [InlineData("100-2")] // Sub-FA-Nummer findet ihren HauptFA (F2)
    [InlineData("100")]
    public async Task NurHauptFa_FaNrKarte_FindetHauptFaAuchUeberSubFa(string suche)
    {
        var page = await (await Repo()).GetForLeitstandGroupedAsync(suche, null, null, false, 1, 50, nurHauptFa: true);
        page.Groups.Should().ContainSingle().Which.Rows.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    // v1.99.88 (Spec 2026-11-26 Leitstand nur HauptFA, Teil 2, AK 18): faNrUeberSubFa=false -> nur die HauptFA-Nummer trifft.
    [Theory]
    [InlineData("100-2", 0)]
    [InlineData("100", 1)]
    public async Task NurHauptFa_OhneSubFaSuche_TrifftNurHauptFaNummer(string suche, int erwartet)
    {
        var page = await (await Repo()).GetForLeitstandGroupedAsync(suche, null, null, false, 1, 50, nurHauptFa: true, faNrUeberSubFa: false);
        page.Groups.SelectMany(g => g.Rows).Should().HaveCount(erwartet);
        if (erwartet == 1) page.Groups.Single().Rows.Single().Id.Should().Be(1);
    }

    [Fact]
    public async Task GetZeilenIdsJeAuftrag_HauptUndSubFa_InklErledigte_OhneStornierte()
    {
        var ctx = TestDbContextFactory.Create();
        Seed(ctx, 1, "100", "100");
        Seed(ctx, 2, "100", "100-1", isDone: true);
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Id = 3, OrderNumber = "100", SubOrderNumber = "100-2", Quantity = 1, IsCancelled = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        Seed(ctx, 4, "200", "200");
        await ctx.SaveChangesAsync();

        var zeilen = await new ProductionOrderRepository(ctx).GetZeilenIdsJeAuftragAsync(new[] { "100" });

        zeilen.Select(z => z.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        zeilen.Should().OnlyContain(z => z.OrderNumber == "100");
    }

    [Fact]
    public async Task OhneNurHauptFa_Unveraendert_AlleZeilen()
    {
        var page = await (await Repo()).GetForLeitstandGroupedAsync(null, null, null, false, 1, 50);
        page.Groups.SelectMany(g => g.Rows).Should().HaveCount(5);
    }

    [Theory]
    [InlineData(false, 1)] // nur 300 (offen)
    [InlineData(true, 2)]  // 300 + 400 (erledigt)
    public async Task CountGruppenOhneHauptFa_ZaehltAuftraegeOhneHauptFaZeile(bool showDone, int erwartet)
        => (await (await Repo()).CountGruppenOhneHauptFaAsync(showDone)).Should().Be(erwartet);

    [Fact]
    public async Task Wurzeln_NurSubFaGleichHauptFa_NichtVerschwundeneZuerst()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.FaHierarchyNodes.AddRange(
            new FaHierarchyNode { Id = 1, HauptFA = 100, SubFA = 100, Matchcode = "alt", VerschwundenSeit = DateTime.Today },
            new FaHierarchyNode { Id = 2, HauptFA = 100, SubFA = 100, Matchcode = "neu" },
            new FaHierarchyNode { Id = 3, HauptFA = 100, SubFA = 101 },
            new FaHierarchyNode { Id = 4, HauptFA = 900, SubFA = 900 });
        await ctx.SaveChangesAsync();
        var roots = await new FaHierarchyNodeRepository(ctx).GetRootsByHauptFaKeysAsync(new[] { 100 });
        roots.Select(r => r.Id).Should().Equal(2, 1);
    }

    // ---- Kopf-/Wurzelfelder + Matchcode (AK 6/14)

    [Theory]
    [InlineData("  STRUKTUR  ", "STAMM", "STRUKTUR")] // Wurzel gewinnt, getrimmt
    [InlineData("   ", "STAMM", "STAMM")]             // Wurzel leer -> Artikelstamm
    [InlineData(null, null, null)]                    // beides leer -> leer
    public void Matchcode_StrukturZuerst_ArtikelstammRueckfall(string? wurzel, string? stamm, string? erwartet)
    {
        var item = new ProductionOrderListItem { Matchcode = stamm };
        ProductionOrdersController.StrukturFelderSetzen(item, null, new FaHierarchyNode { HauptFA = 1, SubFA = 1, Matchcode = wurzel }, false);
        item.Matchcode.Should().Be(erwartet);
    }

    [Fact]
    public void Kombigeraet_KopfLeer_WurzelBefuellt()
    {
        var item = new ProductionOrderListItem();
        ProductionOrdersController.StrukturFelderSetzen(item, head: null,
            new FaHierarchyNode { HauptFA = 1, SubFA = 1, Material = "S235", Fertigungmenge = 2 }, isAmbiguous: true);
        item.IsAmbiguous.Should().BeTrue();
        item.PpsStatus.Should().BeNull();
        item.Material.Should().Be("S235");
        item.Fertigungsmenge.Should().Be(2);
    }

    [Fact]
    public void Kopf_FelderAusOrderInfo()
    {
        var item = new ProductionOrderListItem();
        ProductionOrdersController.StrukturFelderSetzen(item, new FaHierarchyOrderInfo
        {
            Status = "FREI", Pos = "3", Dienstleister = "Lackiererei X", RAL = "9016", Montagestunden = 1.5m,
            Beschichten_Retour = new DateTime(2026, 12, 3), Neuer_PT_PPS = new DateTime(2026, 12, 10), Bemerkung_Uhrzeit = "10:00"
        }, root: null, false);
        (item.PpsStatus, item.AbPosition, item.Dienstleister, item.Ral, item.Montagestunden, item.BemerkungUhrzeit)
            .Should().Be(("FREI", "3", "Lackiererei X", "9016", 1.5m, "10:00"));
        item.BeschichtungRetour.Should().Be(new DateTime(2026, 12, 3));
    }

    // ---- Postfilter je Typ (AK 5)

    private static readonly ProductionOrderListItem Zeile = new()
    {
        Ral = "RAL 9016", Material = "S235", Montagestunden = 12.5m, Fertigungsmenge = 3,
        NeuerPtPps = new DateTime(2026, 12, 10), BeschichtungRetour = new DateTime(2026, 12, 3)
    };

    [Theory]
    [InlineData("ral", "9016", true)]
    [InlineData("ral", "!9016", false)]
    [InlineData("material", "s23", true)]
    [InlineData("komm-ziel", "x", false)] // NULL matcht kein „enthaelt"
    [InlineData("montagestunden", ">10", true)]
    [InlineData("montagestunden", "<10", false)]
    [InlineData("fertigungsmenge", "=3", true)]
    public void Postfilter_TextUndZahl(string key, string raw, bool treffer)
    {
        var type = ColumnFilterTypes.Of("ProductionOrders/Index", key);
        ProductionOrdersController.MatchFaListComputedFilter(Zeile, key, ColumnFilterSyntax.Parse(raw, type)).Should().Be(treffer);
    }

    [Theory]
    [InlineData("neuer-pt-pps", ">=01.12.2026", true)]
    [InlineData("beschichtung-retour", ">=05.12.2026", false)]
    public void Postfilter_Datum(string key, string raw, bool treffer)
        => ProductionOrdersController.MatchFaListDateFilter(Zeile, key, ColumnFilterSyntax.Parse(raw, ColumnFilterType.Date)).Should().Be(treffer);

    // ---- Drift-Waechter Katalog / <th> / #column-config (AK 11, AK 9)

    private static string IndexHtml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Views"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "IdealAkeWms", "Views", "ProductionOrders", "Index.cshtml"));
    }

    [Fact]
    public void DriftWaechter_KatalogThUndColumnConfigDeckungsgleich()
    {
        var html = IndexHtml();
        var katalog = ColumnDefinitions.ProductionOrders.Columns.Select(c => c.Key).ToList();
        KommissionierSpaltenKonsistenzTests.ExtractThDataColKeys(html).Distinct().Should().Equal(katalog);
        // #column-config enthaelt Razor-@if, deshalb Keys per Regex statt JSON-Parse.
        Regex.Matches(Regex.Match(html, "id=\"column-config\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value, "\"key\": \"([^\"]+)\"")
            .Select(m => m.Groups[1].Value).Distinct().Should().Equal(katalog);
    }

    [Fact]
    public void NeueSpalten_DefaultHidden_UndGruppenansichtEntfernt()
    {
        var neu = new[] { "pps-status", "ab-position", "dienstleister", "ral", "beschichtung-retour", "neuer-pt-pps", "montagestunden",
            "bemerkung-uhrzeit", "fertigungsmenge", "artikelgruppe", "arbeitsbereich", "komm-ziel", "hauptlagerplatz", "bemerkung",
            "fertigungsinfo", "arbeitsschritte", "material" };
        ColumnDefinitions.ProductionOrders.Columns.Where(c => neu.Contains(c.Key)).Should().HaveCount(17).And.OnlyContain(c => c.DefaultHidden);
        var html = IndexHtml();
        html.Should().NotContain("data-hierarchical").And.NotContain("fa-liste-gruppierung.js").And.NotContain("fa-liste-wiederholung.js");
    }
}
