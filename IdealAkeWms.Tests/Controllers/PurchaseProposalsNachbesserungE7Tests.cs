using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.30 Spec 2026-11-16-bestellvorschlaege-nachbesserung E7: Einheiten-Ersatzloesung (F7, AK 14–17 in der Ersatzform).
/// Nie still Faktor 1, keine angenommene Richtung von UmrechnungsFaktorLME (P-N7) / UmrechnungsfaktorEK (P-N2).</summary>
public class PurchaseProposalsNachbesserungE7Tests
{
    private static Article A1(string basis = "Stk", string lager = "Stk", decimal? lme = null, decimal? sageMenge = null) => new()
    {
        Id = 1, ArticleNumber = "A1", Description = "A1", ReorderLevel = 85m, Unit = lager,
        SageDetail = new ArticleSageDetail { Basismengeneinheit = basis, Lagermengeneinheit = lager, UmrechnungsFaktorLME = lme, Bestellmenge = sageMenge },
    };

    private static SagePurchaseOrderLine Pos(int id, decimal offen, string? me, string? basis, string? lager) => new()
    {
        BelPosId = id, BelId = id, Belegkennzeichen = "EBB", Belegnummer = "2026-" + id, Artikelnummer = "A1", ArticleId = 1,
        MengeBestellt = offen, MengeOffen = offen, Liefertermin = DateTime.Today, SyncedAt = DateTime.Today,
        Mengeneinheit = me, MengeBasiseinheit = basis, Lagermengeneinheit = lager,
        EinheitAbweichend = !string.Equals(me, lager, StringComparison.OrdinalIgnoreCase) || !string.Equals(basis, lager, StringComparison.OrdinalIgnoreCase),
    };

    private static Dictionary<string, List<StockLocationInfo>> Bestand(decimal m) =>
        new() { ["A1"] = new List<StockLocationInfo> { new() { Code = "L1", Quantity = m } } };

    private static async Task<PurchaseProposalListViewModel> Vm(Article a, List<SagePurchaseOrderLine> bestellungen, bool letzteGelesen = false)
    {
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { a }, Bestand(60m), bestellungen: bestellungen, letzteGelesen: letzteGelesen);
        return (PurchaseProposalListViewModel)((ViewResult)await ctrl.Index(umfang: "alle")).Model!;
    }

    [Fact]
    public async Task OffenePosition_NurBestelleinheitAbweichend_Eingerechnet_EK005564_AK14()
    {
        // Bestellt in PKG, Basis = Lager = Stk: Cache-Mengen in Stk → zaehlt; 60 + 30 > 85 → kein Vorschlag mehr.
        var vm = await Vm(A1(), new() { Pos(1, 30m, "PKG", "Stk", "Stk") });
        var r = vm.Items.Single();
        r.OffenBestellt.Should().Be(30m);
        r.Ergebnis.IstVorschlag.Should().BeFalse();
        vm.PositionenEinheitAbweichend.Should().Be(0);
    }

    [Fact]
    public async Task OffenePosition_BasisUngleichLager_Draussen_HinweisUndBanner_AK14()
    {
        var vm = await Vm(A1(lager: "PKG"), new() { Pos(1, 30m, "Stk", "Stk", "PKG") });
        var r = vm.Items.Single();
        r.OffenBestellt.Should().Be(0m);
        r.Hinweis.Should().Contain("1 offene Bestellposition(en) Einheit Stk → PKG nicht umrechenbar – nicht gezählt");
        vm.PositionenEinheitAbweichend.Should().Be(1);
    }

    [Theory]
    [InlineData("Stk", "PKG", null, "Einheit Stk → PKG nicht umrechenbar")]   // EK003604: Basis Stk, Lager PKG
    [InlineData("Stk", "Stk", 5.0, "Einheit gleich, Faktor 5")]               // gleiches Kuerzel, Faktor ≠ 1
    public async Task SageBestellmenge_BasisUngleichLager_Gesperrt_KeinFaktor1_BV1_AK16(string basis, string lager, double? lme, string hinweis)
    {
        var r = (await Vm(A1(basis, lager, (decimal?)lme, sageMenge: 100m), new(), letzteGelesen: true)).Items.Single();
        r.Ergebnis.Fallback.Should().BeFalse();
        r.Ergebnis.Vorschlag.Should().Be(26m, "keine Mengenquelle: M − v + 1, nicht 100 × Faktor");
        r.Hinweis.Should().Contain(hinweis + " – Sage-Bestellmenge nicht verwendet, Menge prüfen");
    }

    [Fact]
    public async Task SageBestellmenge_BasisGleichLagerFaktor1_Verwendet()
    {
        var r = (await Vm(A1(lme: 1m, sageMenge: 100m), new(), letzteGelesen: true)).Items.Single();
        (r.Ergebnis.Vorschlag, r.Ergebnis.Fallback).Should().Be(((decimal?)100m, true));
    }

    [Fact]
    public void PositionRechenbar_FehlendeEinheit_ZaehltNicht() =>
        PurchaseProposalsController.PositionRechenbar(Pos(1, 5m, "PKG", null, "Stk")).Should().BeFalse();
}
