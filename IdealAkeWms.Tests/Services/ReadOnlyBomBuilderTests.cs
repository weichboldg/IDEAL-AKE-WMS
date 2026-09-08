using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
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
    private static (ApplicationDbContext ctx, ReadOnlyBomBuilder builder) Build(List<BomItem> bomItems,
        string dataSource = "SAGE", bool mengeIstAuftragsmenge = false)
    {
        var ctx = TestDbContextFactory.Create();
        var prodRepo = new ProductionOrderRepository(ctx);
        var stockRepo = new StockMovementRepository(ctx);
        var articleAttrRepo = new ArticleAttributeRepository(ctx);
        var userRepo = new UserRepository(ctx);

        var bomMock = new Mock<IBomRepository>();
        bomMock.Setup(b => b.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()))
            .ReturnsAsync(new BomQueryResult(bomItems, dataSource, mengeIstAuftragsmenge));

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

    /// <summary>
    /// BOM-Bridge: die FA-Hierarchie liefert die Sollmenge bereits als Auftragsgesamtmenge
    /// (<c>MengeIstAuftragsmenge</c>) — der Builder darf sie NICHT noch einmal mit der
    /// FA-Stueckzahl multiplizieren. Die hierarchischen Zusatzfelder landen im ViewModel.
    /// </summary>
    [Fact]
    public async Task BuildAsync_Hierarchical_DoesNotMultiplyQuantity_AndFlagsHierarchical()
    {
        var bom = new List<BomItem>
        {
            new FaHierarchyBomItem
            {
                Artikelnummer = "ART-1", Position = "10", Ressourcenummer = "RES-1", Bezeichnung1 = "Teil 1",
                Menge = 5m, Kommissionieren = "PG", Hauptlagerplatz = "A-01", Ebene = 1, VaterSubFa = "1043421",
                SagePosition = "10", IsBaugruppe = true
            },
        };
        var (ctx, builder) = Build(bom, BomDataSources.FaHierarchie, mengeIstAuftragsmenge: true);
        var o = TestDataHelper.CreateOrderWithStatuses(ctx, "FA-HIER", qty: 4m, articleNumber: "ART-1");

        var vm = await builder.BuildAsync(o.Order.Id, null, null);

        vm.Should().NotBeNull();
        vm!.Hierarchical.Should().BeTrue();
        vm.FullStructure.Should().BeFalse();          // SubOrderNumber != OrderNumber -> DirectChildren
        vm.Items.Should().ContainSingle();
        vm.Items.Single().Menge.Should().Be(5m);      // keine × Quantity
        vm.Items.Single().Kommissionieren.Should().Be("PG");
        vm.Items.Single().Hauptlagerplatz.Should().Be("A-01");
        vm.Items.Single().Ebene.Should().Be(1);
        vm.Items.Single().VaterSubFa.Should().Be("1043421");
        vm.Items.Single().SagePosition.Should().Be("10");
        vm.Items.Single().IsBaugruppe.Should().BeTrue();
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
