using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.47 (Spec [[2026-08-05-wms-bugs-improvements-teil-6-spec]]): „Lagerplatz ausbuchen“ mit FA-Filter –
/// Serverpruefung, gemischte Zeilen, Wagen-Vorbelegung, IDEAL Haupt-FA. AK 4–8, 11, 12, 14 (a)–(g), 16, 18, 20, 24, S-7.</summary>
public class LagerplatzAusbuchenFaFilterTests
{
    private const int Platz = 5;
    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly Mock<IStorageLocationRepository> _locs = new();
    private readonly Mock<IProductionOrderRepository> _po = new();
    private readonly Mock<IServiceSettingRepository> _svc = new();
    private readonly List<StockMovement> _gebucht = new();
    private List<StockOverviewItem> _bestand = new();
    private Dictionary<int, FaAmPaar>? _fa = new();
    private bool _wagen;
    private TempDataDictionary _temp = null!;

    // Ausgangslage 173.4: A (10) „1234567“ 4 Stk, B (2) ohne FA 5, C (3) „111, 222“ 3, N (1) „111“ 2.
    public LagerplatzAusbuchenFaFilterTests()
    {
        Zeile(10, "A", 4m, Zugang("1234567"));
        Zeile(2, "B", 5m, new FaAmPaar([], new(), true));
        Zeile(3, "C", 3m, Zugang("111", "222"));
        Zeile(1, "N", 2m, Zugang("111"));
    }

    private static FaAmPaar Zugang(params string[] nr) => new(nr.ToList(), nr.ToHashSet(StringComparer.OrdinalIgnoreCase), false);

    private void Zeile(int id, string nr, decimal menge, FaAmPaar? fa)
    {
        _bestand.Add(new StockOverviewItem { ArticleId = id, ArticleNumber = nr, CurrentQuantity = menge });
        if (fa != null) _fa![id] = fa;
    }

    private StockMovementsController Ctrl(bool mitFaSetup = true)
    {
        _stock.Setup(r => r.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ReturnsAsync(() => _bestand);
        if (mitFaSetup)
            _stock.Setup(r => r.GetProductionOrdersByArticleAtLocationAsync(Platz)).ReturnsAsync(() => _fa!);
        _stock.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Callback<StockMovement>(_gebucht.Add).ReturnsAsync((StockMovement m) => m);
        _locs.Setup(r => r.GetByIdAsync(Platz)).ReturnsAsync(() => new StorageLocation
            { Id = Platz, Code = "L", BarcodeValue = "L", IsPickingTransport = _wagen, CreatedBy = "t", CreatedByWindows = "t" });
        _locs.Setup(r => r.GetActiveOrderedExcludingPickingTransportAsync()).ReturnsAsync(new List<StorageLocation>());
        _po.Setup(r => r.FindForBookingAsync(It.IsAny<string?>())).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Liste, null));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\t");
        user.Setup(u => u.GetDisplayName()).Returns("T");
        var c = new StockMovementsController(_stock.Object, Mock.Of<IArticleRepository>(), _locs.Object, Mock.Of<IUserRepository>(),
            user.Object, Mock.Of<IAppSettingRepository>(), Mock.Of<IPartRequisitionRepository>(), _po.Object, _svc.Object, Mock.Of<IProductionWorkplaceRepository>());
        var http = new DefaultHttpContext();
        c.ControllerContext = new ControllerContext { HttpContext = http };
        c.TempData = _temp = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return c;
    }

    private static Dictionary<int, string> Stueck(params (int Id, string Menge)[] z) => z.ToDictionary(x => x.Id, x => x.Menge);

    private const string Geaendert = "Bestand oder FA-Zuordnung auf L hat sich geändert – bitte Auswahl erneut prüfen. Nichts ausgebucht.";

    private void ErwarteWarnungOhneBuchung(IActionResult r, string warnung, string eingabe)
    {
        _gebucht.Should().BeEmpty();
        _temp["WarningMessage"].Should().Be(warnung);
        var red = r.Should().BeOfType<RedirectToActionResult>().Subject;
        red.RouteValues!["storageLocationId"].Should().Be(Platz);
        red.RouteValues!["productionOrder"].Should().Be(eingabe);
    }

    [Fact]
    public async Task Filter_BuchtNurTreffendeZeile_MitVerknuepfung_AK4()
    {
        var c = Ctrl();
        _po.Setup(r => r.FindForBookingAsync("1234567")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Gefunden,
            new ProductionOrder { Id = 42, OrderNumber = "1234567", SubOrderNumber = "1234567", CreatedBy = "t", CreatedByWindows = "t" }));

        var r = await c.OutboundAllConfirm(Platz, "1234567", treffendGezeigt: [10], stueckGezeigt: Stueck((10, "4")));

        _gebucht.Should().ContainSingle().Which.Should().Match<StockMovement>(m =>
            m.ArticleId == 10 && m.Quantity == 4m && m.ProductionOrder == "1234567" && m.ProductionOrderId == 42 && m.MovementType == MovementType.Ausbuchung);
        _temp["SuccessMessage"].Should().Be("1 Artikel zu FA 1234567 ausgebucht. 3 Artikel bleibt am Platz: B, C, N.");
        r.Should().BeOfType<RedirectToActionResult>().Which.RouteValues!["storageLocationId"].Should().Be(Platz);
    }

    [Fact]
    public async Task Exakt_TeilstringTrifftNicht_AK5()
    {
        var r = await Ctrl().OutboundAllConfirm(Platz, "123");
        ErwarteWarnungOhneBuchung(r, "Kein Artikel auf L gehört zu FA 123 – nichts ausgebucht.", "123");
    }

    [Fact]
    public async Task ListeImFeld_OhneVerknuepfung_AK6()
    {
        await Ctrl().OutboundAllConfirm(Platz, "111; 1234567", treffendGezeigt: [10, 1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
            stueckGezeigt: Stueck((10, "4"), (1, "2"), (3, "3")));
        _gebucht.Select(m => m.ArticleId).Should().BeEquivalentTo([10, 1, 3]);
        _gebucht.Should().OnlyContain(m => m.ProductionOrderId == null && m.ProductionOrder == "111; 1234567");
    }

    [Fact]
    public async Task KeinTreffer_Warnung_AK7()
    {
        var r = await Ctrl().OutboundAllConfirm(Platz, "999", treffendGezeigt: [], stueckGezeigt: new());
        ErwarteWarnungOhneBuchung(r, "Kein Artikel auf L gehört zu FA 999 – nichts ausgebucht.", "999");
    }

    [Fact]
    public async Task FeldLeer_WieBisher_AK8()
    {
        var c = Ctrl();
        var r = await c.OutboundAllConfirm(Platz, "  ");
        _gebucht.Should().HaveCount(4);
        _temp["SuccessMessage"].Should().Be("4 Artikel erfolgreich ausgebucht.");
        r.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().BeNull();
        _stock.Verify(s => s.GetProductionOrdersByArticleAtLocationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GemischtJa_BuchtVollenBestand_AK11()
    {
        await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        _gebucht.Select(m => (m.ArticleId, m.Quantity)).Should().BeEquivalentTo([(1, 2m), (3, 3m)]);
    }

    [Fact]
    public async Task GemischtNein_CBleibt_AK12()
    {
        await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        _gebucht.Should().ContainSingle().Which.ArticleId.Should().Be(1);
        _temp["SuccessMessage"].Should().Be("1 Artikel zu FA 111 ausgebucht. 3 Artikel bleibt am Platz: A, B, C.");
    }

    [Fact]
    public async Task GemischtNein_EinzigeZeile_ManipulierterPost_AK12()
    {
        // Filter 222: nur C trifft, gemischt (111 fremd).
        var r = await Ctrl().OutboundAllConfirm(Platz, "222", treffendGezeigt: [], gemischtGezeigt: [3], stueckGezeigt: Stueck((3, "3")));
        ErwarteWarnungOhneBuchung(r, "Nichts ausgebucht – alle gemischten Zeilen mit „Nicht ausbuchen“ beantwortet.", "222");
    }

    [Fact]
    public async Task Serverpruefung_a_OhneAuswahl()
    {
        var r = await Ctrl().OutboundAllConfirm(Platz, "111");
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Fact]
    public async Task Serverpruefung_b_AusbuchenAusserhalbGemischt()
    {
        var r = await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3, 10],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Fact]
    public async Task Serverpruefung_c_NeuTreffendeZeile()
    {
        Zeile(7, "R", 1m, Zugang("111"));
        var r = await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Theory]
    [InlineData(true)]   // fremder FA 444 an N (G1)
    [InlineData(false)]  // Zugang ohne FA an N (G2)
    public async Task Serverpruefung_d_TreffendeZeileWirdGemischt(bool fremd)
    {
        _fa![1] = fremd ? Zugang("111", "444") : new FaAmPaar(["111"], new(["111"]), true);
        var r = await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Theory]
    [InlineData("2")]     // gezeigt 2, Server 3 (Einzel-Ausbuchung zwischendurch)
    [InlineData("abc")]   // nicht lesbar
    [InlineData(null)]    // fehlt
    public async Task Serverpruefung_e_Stueckzahl(string? gezeigtC)
    {
        var stueck = Stueck((1, "2"));
        if (gezeigtC != null) stueck[3] = gezeigtC;
        var r = await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3], stueckGezeigt: stueck);
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Fact]
    public async Task Serverpruefung_f_GemischteZeileWeg()
    {
        _bestand.RemoveAll(i => i.ArticleId == 3);
        var r = await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
            stueckGezeigt: Stueck((1, "2"), (3, "3")));
        ErwarteWarnungOhneBuchung(r, Geaendert, "111");
    }

    [Fact]
    public async Task Serverpruefung_g_Positiv_InvarianteDezimalschreibweise()
    {
        _bestand.Single(i => i.ArticleId == 1).CurrentQuantity = 2.5m;
        var prev = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3], gemischtAusbuchen: [3],
                stueckGezeigt: Stueck((1, "2.5"), (3, "3.000")));
        }
        finally { Thread.CurrentThread.CurrentCulture = prev; }
        _gebucht.Single(m => m.ArticleId == 1).Quantity.Should().Be(2.5m);
        _gebucht.Should().HaveCount(2);
    }

    [Fact]
    public async Task Wagen_VorbelegungUndRestBleibtAmWagen_AK16()
    {
        _wagen = true;
        _bestand.Clear(); _fa!.Clear();
        Zeile(1, "WA", 1m, Zugang("A"));
        Zeile(2, "WB", 1m, Zugang("B"));
        Zeile(3, "WX", 1m, new FaAmPaar([], new(), true)); // per „Lagerplatz umbuchen“ ohne FA

        var vm = (OutboundAllViewModel)((ViewResult)await Ctrl().OutboundAll(Platz)).Model!;
        vm.ProductionOrder.Should().Be("A; B");

        await Ctrl().OutboundAllConfirm(Platz, "A; B", treffendGezeigt: [1, 2], stueckGezeigt: Stueck((1, "1"), (2, "1")));
        _gebucht.Select(m => m.ArticleId).Should().BeEquivalentTo([1, 2]);
        _temp["SuccessMessage"].Should().Be("2 Artikel zu FA A; B ausgebucht. 1 Artikel bleibt am Wagen: WX.");
    }

    [Fact]
    public async Task Get_UebergebeneEingabeBleibtStehen()
    {
        _wagen = true;
        var vm = (OutboundAllViewModel)((ViewResult)await Ctrl().OutboundAll(Platz, "999")).Model!;
        vm.ProductionOrder.Should().Be("999");
        vm.FaJeArtikel.Should().ContainKey(3);
    }

    [Fact]
    public async Task Ideal_HauptFaDecktSubFa_AK18()
    {
        _bestand.Clear(); _fa!.Clear();
        Zeile(11, "E", 1m, Zugang("S1"));
        Zeile(12, "F", 1m, Zugang("S2"));
        Zeile(13, "G", 1m, Zugang("H"));
        Zeile(14, "K", 1m, Zugang("S1", "X"));
        _svc.Setup(s => s.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync("true");
        var c = Ctrl();
        _po.Setup(r => r.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "H", SubOrderNumber = "H", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, OrderNumber = "H", SubOrderNumber = "S1", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, OrderNumber = "H", SubOrderNumber = "S2", CreatedBy = "t", CreatedByWindows = "t" },
        });
        _po.Setup(r => r.FindForBookingAsync("H")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Gefunden,
            new ProductionOrder { Id = 1, OrderNumber = "H", SubOrderNumber = "H", CreatedBy = "t", CreatedByWindows = "t" }));

        var vm = (OutboundAllViewModel)((ViewResult)await c.OutboundAll(Platz)).Model!;
        vm.HauptJeSub.Should().BeEquivalentTo(new Dictionary<string, string> { ["S1"] = "H", ["S2"] = "H" });

        await c.OutboundAllConfirm(Platz, "H", treffendGezeigt: [11, 12, 13], gemischtGezeigt: [14],
            stueckGezeigt: Stueck((11, "1"), (12, "1"), (13, "1"), (14, "1")));
        _gebucht.Select(m => m.ArticleId).Should().BeEquivalentTo([11, 12, 13]);
        _gebucht.Should().OnlyContain(m => m.ProductionOrder == "H" && m.ProductionOrderId == 1);
    }

    [Fact]
    public async Task Ake_HauptFaTrifftSubNicht_KeinHierarchieRead_AK20()
    {
        _bestand.Clear(); _fa!.Clear();
        Zeile(11, "E", 1m, Zugang("S1"));
        var r = await Ctrl().OutboundAllConfirm(Platz, "H", treffendGezeigt: [], stueckGezeigt: new());
        ErwarteWarnungOhneBuchung(r, "Kein Artikel auf L gehört zu FA H – nichts ausgebucht.", "H");
        _po.Verify(p => p.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact]
    public async Task BestandOhneFa_InFaZeile_Gemischt_AK24()
    {
        Zeile(20, "Y", 95m, new FaAmPaar(["111"], new(), true));
        var c = Ctrl();
        // Als treffend gezeigt → Abweichung (Y ist gemischt).
        ErwarteWarnungOhneBuchung(await c.OutboundAllConfirm(Platz, "111", treffendGezeigt: [1, 20], gemischtGezeigt: [3],
            stueckGezeigt: Stueck((1, "2"), (3, "3"), (20, "95"))), Geaendert, "111");
        // Richtig gezeigt, „Ausbuchen“ → 95 gebucht.
        await Ctrl().OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], gemischtGezeigt: [3, 20], gemischtAusbuchen: [20],
            stueckGezeigt: Stueck((1, "2"), (3, "3"), (20, "95")));
        _gebucht.Select(m => (m.ArticleId, m.Quantity)).Should().BeEquivalentTo([(1, 2m), (20, 95m)]);
    }

    [Fact]
    public async Task RepositoryMockOhneSetup_KeinAbsturz_S7()
    {
        var r = await Ctrl(mitFaSetup: false).OutboundAllConfirm(Platz, "111", treffendGezeigt: [1], stueckGezeigt: Stueck((1, "2")));
        ErwarteWarnungOhneBuchung(r, "Kein Artikel auf L gehört zu FA 111 – nichts ausgebucht.", "111");
    }
}
