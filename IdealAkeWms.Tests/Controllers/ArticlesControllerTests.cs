using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class ArticlesControllerTests
{
    private static (ArticlesController ctrl,
                    Mock<IArticleRepository> article,
                    Mock<IStockMovementRepository> stock,
                    Mock<IArticleAttributeRepository> attr,
                    Mock<IArticleCategoryRepository> category,
                    Mock<IBomCacheRepository> bom,
                    Mock<IProductionOrderRepository> orders) Build(bool hierarchical = false)
    {
        var article = new Mock<IArticleRepository>();
        var stock = new Mock<IStockMovementRepository>();
        var user = new Mock<ICurrentUserService>();
        var attr = new Mock<IArticleAttributeRepository>();
        var category = new Mock<IArticleCategoryRepository>();
        var bom = new Mock<IBomCacheRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var storageLocations = new Mock<IStorageLocationRepository>();
        storageLocations.Setup(s => s.GetActiveOrderedExcludingPickingTransportAsync())
            .ReturnsAsync(new List<StorageLocation>());

        attr.Setup(a => a.GetActiveDefinitionsOrderedAsync())
            .ReturnsAsync(new List<ArticleAttributeDefinition>());
        attr.Setup(a => a.GetValuesByArticleIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ArticleAttributeValue>());

        // Master ProduktionsauftragHierarchisch: null -> Flachmodus (AKE), "true" -> hierarchisch (IDEAL, Z4).
        var serviceSetting = new Mock<IServiceSettingRepository>();
        serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>()))
            .ReturnsAsync(hierarchical ? "true" : (string?)null);

        var ctrl = new ArticlesController(
            article.Object, stock.Object, user.Object, attr.Object,
            category.Object, bom.Object, orders.Object, storageLocations.Object,
            serviceSetting.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (ctrl, article, stock, attr, category, bom, orders);
    }

    private static StockOverviewItem Stock(decimal qty) => new StockOverviewItem
    {
        StorageLocationCode = "L1",
        CurrentQuantity = qty
    };

    private static ProductionOrder Order(int id, string articleNumber, decimal quantity,
        bool isDone = false, bool isDonePicking = false, bool isCancelled = false) => new ProductionOrder
    {
        Id = id,
        OrderNumber = $"FA{id}",
        ArticleNumber = articleNumber,
        Quantity = quantity,
        IsDone = isDone,
        IsCancelled = isCancelled,
        PickingStatus = new ProductionOrderPickingStatus { IsDonePicking = isDonePicking }
    };

    [Fact]
    public async Task Info_ExcludesPickingDoneOrders_AndComputesAvailableStock()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build();

        article.Setup(a => a.GetByArticleNumberAsync("COMP"))
            .ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null))
            .ReturnsAsync(new List<StockOverviewItem> { Stock(100m) });

        // Two devices contain COMP
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP"))
            .ReturnsAsync(new List<string> { "DEV1", "DEV2" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP"))
            .ReturnsAsync(new Dictionary<string, decimal> { ["DEV1"] = 2m, ["DEV2"] = 3m });

        // FA1 open (DEV1, qty 4); FA2 komm-done (DEV2, qty 5) -> must be excluded
        orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<ProductionOrder>
            {
                Order(1, "DEV1", 4m, isDonePicking: false),
                Order(2, "DEV2", 5m, isDonePicking: true)
            });

        var result = await ctrl.Info("COMP");

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<ArticleInfoViewModel>().Subject;

        // Only the open FA is listed
        vm.UsedInOrders.Should().ContainSingle(o => o.OrderNumber == "FA1");

        // Planned consumption = Menge(DEV1)=2 x FA1.Quantity=4 = 8 (FA2 excluded)
        vm.PlannedConsumption.Should().Be(8m);
        vm.TotalStock.Should().Be(100m);
        vm.AvailableStock.Should().Be(92m);
    }

    [Fact]
    public async Task Info_Hierarchical_CountsHauptFaAndSubFa_ForZ4()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build(hierarchical: true);

        article.Setup(a => a.GetByArticleNumberAsync("COMP"))
            .ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null))
            .ReturnsAsync(new List<StockOverviewItem>());
        // Hierarchisch liefert der Reverse-Lookup die SubOrderNumbers der Eltern-Sub-FAs,
        // nicht Geraete-Artikelnummern (BOM-Bridge).
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP"))
            .ReturnsAsync(new List<string> { "H1-1", "H1-2", "H2-1" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP"))
            .ReturnsAsync(new Dictionary<string, decimal> { ["H1-1"] = 1m, ["H1-2"] = 1m, ["H2-1"] = 1m });

        // Zwei Sub-FAs unter HauptFA H1, eine unter H2 — alle offen.
        ProductionOrder Sub(int id, string haupt, string sub) => new()
        {
            Id = id, OrderNumber = haupt, SubOrderNumber = sub, ArticleNumber = "DEV1", Quantity = 1m,
            PickingStatus = new ProductionOrderPickingStatus { IsDonePicking = false }
        };
        orders.Setup(o => o.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new List<ProductionOrder> { Sub(1, "H1", "H1-1"), Sub(2, "H1", "H1-2"), Sub(3, "H2", "H2-1") });

        var vm = (await ctrl.Info("COMP")).Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<ArticleInfoViewModel>().Subject;

        vm.Hierarchical.Should().BeTrue();
        vm.UsedInOrders.Should().HaveCount(3);       // Sub-FAs = Zeilen
        vm.UsedInHauptFaCount.Should().Be(2);        // Aufträge = distinkte HauptFA (H1, H2)
        vm.UsedInOrders.Should().Contain(o => o.SubOrderNumber == "H1-2");
    }

    [Fact]
    public async Task Info_Hierarchical_ResolvesParentSubFas_AndSumsSollmengeWithoutQuantity()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build(hierarchical: true);
        article.Setup(a => a.GetByArticleNumberAsync("COMP")).ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null)).ReturnsAsync(new List<StockOverviewItem>());
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP")).ReturnsAsync(new List<string> { "1043421", "1043422" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP")).ReturnsAsync(new Dictionary<string, decimal> { ["1043421"] = 5m, ["1043422"] = 2m });
        orders.Setup(o => o.GetBySubOrderNumbersAsync(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "1035235", SubOrderNumber = "1043421", ArticleNumber = "A1", Quantity = 4m, PickingStatus = new ProductionOrderPickingStatus() },
            new() { Id = 2, OrderNumber = "1035235", SubOrderNumber = "1043422", ArticleNumber = "A2", Quantity = 4m, PickingStatus = new ProductionOrderPickingStatus() },
        });

        var vm = (await ctrl.Info("COMP")).Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<ArticleInfoViewModel>().Subject;

        vm.UsedInOrders.Should().HaveCount(2);
        vm.UsedInHauptFaCount.Should().Be(1);
        vm.PlannedConsumption.Should().Be(7m);          // 5 + 2, KEINE × Quantity
        orders.Verify(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Info_ExcludesSageDoneOrders()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build();

        article.Setup(a => a.GetByArticleNumberAsync("COMP"))
            .ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null))
            .ReturnsAsync(new List<StockOverviewItem> { Stock(50m) });
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP"))
            .ReturnsAsync(new List<string> { "DEV1" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP"))
            .ReturnsAsync(new Dictionary<string, decimal> { ["DEV1"] = 2m });
        orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<ProductionOrder>
            {
                Order(1, "DEV1", 4m, isDone: true)
            });

        var result = await ctrl.Info("COMP");

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<ArticleInfoViewModel>().Subject;

        vm.UsedInOrders.Should().BeEmpty();
        vm.PlannedConsumption.Should().Be(0m);
        vm.AvailableStock.Should().Be(50m);
    }

    [Fact]
    public async Task Info_ExcludesCancelledOrders()
    {
        var (ctrl, article, stock, _, _, bom, orders) = Build();

        article.Setup(a => a.GetByArticleNumberAsync("COMP"))
            .ReturnsAsync(new Article { Id = 1, ArticleNumber = "COMP", Unit = "Stk" });
        stock.Setup(s => s.GetCurrentStockAsync("COMP", null, null, null))
            .ReturnsAsync(new List<StockOverviewItem> { Stock(100m) });
        bom.Setup(b => b.GetDeviceArticleNumbersByComponentAsync("COMP"))
            .ReturnsAsync(new List<string> { "DEV1", "DEV2" });
        bom.Setup(b => b.GetComponentMengePerDeviceAsync("COMP"))
            .ReturnsAsync(new Dictionary<string, decimal> { ["DEV1"] = 2m, ["DEV2"] = 3m });

        // FA1 open (DEV1, qty 4); FA2 cancelled (DEV2, qty 5) -> must be excluded from list AND plannedConsumption
        orders.Setup(o => o.GetByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<ProductionOrder>
            {
                Order(1, "DEV1", 4m, isCancelled: false),
                Order(2, "DEV2", 5m, isCancelled: true)
            });

        var result = await ctrl.Info("COMP");

        var vm = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<ArticleInfoViewModel>().Subject;

        vm.UsedInOrders.Should().ContainSingle(o => o.OrderNumber == "FA1");
        // Planned consumption = Menge(DEV1)=2 x FA1.Quantity=4 = 8 (FA2 cancelled -> excluded)
        vm.PlannedConsumption.Should().Be(8m);
    }

    [Fact]
    public async Task EditPost_SageControlledPrimary_DoesNotOverwriteFk()
    {
        var (ctrl, article, _, _, category, _, _) = Build();

        // Existing article: Sage-controlled Hauptlagerplatz (SagePrimaryStorageLocation gesetzt), FK = 5.
        var existing = new Article
        {
            Id = 42,
            ArticleNumber = "A",
            PrimaryStorageLocationId = 5,
            SagePrimaryStorageLocation = "SAGE-L1"
        };
        article.Setup(a => a.GetByIdAsync(42)).ReturnsAsync(existing);
        category.Setup(c => c.GetAllOrderedAsync()).ReturnsAsync(new List<ArticleCategory>());
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());

        // Tampered form: attacker versucht FK auf 999 zu setzen.
        var vm = new ArticleEditViewModel
        {
            Article = new Article { Id = 42, ArticleNumber = "A", PrimaryStorageLocationId = 999 }
        };

        var result = await ctrl.Edit(42, vm);

        result.Should().BeOfType<RedirectToActionResult>();
        // Guard: Sage-Wert bleibt, eingehende 999 ignoriert.
        existing.PrimaryStorageLocationId.Should().Be(5);
        article.Verify(a => a.UpdateAsync(It.Is<Article>(x => x.PrimaryStorageLocationId == 5)), Times.Once);
    }

    [Fact]
    public async Task EditPost_AppControlledPrimary_UpdatesFk()
    {
        var (ctrl, article, _, _, category, _, _) = Build();

        // Existing article: kein Sage-Wert -> app-editierbar.
        var existing = new Article
        {
            Id = 42,
            ArticleNumber = "A",
            PrimaryStorageLocationId = 5,
            SagePrimaryStorageLocation = null
        };
        article.Setup(a => a.GetByIdAsync(42)).ReturnsAsync(existing);
        category.Setup(c => c.GetAllOrderedAsync()).ReturnsAsync(new List<ArticleCategory>());
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());

        var vm = new ArticleEditViewModel
        {
            Article = new Article { Id = 42, ArticleNumber = "A", PrimaryStorageLocationId = 7 }
        };

        var result = await ctrl.Edit(42, vm);

        result.Should().BeOfType<RedirectToActionResult>();
        existing.PrimaryStorageLocationId.Should().Be(7);
        article.Verify(a => a.UpdateAsync(It.Is<Article>(x => x.PrimaryStorageLocationId == 7)), Times.Once);
    }
}
