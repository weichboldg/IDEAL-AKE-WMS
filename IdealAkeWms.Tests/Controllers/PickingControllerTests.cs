using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class PickingControllerTests
{
    /// <summary>
    /// Baut einen PickingController mit Mocks fuer alle Dependencies.
    /// LeitstandAktiv=true (sonst IndexDropdown), Zuweisung=false (GetReleasedForPickingAsync),
    /// KommissionierTage=4, keine Feiertage. Echter BusinessDayService fuer Termin-Berechnung.
    /// </summary>
    private static PickingController Setup(List<ProductionOrder> releasedOrders, string queryString,
        bool hierarchical = false)
    {
        var pickingStatus = new Mock<IProductionOrderPickingStatusRepository>();
        pickingStatus.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(releasedOrders);

        // Master ProduktionsauftragHierarchisch: null -> Flachmodus (AKE), "true" -> hierarchisch (IDEAL).
        var serviceSetting = new Mock<IServiceSettingRepository>();
        serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        if (hierarchical)
        {
            serviceSetting
                .Setup(s => s.GetValueAsync(
                    IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master))
                .ReturnsAsync("true");
        }

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LeitstandAktiv)).ReturnsAsync("true");
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.KommissionierungMitZuweisung)).ReturnsAsync("false");
        settings.Setup(s => s.GetIntValueAsync("KommissionierTage", 4)).ReturnsAsync(4);

        var holidays = new Mock<IHolidayRepository>();
        holidays.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());

        var ctrl = new PickingController(
            Mock.Of<IProductionOrderRepository>(),
            pickingStatus.Object,
            current.Object,
            settings.Object,
            holidays.Object,
            new BusinessDayService(),
            Mock.Of<IBomRepository>(),
            Mock.Of<IPickingRepository>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(),
            Mock.Of<IArticleAttributeRepository>(),
            serviceSetting.Object,
            NullLogger<PickingController>.Instance);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString(queryString);
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return ctrl;
    }

    private static ProductionOrder Order(string orderNumber, DateTime? productionDate = null) => new()
    {
        OrderNumber = orderNumber,
        ProductionDate = productionDate,
        CreatedAt = DateTime.Now,
        CreatedBy = "t",
        CreatedByWindows = "t"
    };

    // Hierarchie-Order: OrderNumber = HauptFA (Gruppenschluessel), SubOrderNumber = Sub-FA,
    // mit PickingStatus (Prio steuert die Warteschlangen-Reihenfolge).
    private static ProductionOrder OrderH(string orderNumber, string subOrderNumber, int? priority = null,
        string? parentSub = null) => new()
    {
        OrderNumber = orderNumber,
        SubOrderNumber = subOrderNumber,
        ParentSubOrderNumber = parentSub,
        CreatedAt = DateTime.Now,
        CreatedBy = "t",
        CreatedByWindows = "t",
        PickingStatus = new ProductionOrderPickingStatus
        {
            PickingPriority = priority,
            IsReleasedForPicking = true,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        },
    };

    /// <summary>
    /// Controller fuer die Bom-Action: alle Listen-/Dictionary-Rueckgaben explizit leer gemockt
    /// (Moq liefert fuer konkrete List/Dictionary sonst null), Benutzer ohne App-Id, alle Toggles aus.
    /// </summary>
    private static PickingController SetupForBom(ProductionOrder order, BomQueryResult bomResult)
    {
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var bom = new Mock<IBomRepository>();
        bom.Setup(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>())).ReturnsAsync(bomResult);

        var picking = new Mock<IPickingRepository>();
        picking.Setup(r => r.GetByProductionOrderAsync(order.Id)).ReturnsAsync(new List<PickingItem>());

        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(r => r.GetStockByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, List<StockLocationInfo>>());

        var attrs = new Mock<IArticleAttributeRepository>();
        attrs.Setup(r => r.GetCategoryNamesByArticleNumbersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, string?>());

        var locations = new Mock<IStorageLocationRepository>();
        locations.Setup(r => r.GetActiveOrderedExcludingPickingTransportAsync()).ReturnsAsync(new List<StorageLocation>());
        locations.Setup(r => r.GetActivePickingTransportLocationsAsync()).ReturnsAsync(new List<StorageLocation>());
        locations.Setup(r => r.GetByCodeAsync("NAN")).ReturnsAsync((StorageLocation?)null);

        var current = new Mock<ICurrentUserService>();
        current.Setup(s => s.GetCurrentAppUserId()).Returns((int?)null);
        current.Setup(s => s.GetDisplayName()).Returns("t");
        current.Setup(s => s.GetWindowsUserName()).Returns("t");
        current.Setup(s => s.CanOrderLagerAsync()).ReturnsAsync(false);
        current.Setup(s => s.CanOrderGlasAsync()).ReturnsAsync(false);

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var serviceSetting = new Mock<IServiceSettingRepository>();
        serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var ctrl = new PickingController(
            orders.Object,
            Mock.Of<IProductionOrderPickingStatusRepository>(),
            current.Object,
            settings.Object,
            Mock.Of<IHolidayRepository>(),
            new BusinessDayService(),
            bom.Object,
            picking.Object,
            stock.Object,
            locations.Object,
            Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(),
            attrs.Object,
            serviceSetting.Object,
            NullLogger<PickingController>.Instance);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    /// <summary>
    /// UAT-Lauf 1, Befund U1: Die Stuecklisten-Kopfzeile nennt die eigene FA-Nummer (Z4-Konvention).
    /// Beide Faelle hier sind HauptFA (Kommissionierung nur am HauptFA, Spec 2026-09-25 — auf einer
    /// Sub-FA leitet Picking/Bom um, siehe <see cref="Bom_SubFa_RedirectsToIndexWithWarning"/>). Der
    /// Sub-FA-Kopfzeilen-Zusatz lebt seitdem in der read-only Stueckliste (ReadOnlyBomBuilderTests).
    /// </summary>
    [Theory]
    [InlineData("1035235", "1035235")] // hierarchisch: HauptFA (SubOrderNumber == OrderNumber)
    [InlineData("4711", "4711")]       // flach: Backfill-Invariante SubOrderNumber == OrderNumber
    public async Task Bom_ViewModel_CarriesSubOrderNumber(string orderNumber, string subOrderNumber)
    {
        var order = OrderH(orderNumber, subOrderNumber);
        order.Id = 101;
        order.ArticleNumber = "50001929";
        var ctrl = SetupForBom(order,
            new BomQueryResult(new List<BomItem>(), BomDataSources.FaHierarchie));

        var result = await ctrl.Bom(101, null);

        var vm = result.Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<BomViewModel>().Subject;
        vm.OrderNumber.Should().Be(orderNumber);
        vm.SubOrderNumber.Should().Be(subOrderNumber);
        vm.DataSource.Should().Be(BomDataSources.FaHierarchie);
    }

    /// <summary>
    /// BOM-Bridge: die FA-Hierarchie liefert die Sollmenge bereits als Auftragsgesamtmenge
    /// (<c>MengeIstAuftragsmenge</c>) — der Controller darf sie NICHT noch einmal mit der
    /// FA-Stueckzahl multiplizieren. Zusaetzlich landen die hierarchischen Zusatzfelder im
    /// ViewModel und die Anzeige-Flags stehen richtig (HauptFA → Vollstruktur; Picking/Bom ist seit
    /// Spec 2026-09-25 ohnehin nur auf der HauptFA erreichbar).
    /// </summary>
    [Fact]
    public async Task Bom_Hierarchical_DoesNotMultiplyQuantity_AndFlagsHierarchical()
    {
        var order = OrderH("1035235", "1035235");
        order.Id = 101; order.ArticleNumber = "A1"; order.Quantity = 4m;
        var items = new List<BomItem> { new FaHierarchyBomItem { Ressourcenummer = "L1", Position = "2", Menge = 5m, Kommissionieren = "PG" } };
        var ctrl = SetupForBom(order, new BomQueryResult(items, BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true));

        var vm = (await ctrl.Bom(101, null)).Should().BeOfType<ViewResult>().Which.Model.Should().BeOfType<BomViewModel>().Subject;

        vm.Hierarchical.Should().BeTrue();
        vm.FullStructure.Should().BeTrue();             // HauptFA -> Vollstruktur
        vm.Items.Single().Menge.Should().Be(5m);        // keine × Quantity
        vm.Items.Single().Kommissionieren.Should().Be("PG");
    }

    /// <summary>
    /// Kommissionierung nur am HauptFA (Spec 2026-09-25): interaktive Stueckliste einer Sub-FA
    /// leitet mit Hinweis um, statt zu rendern — <see cref="IBomRepository"/> darf gar nicht
    /// erst gefragt werden (kein unnoetiger BOM-Query fuer eine Ansicht, die nie erscheint).
    /// </summary>
    [Fact]
    public async Task Bom_SubFa_RedirectsToIndexWithWarning()
    {
        var order = new ProductionOrder
        {
            Id = 7,
            OrderNumber = "100",
            SubOrderNumber = "100-7",
            ArticleNumber = "A",
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        };
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(order);
        var bom = new Mock<IBomRepository>();

        var ctrl = new PickingController(
            orders.Object,
            Mock.Of<IProductionOrderPickingStatusRepository>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IAppSettingRepository>(),
            Mock.Of<IHolidayRepository>(),
            new BusinessDayService(),
            bom.Object,
            Mock.Of<IPickingRepository>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStorageLocationRepository>(),
            Mock.Of<IArticleRepository>(),
            Mock.Of<IPickingTransferService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IEnaioDmsDocumentRepository>(),
            Mock.Of<IPartRequisitionRepository>(),
            Mock.Of<IArticleAttributeRepository>(),
            Mock.Of<IServiceSettingRepository>(),
            NullLogger<PickingController>.Instance);
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await ctrl.Bom(7, null);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
        ctrl.TempData["WarningMessage"].Should().Be("Kommissionierung erfolgt nur am HauptFA 100.");
        bom.Verify(r => r.GetBomItemsAsync(It.IsAny<BomKey>(), It.IsAny<BomScope>()), Times.Never);
    }

    [Fact]
    public async Task Index_Hierarchical_GroupsByHauptFa_MostUrgentFirst_WithZ4Counts()
    {
        // Eingabe bereits in Prio-Reihenfolge (wie GetReleasedForPickingAsync liefert):
        // H2 (Prio 1, dringlichst), dann H1 mit zwei Sub-FAs (Prio 2 und 5).
        var ctrl = Setup(new List<ProductionOrder>
        {
            OrderH("H2", "H2-1", priority: 1),
            OrderH("H1", "H1-1", priority: 2),
            OrderH("H1", "H1-2", priority: 5, parentSub: "H1-1"),
        }, "", hierarchical: true);

        var result = await ctrl.Index();

        var vm = ((ViewResult)result).Model.Should().BeOfType<PickingListViewModel>().Subject;
        vm.Hierarchical.Should().BeTrue();
        vm.Groups.Should().HaveCount(2);
        vm.HierarchicalRowCount.Should().Be(3);
        vm.Pagination.TotalCount.Should().Be(2); // Paginierung ueber Gruppen

        // Dringlichste Gruppe zuerst (H2 traegt Prio 1); innerhalb H1 die Prio-Reihenfolge der Queue.
        vm.Groups.First().OrderNumber.Should().Be("H2");
        var h1 = vm.Groups.Single(g => g.OrderNumber == "H1");
        h1.Items.Select(i => i.SubOrderNumber).Should().ContainInOrder("H1-1", "H1-2");
        h1.Items.Single(i => i.SubOrderNumber == "H1-2").ParentSubOrderNumber.Should().Be("H1-1");
    }

    [Fact]
    public async Task Index_Hierarchical_FaNumberColumnFilter_MatchesSubOrHaupt()
    {
        var ctrl = Setup(new List<ProductionOrder>
        {
            OrderH("H1", "SUB-AAA", priority: 1),
            OrderH("H2", "SUB-BBB", priority: 2),
        }, "?colf_order-number=SUB-AAA", hierarchical: true);

        var vm = ((ViewResult)(await ctrl.Index())).Model.Should().BeOfType<PickingListViewModel>().Subject;
        vm.Groups.Should().ContainSingle().Which.OrderNumber.Should().Be("H1");
    }

    [Fact]
    public async Task Index_FlatMode_LeavesHierarchicalFalse()
    {
        // Regression: Master aus (default) → keine Gruppierung, bit-identisch zu heute.
        var ctrl = Setup(new List<ProductionOrder> { Order("FA-1") }, "");

        var vm = ((ViewResult)(await ctrl.Index())).Model.Should().BeOfType<PickingListViewModel>().Subject;
        vm.Hierarchical.Should().BeFalse();
        vm.Groups.Should().BeEmpty();
        vm.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Index_ColumnFilter_TextColumn_FiltersAcrossAllRows()
    {
        var ctrl = Setup(new List<ProductionOrder>
        {
            Order("FA-1001"),
            Order("FA-2002"),
            Order("FA-3003")
        }, "?colf_order-number=1001");

        var result = await ctrl.Index() as ViewResult;

        var vm = result!.Model as PickingListViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].OrderNumber.Should().Be("FA-1001");
        vm.Pagination.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_ColumnFilter_DateColumn_MatchesKw()
    {
        // KommissionierTage=4 Arbeitstage, keine Feiertage:
        //   ProductionDate 17.07.2026 (Fr) -> Komm.-Termin 13.07.2026 (Mo) = KW29
        //   ProductionDate 14.08.2026 (Fr) -> Komm.-Termin 10.08.2026 (Mo) = KW33
        var ctrl = Setup(new List<ProductionOrder>
        {
            Order("FA-1001", new DateTime(2026, 7, 17)),
            Order("FA-2002", new DateTime(2026, 8, 14))
        }, "?colf_picking-date=kw29");

        var result = await ctrl.Index() as ViewResult;

        var vm = result!.Model as PickingListViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].OrderNumber.Should().Be("FA-1001");
        vm.Items[0].KommissionierTermin.Should().Be(new DateTime(2026, 7, 13));
        vm.Pagination.TotalCount.Should().Be(1);
    }
}
