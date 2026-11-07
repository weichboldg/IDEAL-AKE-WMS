using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>Spec 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung (v1.87.0): Aufloesungsregel (AK 2) und FK in den Schreibwegen 1–5 (AK 3).</summary>
public class LagerbuchungFaVerknuepfungTests
{
    private static ProductionOrder Po(int id, string nr, string sub = "") => new()
    {
        Id = id, OrderNumber = nr, SubOrderNumber = sub, CreatedBy = "t", CreatedByWindows = "t"
    };

    private static async Task<ApplicationDbContext> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        db.ProductionOrders.AddRange(
            Po(1, "1065141", "1065141"),          // AKE: Sub = Order
            Po(2, "H-500", ""),                   // IDEAL Haupt-FA (Sub leer)
            Po(3, "H-500", "77001"));             // IDEAL Sub-FA (BelID)
        await db.SaveChangesAsync();
        return db;
    }

    [Theory]
    [InlineData("1065141", FaAufloesungStatus.Gefunden, 1)]
    [InlineData("  1065141 ", FaAufloesungStatus.Gefunden, 1)]
    [InlineData("H-500", FaAufloesungStatus.Gefunden, 2)]
    [InlineData("77001", FaAufloesungStatus.Gefunden, 3)]
    [InlineData("1065141; 1065142", FaAufloesungStatus.Liste, null)]
    [InlineData("1065141,1065142", FaAufloesungStatus.Liste, null)]
    [InlineData("9999999", FaAufloesungStatus.Unbekannt, null)]
    [InlineData("", FaAufloesungStatus.Leer, null)]
    [InlineData(null, FaAufloesungStatus.Leer, null)]
    public async Task FindForBooking_Regel_AK2(string? text, FaAufloesungStatus status, int? id)
    {
        using var db = await SeedAsync();
        var r = await new ProductionOrderRepository(db).FindForBookingAsync(text);
        (r.Status, r.Order?.Id).Should().Be((status, id));
    }

    [Fact]
    public async Task FindForBooking_ZweiHauptFaZeilenOhneSubTreffer_Mehrdeutig_AK2()
    {
        using var db = TestDbContextFactory.Create();
        // Zwei Haupt-FA-Zeilen gleicher OrderNumber ohne SubOrderNumber-Treffer (InMemory erzwingt den UNIQUE-Index nicht).
        db.ProductionOrders.AddRange(Po(4, "DOPPELT", ""), Po(7, "DOPPELT", ""));
        await db.SaveChangesAsync();
        (await new ProductionOrderRepository(db).FindForBookingAsync("DOPPELT")).Status.Should().Be(FaAufloesungStatus.Mehrdeutig);
    }

    // ---------- Schreibwege (AK 3) ----------

    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly Mock<IProductionOrderRepository> _po = new();
    private readonly List<StockMovement> _gebucht = new();
    private readonly Mock<IAppSettingRepository> _settings = new();
    private string? _pflicht;

    private StockMovementsController Ctrl()
    {
        _stock.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Callback<StockMovement>(_gebucht.Add).ReturnsAsync((StockMovement m) => m);
        _stock.Setup(r => r.GetCurrentStockAtLocationAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(100m);
        _stock.Setup(r => r.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ReturnsAsync(new List<StockOverviewItem> { new() { ArticleId = 9, CurrentQuantity = 3m } });
        _po.Setup(r => r.FindForBookingAsync("1065141")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Gefunden, Po(42, "1065141", "1065141")));
        _po.Setup(r => r.FindForBookingAsync("9999999")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Unbekannt, null));
        _po.Setup(r => r.FindForBookingAsync(null)).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Leer, null));
        _po.Setup(r => r.FindForBookingAsync("ERL")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Gefunden,
            new ProductionOrder { Id = 43, OrderNumber = "ERL", SubOrderNumber = "ERL", IsDone = true, CreatedBy = "t", CreatedByWindows = "t" }));
        _po.Setup(r => r.FindForBookingAsync("A; B")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Liste, null));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\t");
        user.Setup(u => u.GetDisplayName()).Returns("T");
        var settings = _settings;
        settings.Setup(r => r.GetValueAsync(AppSettingKeys.FaPflichtBuchungsarten)).ReturnsAsync(() => _pflicht);
        var ctrl = new StockMovementsController(_stock.Object, Mock.Of<IArticleRepository>(), Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IUserRepository>(), user.Object, settings.Object, Mock.Of<IPartRequisitionRepository>(), _po.Object);
        var http = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        ctrl.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task Schreibwege1bis5_SetzenFkUndBehaltenText_AK3()
    {
        var c = Ctrl();
        await c.Inbound(new StockMovementCreateViewModel { ArticleId = 9, Quantity = 1, StorageLocationId = 2, ProductionOrder = "1065141" }, null);
        await c.InboundBulk(new StockMovementBulkInboundViewModel
        {
            StorageLocationId = 2, ProductionOrder = "1065141",
            Lines = new() { new() { ArticleId = 9, Quantity = 1 }, new() { ArticleId = 10, Quantity = 2 } }
        });
        await c.Outbound(new StockMovementCreateViewModel { ArticleId = 9, Quantity = 1, StorageLocationId = 2, ProductionOrder = "1065141" });
        await c.Transfer(new StockTransferViewModel { ArticleId = 9, Quantity = 1, SourceStorageLocationId = 2, StorageLocationId = 3, ProductionOrder = "1065141" });
        await c.OutboundAllConfirm(2, "1065141");

        _gebucht.Should().HaveCount(6).And.OnlyContain(m => m.ProductionOrderId == 42 && m.ProductionOrder == "1065141");
    }

    [Fact]
    public async Task LagerplatzAusbuchen_WagenListe_NichtVerknuepft_Antwort5()
    {
        await Ctrl().OutboundAllConfirm(2, "A; B");
        _gebucht.Should().ContainSingle().Which.Should().Match<StockMovement>(m => m.ProductionOrderId == null && m.ProductionOrder == "A; B");
    }

    // ---------- E3: Warnung, Pflicht, Einstellung (AK 4–8) ----------

    private static StockMovementCreateViewModel Vm(string? fa) => new() { ArticleId = 9, Quantity = 1, StorageLocationId = 2, ProductionOrder = fa };

    [Fact]
    public async Task UnbekannterFa_OhnePflicht_GebuchtMitWarnung_AK4()
    {
        var c = Ctrl();
        await c.Inbound(Vm("9999999"), null);
        _gebucht.Should().ContainSingle().Which.ProductionOrderId.Should().BeNull();
        c.TempData["SuccessMessage"].Should().NotBeNull();
        ((string)c.TempData["WarningMessage"]!).Should().Contain("FA 9999999 ist im WMS unbekannt");
    }

    [Fact]
    public async Task ErledigterFa_GebuchtVerknuepftWarnungNenntStatus_AK5()
    {
        var c = Ctrl();
        await c.Outbound(Vm("ERL"));
        _gebucht.Should().ContainSingle().Which.ProductionOrderId.Should().Be(43);
        ((string)c.TempData["WarningMessage"]!).Should().Contain("erledigt");
    }

    [Fact]
    public async Task Pflicht_Warenausgang_UnbekanntUndLeerAbgewiesen_WareneingangBuchtWeiter_AK6()
    {
        _pflicht = "Warenausgang";
        var c = Ctrl();
        await c.Outbound(Vm("9999999"));
        c.ModelState[nameof(StockMovementCreateViewModel.ProductionOrder)]!.Errors.Should().ContainSingle();
        var c2 = Ctrl();
        await c2.Outbound(Vm(null));
        c2.ModelState.IsValid.Should().BeFalse();
        _gebucht.Should().BeEmpty();

        await Ctrl().Inbound(Vm("9999999"), null);
        _gebucht.Should().ContainSingle();
        await Ctrl().Outbound(Vm("1065141"));
        _gebucht.Should().HaveCount(2);
    }

    [Fact]
    public async Task Pflicht_SammelWareneingang_UnbekannterKopfFa_NichtsGebucht_AK7()
    {
        _pflicht = "Sammel-Wareneingang";
        var c = Ctrl();
        await c.InboundBulk(new StockMovementBulkInboundViewModel
        {
            StorageLocationId = 2, ProductionOrder = "9999999",
            Lines = new() { new() { ArticleId = 9, Quantity = 1 }, new() { ArticleId = 10, Quantity = 2 } }
        });
        _gebucht.Should().BeEmpty();
        c.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Einstellung_FaPflicht_MultiChoice_ManipulierterWertAbgewiesen_AK8()
    {
        var d = AppSettingDefinitions.All.Single(x => x.Key == AppSettingKeys.FaPflichtBuchungsarten);
        (d.Type, d.Anzeigename, d.LeerErlaubt).Should().Be((SettingType.MultiChoice, "FA-Pflicht bei Buchungen", true));
        d.Warnhinweis.Should().Contain("Sage");
        IdealAkeWms.Services.Settings.SettingValidator.Validate(d, "Kommissionierung").Gueltig.Should().BeFalse();
        IdealAkeWms.Services.Settings.SettingValidator.Validate(d, "Warenausgang,Umbuchung").Gueltig.Should().BeTrue();
        IdealAkeWms.Services.Settings.SettingValidator.Validate(d, "").Gueltig.Should().BeTrue();
    }

    // ---------- E3: API (AK 10) ----------

    private static async Task<StockApiController> ApiAsync(ApplicationDbContext db, bool faLeserecht, string? master = null)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.CanPickAsync()).ReturnsAsync(faLeserecht);
        var ss = new Mock<IServiceSettingRepository>();
        ss.Setup(r => r.GetValueAsync(It.IsAny<string>())).ReturnsAsync(master);
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns((UrlActionContext a) => $"/{a.Controller}");
        var api = new StockApiController(Mock.Of<IStockMovementRepository>(), new ProductionOrderRepository(db), user.Object, ss.Object) { Url = url.Object };
        return await Task.FromResult(api);
    }

    private static Dictionary<string, object?> Json(IActionResult r) =>
        ((OkObjectResult)r).Value!.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(((OkObjectResult)r).Value));

    [Fact]
    public async Task Api_FaLookup_LinkNurMitLeserecht_ZielJeMaster_AK10_AK12()
    {
        using var db = await SeedAsync();
        Json(await (await ApiAsync(db, false)).FaLookup("1065141"))["link"].Should().BeNull();
        var ake = Json(await (await ApiAsync(db, true)).FaLookup("1065141"));
        (ake["status"], ake["statusText"], ake["link"]).Should().Be(("gefunden", "offen", "/ProductionOrders"));
        var ideal = Json(await (await ApiAsync(db, true, "true")).FaLookup("77001"));
        (ideal["link"], ideal["anzeigeNummer"]).Should().Be(("/FaHierarchy", "77001 (Haupt-FA H-500)"));
        Json(await (await ApiAsync(db, true)).FaLookup("9999999"))["hinweis"].Should().Be("FA 9999999 ist im WMS unbekannt.");
    }

    [Fact]
    public void Api_KlasseBleibtHinterLagerPickingTrackingFilter_AK10()
    {
        typeof(StockApiController).GetCustomAttributes(typeof(IdealAkeWms.Filters.RequireStockOrPickingOrTrackingAccessAttribute), true)
            .Should().ContainSingle();
    }

    // ---------- E4: Bewegungshistorie (AK 11) ----------

    [Fact]
    public async Task Historie_VerknuepfteZeileMitFa_NichtVerknuepfteNurText_FilterAmText_AK11()
    {
        using var db = await SeedAsync();
        db.Articles.Add(new Article { Id = 9, ArticleNumber = "A9", CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 2, Code = "L2", CreatedBy = "t", CreatedByWindows = "t" });
        StockMovement M(int id, string? text, int? faId) => new()
        {
            Id = id, ArticleId = 9, StorageLocationId = 2, Quantity = 1, ProductionOrder = text, ProductionOrderId = faId,
            Timestamp = new DateTime(2026, 11, 8, 10, id, 0), WindowsUser = "w", CreatedBy = "t", CreatedByWindows = "t"
        };
        db.StockMovements.AddRange(M(1, "1065141", 1), M(2, "asdf", null));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var (items, total) = await new StockMovementRepository(db).GetMovementHistoryAsync(page: 1, pageSize: 25);
        total.Should().Be(2);
        items.Single(i => i.Id == 1).LinkedFa!.OrderNumber.Should().Be("1065141");
        items.Single(i => i.Id == 2).LinkedFa.Should().BeNull();
        (await new StockMovementRepository(db).GetMovementHistoryAsync(filterProductionOrder: "asdf", page: 1, pageSize: 25)).TotalCount.Should().Be(1);
    }
}
