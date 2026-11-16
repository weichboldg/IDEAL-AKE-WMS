using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung, Etappe 3): EINE Bedarfsregel fuer Artikelinfo und Bestellvorschlaege.</summary>
public class PlannedConsumptionServiceTests
{
    private sealed class Fixture
    {
        public readonly Mock<IBomCacheRepository> Bom = new();
        public readonly Mock<IProductionOrderRepository> Orders = new();
        public readonly Mock<IStockMovementRepository> Stock = new();
        public readonly Mock<IPurchaseProposalRepository> Bedarfe = new();
        public readonly Mock<IAppSettingRepository> Settings = new();
        public readonly Mock<IServiceSettingRepository> ServiceSettings = new();
        public List<OffenerBedarf> Lager = new();
        public List<OffenerBedarf> Meldungen = new();
        public Dictionary<(int, string), decimal> Ausgebucht = new();

        public Fixture(bool hierarchisch = false)
        {
            ServiceSettings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync(hierarchisch ? "true" : null);
            Bedarfe.Setup(b => b.GetOffeneLagerbestellungenAsync()).ReturnsAsync(() => Lager);
            Bedarfe.Setup(b => b.GetOffeneBedarfsmeldungenAsync()).ReturnsAsync(() => Meldungen);
            Stock.Setup(s => s.GetAusbuchungenJeFaAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(() => Ausgebucht);
            Orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<ProductionOrder>());
        }

        /// <summary>Flach: COMP mit Menge je Geraet DEV1; offene FAs auf DEV1.</summary>
        public Fixture Flach(decimal mengeJeGeraet, params ProductionOrder[] fas)
        {
            Bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase)
                    { ["COMP"] = new(StringComparer.OrdinalIgnoreCase) { ["DEV1"] = mengeJeGeraet } });
            Orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(fas.ToList());
            return this;
        }

        public PlannedConsumptionService Sut() =>
            new(Bom.Object, Orders.Object, Stock.Object, Bedarfe.Object, Settings.Object, ServiceSettings.Object);
    }

    private static readonly Article Comp = new() { Id = 1, ArticleNumber = "COMP", Unit = "Stk" };

    private static ProductionOrder Fa(int id, decimal qty, bool isDone = false, bool isDonePicking = false, bool isCancelled = false) => new()
    {
        Id = id, OrderNumber = $"FA{id}", SubOrderNumber = $"FA{id}", ArticleNumber = "DEV1", Quantity = qty,
        IsDone = isDone, IsCancelled = isCancelled, ProductionDate = new DateTime(2026, 12, id),
        PickingStatus = new ProductionOrderPickingStatus { IsDonePicking = isDonePicking }
    };

    private static OffenerBedarf Bm(decimal menge, int faId, string unit = "Stk") => new("COMP", unit, menge, ProductionOrderId: faId);
    private static OffenerBedarf Lb(decimal menge, string unit = "Stk") => new("comp ", unit, menge);

    private static async Task<ArtikelBedarf> Run(Fixture f) => (await f.Sut().GetJeArtikelAsync(new[] { Comp })).Fuer("COMP");

    [Fact]
    public async Task OhneBedarfeUndAusbuchungen_GleichDerAltenFaSumme_Flach_AK7a()
    {
        // Alt (ArticlesController bis v1.99.29): Σ Menge je Geraet × FA-Stueckzahl ueber offene FAs = 2×4 = 8; erledigte/stornierte/komm.-fertige fallen raus.
        var f = new Fixture().Flach(2m, Fa(1, 4m), Fa(2, 5m, isDonePicking: true), Fa(3, 6m, isDone: true), Fa(4, 7m, isCancelled: true));
        var b = await Run(f);
        b.Fas.Should().ContainSingle(x => x.ProductionOrderId == 1);
        b.FaRest.Should().Be(8m);
        b.Angerechnet.Should().Be(8m);
    }

    [Fact]
    public async Task Hierarchisch_SollmengeOhneStueckzahl_AK7a()
    {
        var f = new Fixture(hierarchisch: true);
        f.Bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>> { ["COMP"] = new() { ["1043421"] = 5m, ["1043422"] = 2m } });
        f.Orders.Setup(o => o.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "1035235", SubOrderNumber = "1043421", Quantity = 4m, PickingStatus = new() },
            new() { Id = 2, OrderNumber = "1035235", SubOrderNumber = "1043422", Quantity = 4m, PickingStatus = new() },
        });
        var b = await Run(f);
        b.Angerechnet.Should().Be(7m);   // 5 + 2, KEINE × Quantity
        f.Orders.Verify(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 4, 10)]   // FA 10 + Lagerbestellung 4 -> 10
    [InlineData(3, 8, 8)]     // FA 3 + Lagerbestellung 8 -> 8
    public async Task Lagerbestellung_MaxMitFaTeil_AK9(decimal fa, decimal lager, decimal erwartet)
    {
        var f = new Fixture().Flach(fa, Fa(1, 1m));
        f.Lager = new() { Lb(lager) };
        var b = await Run(f);
        b.Lagerbestellungen.Should().Be(lager);
        b.Angerechnet.Should().Be(erwartet);
    }

    [Fact]
    public async Task Bedarfsmeldung_ZumSelbenFa_NurEinmal_Max_AK9()
    {
        var f = new Fixture().Flach(1m, Fa(1, 10m), Fa(2, 3m));
        f.Meldungen = new() { Bm(4m, faId: 1), Bm(5m, faId: 2) };   // FA1: max(10,4)=10; FA2: max(3,5)=5
        var b = await Run(f);
        b.Bedarfsmeldungen.Should().Be(9m);
        b.FaTeil.Should().Be(15m);
        b.Angerechnet.Should().Be(15m);
    }

    [Fact]
    public async Task Bedarfsmeldung_FaOhneStuecklistenTreffer_ZaehltVoll()
    {
        var f = new Fixture().Flach(1m, Fa(1, 10m));
        f.Meldungen = new() { Bm(6m, faId: 77) };
        var b = await Run(f);
        b.Fas.Should().Contain(x => x.ProductionOrderId == 77 && !x.AusStueckliste && x.Angerechnet == 6m);
        b.Angerechnet.Should().Be(16m);
    }

    [Fact]
    public async Task TeilweiseKommissioniert_NurRest_NieNegativ_AK10()
    {
        var f = new Fixture().Flach(10m, Fa(1, 1m), Fa(2, 1m));
        f.Ausgebucht = new() { [(1, "COMP")] = 6m, [(2, "COMP")] = 15m };   // FA1: 10−6 = 4; FA2: 10−15 -> 0
        var b = await Run(f);
        b.Fas.Single(x => x.ProductionOrderId == 1).Rest.Should().Be(4m);
        b.Fas.Single(x => x.ProductionOrderId == 2).Rest.Should().Be(0m);
        b.Angerechnet.Should().Be(4m);
    }

    [Fact]
    public async Task AbweichendeEinheit_NichtGerechnet_AberGezaehlt()
    {
        var f = new Fixture().Flach(1m, Fa(1, 2m));
        f.Lager = new() { Lb(50m, unit: "m") };
        f.Meldungen = new() { Bm(9m, faId: 1, unit: "kg") };
        var b = await Run(f);
        b.EinheitAbweichend.Should().Be(2);
        b.Angerechnet.Should().Be(2m);
    }

    [Theory]
    [InlineData("", 2)]                    // leer = keine
    [InlineData("Bedarfsmeldungen", 5)]    // nur Bedarfsmeldungen
    [InlineData(null, 8)]                  // nicht gesetzt = beide
    public async Task EinstellungEinkaufOffeneBedarfe_SteuertLagerUndMeldungen(string? wert, decimal erwartet)
    {
        var f = new Fixture().Flach(1m, Fa(1, 2m));
        f.Settings.Setup(s => s.GetValueAsync(AppSettingKeys.EinkaufOffeneBedarfe)).ReturnsAsync(wert);
        f.Lager = new() { Lb(8m) };
        f.Meldungen = new() { Bm(5m, faId: 1) };
        (await Run(f)).Angerechnet.Should().Be(erwartet);
    }

    [Fact]
    public async Task Repository_AusbuchungenJeFa_NurAusbuchung_EinbuchungMindertNicht_AK10()
    {
        using var db = TestDbContextFactory.Create();
        db.Articles.Add(new Article { Id = 9, ArticleNumber = " comp", CreatedBy = "t", CreatedByWindows = "t" });
        db.StorageLocations.Add(new StorageLocation { Id = 2, Code = "L2", CreatedBy = "t", CreatedByWindows = "t" });
        StockMovement M(int id, MovementType typ, decimal qty, int? fa) => new()
        {
            Id = id, ArticleId = 9, StorageLocationId = 2, Quantity = qty, ProductionOrderId = fa, MovementType = typ,
            Timestamp = DateTime.Now, WindowsUser = "w", CreatedBy = "t", CreatedByWindows = "t"
        };
        db.StockMovements.AddRange(M(1, MovementType.Ausbuchung, 4m, 1), M(2, MovementType.Ausbuchung, 2m, 1),
            M(3, MovementType.Einbuchung, 7m, 1), M(4, MovementType.Ausbuchung, 3m, 5), M(5, MovementType.Ausbuchung, 1m, null));
        await db.SaveChangesAsync();

        var r = await new StockMovementRepository(db).GetAusbuchungenJeFaAsync(new[] { 1, 2 });

        r.Should().BeEquivalentTo(new Dictionary<(int, string), decimal> { [(1, "COMP")] = 6m });
    }
}
