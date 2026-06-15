using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer den <see cref="ReadOnlyBomBuilder"/> (gemeinsame read-only Stueckliste fuer
/// FaWorklist + FaCompletion). BOM kommt aus SAGE/OSEON (Dapper) -> <c>IBomRepository</c>-Mock;
/// die uebrigen Repos laufen gegen InMemory-DbContext.
/// </summary>
public class ReadOnlyBomBuilderTests
{
    private static (ApplicationDbContext ctx, ReadOnlyBomBuilder builder) Build(List<BomItem> bomItems)
    {
        var ctx = TestDbContextFactory.Create();
        var prodRepo = new ProductionOrderRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var articleAttrRepo = new ArticleAttributeRepository(ctx);
        var userRepo = new UserRepository(ctx);

        var bomMock = new Mock<IBomRepository>();
        bomMock.Setup(b => b.GetBomItemsAsync(It.IsAny<string>()))
            .ReturnsAsync(new BomQueryResult(bomItems, "SAGE"));

        var builder = new ReadOnlyBomBuilder(prodRepo, bomMock.Object, stockRepo, articleAttrRepo, userRepo);
        return (ctx, builder);
    }

    [Fact]
    public async Task BuildAsync_UnknownOrder_ReturnsNull()
    {
        var (_, builder) = Build(new List<BomItem>());

        var vm = await builder.BuildAsync(999_999, null, null);

        vm.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_OrderWithoutArticleNumber_ReturnsNull()
    {
        var (ctx, builder) = Build(new List<BomItem>());
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-NOART", articleNumber: null);

        var vm = await builder.BuildAsync(o.Order.Id, null, null);

        vm.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_SetsReadOnly_AndDefaultBackController_AndMultipliesQuantity()
    {
        var bom = new List<BomItem>
        {
            new() { Artikelnummer = "ART-1", Position = "10", Ressourcenummer = "RES-1", Bezeichnung1 = "Teil 1", Menge = 2m },
        };
        var (ctx, builder) = Build(bom);
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-BOM", qty: 3m, articleNumber: "ART-1");

        var vm = await builder.BuildAsync(o.Order.Id, null, null);

        vm.Should().NotBeNull();
        vm!.ReadOnly.Should().BeTrue();
        vm.BackController.Should().Be("FaWorklist"); // Default; FaCompletion ueberschreibt das im Controller.
        vm.ProductionOrderId.Should().Be(o.Order.Id);
        vm.OrderNumber.Should().Be("FA-BOM");
        vm.Items.Should().ContainSingle();
        // Menge = BOM-Menge * FA-Stueckzahl (2 * 3).
        vm.Items.Single().Menge.Should().Be(6m);
    }

    [Fact]
    public async Task BuildAsync_FilterText_FiltersItems()
    {
        var bom = new List<BomItem>
        {
            new() { Artikelnummer = "ART-1", Position = "10", Ressourcenummer = "RES-1", Bezeichnung1 = "Kuehler", Menge = 1m },
            new() { Artikelnummer = "ART-1", Position = "20", Ressourcenummer = "RES-2", Bezeichnung1 = "Luefter", Menge = 1m },
        };
        var (ctx, builder) = Build(bom);
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-FILTER", qty: 1m, articleNumber: "ART-1");

        var vm = await builder.BuildAsync(o.Order.Id, "Luefter", null);

        vm.Should().NotBeNull();
        vm!.Items.Should().ContainSingle();
        vm.Items.Single().Ressourcenummer.Should().Be("RES-2");
        vm.FilterText.Should().Be("Luefter");
    }
}
