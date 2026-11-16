using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.20 Spec 2026-11-08-einkauf-bestellvorschlaege E1 (AK 1-5, 10, 11, 14, 15, 17).</summary>
public partial class PurchaseProposalsControllerTests
{
    private static Article A(int id, string nr, decimal melde, string? unit = "Stk", string? gruppe = null, ArticleSageDetail? sage = null) => new()
    {
        Id = id, ArticleNumber = nr, Description = "Artikel " + nr, ReorderLevel = melde, Unit = unit, ArticleGroup = gruppe, SageDetail = sage,
    };

    private static Dictionary<string, List<StockLocationInfo>> Bestand(params (string Nr, decimal Menge)[] b) =>
        b.ToDictionary(x => x.Nr, x => new List<StockLocationInfo> { new() { Code = "L1", Quantity = x.Menge } });

    internal static (PurchaseProposalsController ctrl, Mock<IAppSettingRepository> settings) Build(List<Article> artikel,
        Dictionary<string, List<StockLocationInfo>>? bestand = null, Dictionary<int, ArticleSupplierRow>? lieferanten = null,
        List<OffenerBedarf>? lager = null, List<OffenerBedarf>? meldungen = null, string? bedarfeEinstellung = null,
        List<SagePurchaseOrderLine>? bestellungen = null, Dictionary<int, ErfasstJeBelPos>? erfasst = null,
        Dictionary<int, ArticleSupplierRow>? letzte = null, bool letzteGelesen = false, Dictionary<int, List<ArticleSupplierRow>>? weitere = null,
        Mock<IBomCacheRepository>? bom = null, Mock<IProductionOrderRepository>? orders = null, Mock<IServiceSettingRepository>? serviceSettings = null,
        Dictionary<(int, string), decimal>? ausgebucht = null, ICurrentUserService? user = null)
    {
        // lieferanten = Hauptlieferant je Artikel (wie bisher), weitere = zusaetzliche Artikel-Lieferanten (F8).
        var alleLieferanten = (lieferanten ?? new()).Select(kv => (kv.Key, kv.Value))
            .Concat((weitere ?? new()).SelectMany(kv => kv.Value.Select(v => (kv.Key, v))))
            .GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToList());
        var lines = new Mock<ISagePurchaseOrderLineRepository>();
        lines.Setup(l => l.GetAllAsync()).ReturnsAsync(bestellungen ?? new List<SagePurchaseOrderLine>());
        lines.Setup(l => l.GetStandAsync()).ReturnsAsync(bestellungen == null ? null : new DateTime(2026, 11, 13, 6, 0, 0));
        var receipts = new Mock<IPurchaseOrderReceiptRepository>();
        receipts.Setup(r => r.GetErfasstJeBelPosAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(erfasst ?? new Dictionary<int, ErfasstJeBelPos>());
        var repo = new Mock<IPurchaseProposalRepository>();
        repo.Setup(r => r.GetNachbestellartikelAsync()).ReturnsAsync(artikel);
        repo.Setup(r => r.GetLieferantenAsync()).ReturnsAsync(alleLieferanten);
        repo.Setup(r => r.GetLetzteBestellungenAsync()).ReturnsAsync(letzte ?? new Dictionary<int, ArticleSupplierRow>());
        repo.Setup(r => r.HatLetzteBestellungenAsync()).ReturnsAsync(letzteGelesen);
        repo.Setup(r => r.GetOffeneLagerbestellungenAsync()).ReturnsAsync(lager ?? new List<OffenerBedarf>());
        repo.Setup(r => r.GetOffeneBedarfsmeldungenAsync()).ReturnsAsync(meldungen ?? new List<OffenerBedarf>());
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetStockByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(bestand ?? new Dictionary<string, List<StockLocationInfo>>());
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.EinkaufOffeneBedarfe)).ReturnsAsync(bedarfeEinstellung);
        // v1.99.30 E4: echter PlannedConsumptionService (dieselbe Regel wie die Artikelinfo); ohne bom/orders keine FA-Bedarfe.
        stock.Setup(s => s.GetAusbuchungenJeFaAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(ausgebucht ?? new Dictionary<(int, string), decimal>());
        if (bom == null)
        {
            bom = new Mock<IBomCacheRepository>();
            bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());
        }
        serviceSettings ??= new Mock<IServiceSettingRepository>();
        var planned = new PlannedConsumptionService(bom.Object, (orders ?? new Mock<IProductionOrderRepository>()).Object, stock.Object, repo.Object,
            settings.Object, serviceSettings.Object);
        var ctrl = new PurchaseProposalsController(repo.Object, stock.Object, settings.Object, user ?? new FakeCurrentUserService(), lines.Object, receipts.Object,
            planned, serviceSettings.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return (ctrl, settings);
    }

    private static PurchaseProposalListViewModel Vm(IActionResult r) => (PurchaseProposalListViewModel)((ViewResult)r).Model!;

    [Fact]
    public async Task NurVorschlaege_Standard_AlleMitUmfang_AK3()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m), A(2, "A2", 10m) }, Bestand(("A1", 85m), ("A2", 11m)));
        Vm(await ctrl.Index()).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A1");
        var alle = Vm(await ctrl.Index(umfang: "alle"));
        alle.Items.Should().HaveCount(2);
        alle.Items.Single(i => i.Article.ArticleNumber == "A2").Ergebnis.Vorschlag.Should().BeNull();
    }

    [Fact]
    public async Task OhneBewegung_Bestand0_Kritisch_Filter_AK4_AK15()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 5m), A(2, "A2", 5m) }, Bestand(("A2", -2m)));
        var vm = Vm(await ctrl.Index());
        vm.Items.Single(i => i.Article.ArticleNumber == "A1").BestandWms.Should().Be(0m);
        Vm(await ctrl.Index(dringlichkeit: "kritisch")).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A2");

        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_dringlichkeit=kritisch");
        Vm(await ctrl.Index()).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A2");
    }

    [Fact]
    public async Task Bedarfe_Einstellung_FehltBeide_LeerKeine_EinheitAbweichendGezaehlt_AK5()
    {
        var lager = new List<OffenerBedarf> { new(" a1 ", "Stk", 3m), new("A1", "m", 7m), new("FREMD", null, 9m) };
        var meldungen = new List<OffenerBedarf> { new("A1", null, 2m) };
        var artikel = new List<Article> { A(1, "A1", 85m) };

        var vm = Vm(await Build(artikel, Bestand(("A1", 100m)), lager: lager, meldungen: meldungen).ctrl.Index(umfang: "alle"));
        vm.Items.Single().OffeneBedarfe.Should().Be(5m);
        // v1.99.30 F4 (Antwort 1): angerechnet = max(FA-Teil 2 [Bedarfsmeldung], Lagerbestellung 3) = 3, nicht 5.
        vm.Items.Single().Ergebnis.Verfuegbar.Should().Be(97m);
        vm.BedarfeEinheitAbweichend.Should().Be(1);

        Vm(await Build(artikel, lager: lager, meldungen: meldungen, bedarfeEinstellung: "").ctrl.Index(umfang: "alle"))
            .Items.Single().OffeneBedarfe.Should().Be(0m);
        Vm(await Build(artikel, lager: lager, meldungen: meldungen, bedarfeEinstellung: "Bedarfsmeldungen").ctrl.Index(umfang: "alle"))
            .Items.Single().OffeneBedarfe.Should().Be(2m);
    }

    [Fact]
    public async Task Lieferant_KreditorFehlt_KtoMitHinweis_Banner_Sortierung_AK10_AK11()
    {
        var mitSage = new ArticleSageDetail { Hauptlieferant = "K99", LagerbestandVerfuegbar = 4m, Wiederbeschaffungszeit = 7 };
        var lieferanten = new Dictionary<int, ArticleSupplierRow>
        {
            [2] = new(10, "Zulieferer", "K31013", true, "B-1", 200m, "Stk", 14, true, 1m),
            [3] = new(11, "Alpha", "K10000", true, null, null, null, null, true),
        };
        var (ctrl, _) = Build(new() { A(1, "A1", 5m, sage: mitSage), A(2, "A2", 5m), A(3, "A3", 5m), A(4, "A4", 5m) }, lieferanten: lieferanten);
        var vm = Vm(await ctrl.Index());

        vm.Items.Select(i => i.Article.ArticleNumber).Should().Equal("A3", "A1", "A2", "A4");   // Alpha, K99, Zulieferer, ohne
        var a1 = vm.Items.Single(i => i.Article.ArticleNumber == "A1");
        a1.LieferantText.Should().Be("K99");
        a1.Hinweis.Should().Contain("Kreditor fehlt im WMS");
        a1.Wbz.Should().Be(7);
        a1.Wunschtermin.Should().Be(DateTime.Today.AddDays(7));
        vm.Items.Single(i => i.Article.ArticleNumber == "A2").Wbz.Should().Be(14);
        vm.Items.Single(i => i.Article.ArticleNumber == "A4").Hinweis.Should().Contain(PurchaseProposalsController.HinweisKeinLieferant).And.Contain("Sage-Zusatzfelder fehlen");
        vm.OhneSageDetail.Should().Be(3);
        vm.OhneArtikelLieferant.Should().Be(2);
        Vm(await ctrl.Index(lieferant: "K99")).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A1");
    }

    [Fact]
    public async Task OhneLetzteBestellung_GrundSichtbar_Rueckfrage13b()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m) }, Bestand(("A1", 60m)));
        var r = Vm(await ctrl.Index()).Items.Single();
        r.Ergebnis.Vorschlag.Should().Be(26m);   // v1.99.30 F6: M − v + 1
        r.Ergebnis.Grund.Should().Be(PurchaseProposalCalculator.GrundOhneLetzteBestellung);
    }

    // ----- v1.99.21 E3: letzte Bestellmenge, Fallback Sage-Bestellmenge (Freigabe-Nachtrag 2) -----

    private static ArticleSupplierRow Lf(int creditorId, string name, decimal? letzte = null, int tag = 1, bool haupt = true) =>
        new(creditorId, name, "K" + creditorId, true, null, null, null, 14, haupt, 1m, letzte, letzte == null ? null : new DateTime(2026, 4, tag), letzte == null ? null : "2027-" + tag);

    private static ArticleSageDetail Sd(decimal? bestellmenge = null, string? basis = "Stk", string? lager = "Stk") =>
        new() { Bestellmenge = bestellmenge, Basismengeneinheit = basis, Lagermengeneinheit = lager };

    [Fact]
    public async Task LetzteBestellung_Hauptlieferant_SonstAndererLieferant_Spalten_AK7()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m, sage: Sd(500m)), A(2, "A2", 85m, sage: Sd(500m)) }, Bestand(("A1", 60m), ("A2", 60m)),
            lieferanten: new() { [1] = Lf(1, "Huber", 400m, tag: 9), [2] = Lf(1, "Huber") },
            letzte: new() { [1] = Lf(2, "Maier", 30m, tag: 20, haupt: false), [2] = Lf(2, "Maier", 30m, tag: 20, haupt: false) }, letzteGelesen: true);
        var vm = Vm(await ctrl.Index());

        var a1 = vm.Items.Single(i => i.Article.ArticleNumber == "A1");
        (a1.Ergebnis.Vorschlag, a1.Ergebnis.Grund, a1.Ergebnis.Fallback).Should().Be(((decimal?)400m, PurchaseProposalCalculator.GrundLetzteBestellmenge, false));
        (a1.LetzteBestellung!.LetzteBestellmenge, a1.LetzteBestellung.LetzteBestellungAm).Should().Be(((decimal?)400m, (DateTime?)new DateTime(2026, 4, 9)));
        var a2 = vm.Items.Single(i => i.Article.ArticleNumber == "A2");
        (a2.Ergebnis.Vorschlag, a2.Ergebnis.Grund).Should().Be(((decimal?)30m, PurchaseProposalCalculator.GrundLetzteBestellmenge));
        a2.Hinweis.Should().Contain("letzte Bestellung bei Maier (K2)");
        vm.FallbackZeilen.Should().Be(0);

        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_letzte-bestellmenge=400");
        Vm(await ctrl.Index()).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A1");
    }

    [Fact]
    public async Task KeineBestellungIn3Jahren_FallbackSageBestellmenge_HinweisFilterbar_Banner_Nachtrag2()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m, sage: Sd(100m)), A(2, "A2", 85m, sage: Sd(null)), A(3, "A3", 85m, sage: Sd(40m)) },
            Bestand(("A1", 60m), ("A2", 60m), ("A3", 60m)), letzteGelesen: true);
        var vm = Vm(await ctrl.Index());

        var a1 = vm.Items.Single(i => i.Article.ArticleNumber == "A1");
        (a1.Ergebnis.Vorschlag, a1.Ergebnis.Grund, a1.Ergebnis.Fallback).Should().Be(((decimal?)100m, PurchaseProposalCalculator.GrundSageBestellmenge, true));
        a1.Hinweis.Should().Contain("Keine Bestellung in 3 Jahren – Sage-Bestellmenge");
        var a2 = vm.Items.Single(i => i.Article.ArticleNumber == "A2");
        (a2.Ergebnis.Vorschlag, a2.Ergebnis.Grund, a2.Ergebnis.Fallback).Should().Be(((decimal?)26m, PurchaseProposalCalculator.GrundOhneLetzteBestellung, false),
            "ohne Sage-Bestellmenge M − v + 1 (F6)");
        vm.Items.Single(i => i.Article.ArticleNumber == "A3").Ergebnis.Vorschlag.Should().Be(40m, "F = 25 < 40: Sage-Bestellmenge selbst");
        vm.FallbackZeilen.Should().Be(2);
        vm.OhneMengenquelle.Should().Be(1);
        a2.Hinweis.Should().Contain("Keine Bestellung in 3 Jahren – Meldebestand", "F3: Ursache statt pauschal");

        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_hinweis=Sage-Bestellmenge");
        Vm(await ctrl.Index()).Items.Select(i => i.Article.ArticleNumber).Should().Equal("A1", "A3");
    }

    [Fact]
    public async Task NochNichtGelesen_KeinFallback_EinheitBasisUngleichLager_LeerMitHinweis_AK8()
    {
        var (ctrl, _) = Build(new() { A(1, "A1", 85m, sage: Sd(100m)), A(2, "A2", 85m, sage: Sd(100m, basis: "Stk", lager: "PKG")) },
            Bestand(("A1", 60m), ("A2", 60m)), lieferanten: new() { [2] = Lf(1, "Huber", 400m) });
        var vm = Vm(await ctrl.Index());

        vm.LetzteBestellungenGelesen.Should().BeFalse();
        var a1 = vm.Items.Single(i => i.Article.ArticleNumber == "A1");
        (a1.Ergebnis.Grund, a1.Ergebnis.Fallback).Should().Be((PurchaseProposalCalculator.GrundOhneLetzteBestellung, false), "ohne gelesene Daten kein Fallback");
        a1.Hinweis.Should().NotContain(PurchaseProposalCalculator.HinweisKeineBestellung, "ohne gelesene Daten keine Aussage ueber 3 Jahre");
        var a2 = vm.Items.Single(i => i.Article.ArticleNumber == "A2");
        a2.LetzteBestellung.Should().BeNull();
        a2.Ergebnis.Grund.Should().Be(PurchaseProposalCalculator.GrundOhneLetzteBestellung);
        a2.Hinweis.Should().Contain("Einheit Stk → PKG nicht umrechenbar – letzte Bestellmenge nicht verwendet");
    }

    [Fact]
    public void Waechter_RolleUndGate_AmController_KeinePostAction_AK1_AK2()
    {
        var t = typeof(PurchaseProposalsController);
        t.GetCustomAttribute<RequireEinkaufAccessAttribute>().Should().NotBeNull();
        t.GetCustomAttribute<RequireEinkaufAktivAttribute>().Should().NotBeNull();
        t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute)).Should().BeEmpty("E5 (Uebergabe an Sage) ist blockiert");
    }

    private static ActionExecutingContext Ctx(Controller c)
    {
        var ac = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        c.TempData = new TempDataDictionary(ac.HttpContext, Mock.Of<ITempDataProvider>());
        return new ActionExecutingContext(ac, new List<IFilterMetadata>(), new Dictionary<string, object?>(), c);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("false", false)]
    [InlineData("1", false)]
    [InlineData("TRUE", true)]
    public async Task Gate_NurExaktTrue_SonstRedirectHome_AK1(string? wert, bool durch)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.EinkaufAktiv)).ReturnsAsync(wert);
        var ctx = Ctx(Build(new()).ctrl);
        var weiter = false;
        await new RequireEinkaufAktivFilter(settings.Object).OnActionExecutionAsync(ctx, () => { weiter = true; return Task.FromResult<ActionExecutedContext>(null!); });
        weiter.Should().Be(durch);
        if (!durch) ctx.Result.Should().BeOfType<RedirectToActionResult>().Which.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task Rolle_Fehlt_AccessDenied_AK2()
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanAccessEinkaufAsync()).ReturnsAsync(false);
        var ctx = Ctx(Build(new()).ctrl);
        await new RequireEinkaufAccessFilter(user.Object).OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));
        ctx.Result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AccessDenied");
    }

    [Fact]
    public void Einstellungen_DefaultAus_BedarfeBeideLeerErlaubt()
    {
        AppSettingDefinitions.All.Single(d => d.Key == AppSettingKeys.EinkaufAktiv).DefaultValue.Should().Be("false");
        var b = AppSettingDefinitions.All.Single(d => d.Key == AppSettingKeys.EinkaufOffeneBedarfe);
        b.Type.Should().Be(SettingType.MultiChoice);
        b.DefaultValue.Should().Be("Lagerbestellungen,Bedarfsmeldungen");
        b.LeerErlaubt.Should().BeTrue();
        RoleKeys.Einkauf.Should().Be("einkauf");
    }
}
