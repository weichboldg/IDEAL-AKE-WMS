using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            serviceSetting.Object);

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
