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

    private StockMovementsController Ctrl()
    {
        _stock.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Callback<StockMovement>(_gebucht.Add).ReturnsAsync((StockMovement m) => m);
        _stock.Setup(r => r.GetCurrentStockAtLocationAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(100m);
        _stock.Setup(r => r.GetCurrentStockAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .ReturnsAsync(new List<StockOverviewItem> { new() { ArticleId = 9, CurrentQuantity = 3m } });
        _po.Setup(r => r.FindForBookingAsync("1065141")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Gefunden, Po(42, "1065141", "1065141")));
        _po.Setup(r => r.FindForBookingAsync("A; B")).ReturnsAsync(new FaAufloesung(FaAufloesungStatus.Liste, null));
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetWindowsUserName()).Returns("AKE\\t");
        user.Setup(u => u.GetDisplayName()).Returns("T");
        var settings = new Mock<IAppSettingRepository>();
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
}
