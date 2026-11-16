using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.30 Spec 2026-11-16-bestellvorschlaege-nachbesserung E5: Dringlichkeit nach Bedarfstermin und WBZ (F5, Antwort 6, AK 11, 11a, 12).</summary>
public class PurchaseProposalsNachbesserungE5Tests
{
    private const int Wbz = 10;
    private static readonly DateTime Heute = DateTime.Today;

    private static Article A1(int? wbz = Wbz) => new()
    {
        Id = 1, ArticleNumber = "A1", Description = "A1", ReorderLevel = 0m, Unit = "Stk",
        SageDetail = new ArticleSageDetail { Basismengeneinheit = "Stk", Lagermengeneinheit = "Stk", Wiederbeschaffungszeit = wbz },
    };

    private static SagePurchaseOrderLine Pos(int id, decimal offen, DateTime? termin) => new()
    {
        BelPosId = id, BelId = id, Belegkennzeichen = "EBB", Belegnummer = "2026-" + id, Artikelnummer = "A1", ArticleId = 1,
        MengeBestellt = offen, MengeOffen = offen, Liefertermin = termin, SyncedAt = Heute,
    };

    private static Dictionary<string, List<StockLocationInfo>> Bestand(decimal m) =>
        new() { ["A1"] = new List<StockLocationInfo> { new() { Code = "L1", Quantity = m } } };

    /// <summary>Ein FA-Bedarf von 5 Stk mit Fertigungstermin <paramref name="termin"/> (flach, DEV1 × 1).</summary>
    private static (Mock<IBomCacheRepository>, Mock<IProductionOrderRepository>) FaBedarf(DateTime? termin)
    {
        var bom = new Mock<IBomCacheRepository>();
        bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>> { ["A1"] = new() { ["DEV1"] = 5m } });
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<ProductionOrder>
            { new() { Id = 7, OrderNumber = "FA7", ArticleNumber = "DEV1", Quantity = 1m, ProductionDate = termin, PickingStatus = new() } });
        return (bom, orders);
    }

    private static async Task<PurchaseProposalRow> Zeile(Article a, DateTime? bedarfTermin, List<SagePurchaseOrderLine> bestellungen,
        string? query = null)
    {
        var (bom, orders) = FaBedarf(bedarfTermin);
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { a }, Bestand(0m), bestellungen: bestellungen, bom: bom, orders: orders);
        if (query != null) ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString(query);
        return ((PurchaseProposalListViewModel)((ViewResult)await ctrl.Index(umfang: "alle")).Model!).Items.Single();
    }

    [Theory]
    [InlineData(Wbz + 1, PurchaseProposalDringlichkeit.Kritisch)]       // Bestellung kommt nach heute + WBZ → zaehlt nicht im Fenster
    [InlineData(Wbz, PurchaseProposalDringlichkeit.Nachbestellen)]      // genau heute + WBZ → zaehlt
    [InlineData(-3, PurchaseProposalDringlichkeit.Nachbestellen)]       // ueberfaellig → zaehlt
    public async Task Bestellung_ZaehltNurMitLieferterminImFenster_AK11a(int tage, PurchaseProposalDringlichkeit erwartet)
    {
        var r = await Zeile(A1(), Heute, new() { Pos(1, 5m, Heute.AddDays(tage)) });
        r.Ergebnis.Verfuegbar.Should().Be(0m);   // alle Bestellungen zaehlen weiter in verfuegbar
        r.Ergebnis.Dringlichkeit.Should().Be(erwartet);
    }

    [Fact]
    public async Task BestellungOhneLiefertermin_NichtImFenster_MitHinweis_AK11a()
    {
        var r = await Zeile(A1(), Heute, new() { Pos(1, 5m, null) });
        r.Ergebnis.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Kritisch);
        r.Hinweis.Should().Contain(PurchaseProposalCalculator.HinweisOhneLiefertermin);
    }

    [Fact]
    public async Task BedarfNachHeutePlusWbz_Nachbestellen_OhneTermin_Kritisch_AK11()
    {
        (await Zeile(A1(), Heute.AddDays(Wbz + 30), new())).Ergebnis.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Nachbestellen);
        (await Zeile(A1(), null, new())).Ergebnis.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Kritisch);   // FA ohne Termin = sofort
    }

    [Fact]
    public async Task OhneWbz_NurHeuteFaellig_MitHinweis()
    {
        var r = await Zeile(A1(wbz: null), Heute.AddDays(1), new());
        r.Ergebnis.Dringlichkeit.Should().Be(PurchaseProposalDringlichkeit.Nachbestellen);
        r.Hinweis.Should().Contain(PurchaseProposalCalculator.HinweisOhneWbz);
    }

    [Fact]
    public async Task NaechsterBedarf_SpalteUndDatumsfilter_AK12()
    {
        var termin = Heute.AddDays(20);
        (await Zeile(A1(), termin, new())).NaechsterBedarf.Should().Be(termin);
        var (bom, orders) = FaBedarf(termin);
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { A1() }, Bestand(0m), bestellungen: new(), bom: bom, orders: orders);
        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_naechster-bedarf=<" + Heute.AddDays(10).ToString("dd.MM.yyyy"));
        ((PurchaseProposalListViewModel)((ViewResult)await ctrl.Index(umfang: "alle")).Model!).Items.Should().BeEmpty();
    }

    [Fact]
    public void ArtikelBedarf_Fenster_MaxRegelUndLagerbestellungSofort()
    {
        var b = new ArtikelBedarf(new[]
        {
            new FaBedarf(1, "FA1", null, Heute.AddDays(2), null, 6m, 0m, 0m),
            new FaBedarf(2, "FA2", null, Heute.AddDays(40), null, 9m, 0m, 0m),
        }, Lagerbestellungen: 7m, EinheitAbweichend: 0);
        b.AngerechnetBis(Heute.AddDays(10)).Should().Be(7m);    // max(6, LB 7)
        b.AngerechnetBis(Heute.AddDays(40)).Should().Be(15m);   // max(15, 7)
        b.NaechsterBedarf(Heute).Should().Be(Heute);            // Lagerbestellung = sofort
        PurchaseProposalCalculator.VerfuegbarBis(10m, new[] { (5m, (DateTime?)Heute.AddDays(3)), (4m, (DateTime?)null) }, b, Heute, 10)
            .Should().Be(10m + 5m - 7m);
    }
}
