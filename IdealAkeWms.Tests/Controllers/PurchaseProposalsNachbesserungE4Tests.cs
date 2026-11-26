using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>v1.99.30 Spec 2026-11-16-bestellvorschlaege-nachbesserung E4: Liste auf dem Bedarfs-Service (AK 7, 8, 9, 10),
/// Matchcode-Spalte und Link zur Artikelinfo (AK 20a–20c).</summary>
public class PurchaseProposalsNachbesserungE4Tests
{
    private static Article Comp() => new()
    {
        Id = 1, ArticleNumber = "COMP", Matchcode = "SCHRAUBE M8", Description = "Schraube", ReorderLevel = 10m, Unit = "Stk",
        SageDetail = new ArticleSageDetail { Basismengeneinheit = "Stk", Lagermengeneinheit = "Stk" },
    };

    private static Dictionary<string, List<StockLocationInfo>> Bestand(decimal m) =>
        new() { ["COMP"] = new List<StockLocationInfo> { new() { Code = "L1", Quantity = m } } };

    private static PurchaseProposalListViewModel Vm(IActionResult r) => (PurchaseProposalListViewModel)((ViewResult)r).Model!;

    /// <summary>Gemeinsame Mocks fuer Liste und Artikelinfo: flach DEV1 (Menge 3 je Geraet) bzw. hierarchisch Sub-FAs mit Sollmenge.</summary>
    private static (Mock<IBomCacheRepository> Bom, Mock<IProductionOrderRepository> Orders, Mock<IServiceSettingRepository> Ss) FaMocks(bool hierarchisch)
    {
        var bom = new Mock<IBomCacheRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var ss = new Mock<IServiceSettingRepository>();
        ss.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync(hierarchisch ? "true" : null);
        if (hierarchisch)
        {
            bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>> { ["COMP"] = new() { ["S1"] = 12m, ["S2"] = 5m } });
            orders.Setup(o => o.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<ProductionOrder>
            {
                new() { Id = 1, OrderNumber = "H1", SubOrderNumber = "S1", Quantity = 2m, PickingStatus = new() },
                new() { Id = 2, OrderNumber = "H1", SubOrderNumber = "S2", Quantity = 2m, PickingStatus = new() },
            });
        }
        else
        {
            bom.Setup(b => b.GetComponentMengePerDeviceAsync(It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>> { ["COMP"] = new() { ["DEV1"] = 3m } });
            orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>())).ReturnsAsync(new List<ProductionOrder>
            {
                new() { Id = 1, OrderNumber = "FA1", ArticleNumber = "DEV1", Quantity = 4m, PickingStatus = new() },
                new() { Id = 2, OrderNumber = "FA2", ArticleNumber = "DEV1", Quantity = 1m, PickingStatus = new() },
            });
        }
        return (bom, orders, ss);
    }

    private static ArticlesController Artikelinfo(Mock<IBomCacheRepository> bom, Mock<IProductionOrderRepository> orders, Mock<IServiceSettingRepository> ss,
        List<OffenerBedarf> lager, List<OffenerBedarf> meldungen, Dictionary<(int, string), decimal> ausgebucht)
    {
        var article = new Mock<IArticleRepository>();
        article.Setup(a => a.GetByArticleNumberAsync("COMP")).ReturnsAsync(Comp());
        article.Setup(a => a.GetSuppliersAsync(It.IsAny<int>())).ReturnsAsync(new List<ArticleSupplierRow>());
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.GetCurrentStockAsync(It.IsAny<string>(), null, null, null)).ReturnsAsync(new List<StockOverviewItem>());
        stock.Setup(s => s.GetAusbuchungenJeFaAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(ausgebucht);
        var attr = new Mock<IArticleAttributeRepository>();
        attr.Setup(a => a.GetActiveDefinitionsOrderedAsync()).ReturnsAsync(new List<ArticleAttributeDefinition>());
        attr.Setup(a => a.GetValuesByArticleIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ArticleAttributeValue>());
        var repo = new Mock<IPurchaseProposalRepository>();
        repo.Setup(r => r.GetOffeneLagerbestellungenAsync()).ReturnsAsync(lager);
        repo.Setup(r => r.GetOffeneBedarfsmeldungenAsync()).ReturnsAsync(meldungen);
        var planned = new PlannedConsumptionService(bom.Object, orders.Object, stock.Object, repo.Object, Mock.Of<IAppSettingRepository>(), ss.Object);
        return new ArticlesController(article.Object, stock.Object, Mock.Of<ICurrentUserService>(), attr.Object, Mock.Of<IArticleCategoryRepository>(),
            planned, Mock.Of<IStorageLocationRepository>(), Mock.Of<ICreditorRepository>(), Mock.Of<IAppSettingRepository>(), Mock.Of<ISagePurchaseOrderLineRepository>(), Mock.Of<IPurchaseOrderReceiptRepository>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ListeUndArtikelinfo_DieselbenBedarfe_FlachUndHierarchisch_AK7_AK8(bool hierarchisch)
    {
        // FA1 teilweise ausgebucht (AK 10), Bedarfsmeldung zu FA2 groesser als dessen Rest (AK 9), Lagerbestellung kleiner als der FA-Teil.
        var lager = new List<OffenerBedarf> { new("COMP", "Stk", 4m) };
        var meldungen = new List<OffenerBedarf> { new("COMP", "Stk", 9m, ProductionOrderId: 2) };
        var ausgebucht = new Dictionary<(int, string), decimal> { [(1, "COMP")] = 5m };

        var (bom, orders, ss) = FaMocks(hierarchisch);
        var liste = Vm(await PurchaseProposalsControllerTests.Build(new() { Comp() }, Bestand(100m), lager: lager, meldungen: meldungen,
            bom: bom, orders: orders, serviceSettings: ss, ausgebucht: ausgebucht).ctrl.Index(umfang: "alle")).Items.Single();
        var (bom2, orders2, ss2) = FaMocks(hierarchisch);
        var info = (ArticleInfoViewModel)((ViewResult)await Artikelinfo(bom2, orders2, ss2, lager, meldungen, ausgebucht).Info("COMP")).Model!;

        // flach: FA1 3×4=12 −5 = 7, FA2 3×1=3 → max(3, BM 9) = 9; hierarchisch: S1 12−5 = 7, S2 5 → max(5, 9) = 9. Angerechnet max(16, LB 4) = 16.
        var faRest = hierarchisch ? 12m : 10m;
        liste.FaBedarf.Should().Be(faRest).And.Be(info.Bedarf.FaRest);
        liste.Bedarf.Bedarfsmeldungen.Should().Be(9m).And.Be(info.Bedarf.Bedarfsmeldungen);
        liste.Bedarf.Lagerbestellungen.Should().Be(4m).And.Be(info.Bedarf.Lagerbestellungen);
        liste.Bedarf.Angerechnet.Should().Be(16m).And.Be(info.Bedarf.Angerechnet);
        liste.Ergebnis.Verfuegbar.Should().Be(100m - 16m);   // AK 8: verfuegbar um die angerechneten Bedarfe kleiner
        info.UsedInOrders.Single(o => o.Id == 1).Rest.Should().Be(7m);
    }

    [Fact]
    public async Task Matchcode_SpalteGefiltertUndArtikelinfoLink_NurMitStammdatenLesen_AK20a_AK20c()
    {
        var (ctrl, _) = PurchaseProposalsControllerTests.Build(new() { Comp() }, Bestand(0m));
        ctrl.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_matchcode=M8");
        var vm = Vm(await ctrl.Index(umfang: "alle"));
        vm.Items.Should().ContainSingle();
        vm.ArtikelinfoLink.Should().BeTrue();

        var (ctrl2, _) = PurchaseProposalsControllerTests.Build(new() { Comp() }, Bestand(0m));
        ctrl2.ControllerContext.HttpContext.Request.QueryString = new QueryString("?colf_matchcode=NICHTDA");
        Vm(await ctrl2.Index(umfang: "alle")).Items.Should().BeEmpty();

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.HasMasterDataReadAccessAsync()).ReturnsAsync(false);
        var (ohne, _) = PurchaseProposalsControllerTests.Build(new() { Comp() }, Bestand(0m), user: user.Object);
        Vm(await ohne.Index(umfang: "alle")).ArtikelinfoLink.Should().BeFalse();
    }

    [Fact]
    public void Spalten_MatchcodeUndFaBedarf_ImZahnradUndAlsFilterTyp()
    {
        ColumnDefinitions.PurchaseProposals.Columns.Select(c => c.Key).Should().ContainInOrder("artikel", "matchcode", "bezeichnung")
            .And.ContainInOrder("fa-bedarf", "bedarfe");
        ColumnFilterTypes.For("PurchaseProposals/Index").Should().ContainKey("fa-bedarf");
    }
}
