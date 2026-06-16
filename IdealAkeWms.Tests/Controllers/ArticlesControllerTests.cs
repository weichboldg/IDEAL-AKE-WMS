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

public class ArticlesControllerTests
{
    private static (ArticlesController ctrl,
                    Mock<IArticleRepository> article,
                    Mock<IStockMovementRepository> stock,
                    Mock<IArticleAttributeRepository> attr,
                    Mock<IArticleCategoryRepository> category,
                    Mock<IBomCacheRepository> bom,
                    Mock<IProductionOrderRepository> orders) Build()
    {
        var article = new Mock<IArticleRepository>();
        var stock = new Mock<IStockMovementRepository>();
        var user = new Mock<ICurrentUserService>();
        var attr = new Mock<IArticleAttributeRepository>();
        var category = new Mock<IArticleCategoryRepository>();
        var bom = new Mock<IBomCacheRepository>();
        var orders = new Mock<IProductionOrderRepository>();

        attr.Setup(a => a.GetActiveDefinitionsOrderedAsync())
            .ReturnsAsync(new List<ArticleAttributeDefinition>());
        attr.Setup(a => a.GetValuesByArticleIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<ArticleAttributeValue>());

        var ctrl = new ArticlesController(
            article.Object, stock.Object, user.Object, attr.Object,
            category.Object, bom.Object, orders.Object);
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
        bool isDone = false, bool isDonePicking = false) => new ProductionOrder
    {
        Id = id,
        OrderNumber = $"FA{id}",
        ArticleNumber = articleNumber,
        Quantity = quantity,
        IsDone = isDone,
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
}
