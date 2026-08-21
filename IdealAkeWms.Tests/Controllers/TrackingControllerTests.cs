using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.Oseon;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class TrackingControllerTests
{
    private static TrackingController BuildController(
        Mock<IOseonProductionOrderRepository>? oseonRepo = null,
        Mock<IOseonGroupViewModelBuilder>? builder = null,
        Mock<IOseonOperationConfigRepository>? opConfigRepo = null,
        Mock<IWorkOperationRepository>? workOpRepo = null,
        Mock<IServiceSettingRepository>? serviceSetting = null)
    {
        oseonRepo ??= new Mock<IOseonProductionOrderRepository>();
        builder ??= new Mock<IOseonGroupViewModelBuilder>();
        opConfigRepo ??= new Mock<IOseonOperationConfigRepository>();
        opConfigRepo
            .Setup(r => r.GetAllAsDictionaryAsync())
            .ReturnsAsync(new Dictionary<string, OseonOperationConfig>());

        workOpRepo ??= new Mock<IWorkOperationRepository>();
        var workplaceRepo = new Mock<IProductionWorkplaceRepository>();
        workplaceRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        var currentUser = new FakeCurrentUserService();
        var trafficLight = new Mock<IOseonTrafficLightService>();
        var businessDays = new Mock<IBusinessDayService>();
        var holidayRepo = new Mock<IHolidayRepository>();
        holidayRepo.Setup(r => r.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());

        // Master ProduktionsauftragHierarchisch: null -> Flachmodus (AKE), "true" -> hierarchisch (IDEAL).
        // Nur beim selbst erzeugten Mock den Default setzen — ein uebergebener Mock behaelt seine Setups
        // (sonst wuerde die It.IsAny-null-Regel eine spezifische Master="true"-Regel ueberschreiben).
        if (serviceSetting == null)
        {
            serviceSetting = new Mock<IServiceSettingRepository>();
            serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        }

        return new TrackingController(
            workOpRepo.Object,
            workplaceRepo.Object,
            currentUser,
            oseonRepo.Object,
            trafficLight.Object,
            opConfigRepo.Object,
            businessDays.Object,
            holidayRepo.Object,
            builder.Object,
            serviceSetting.Object);
    }

    [Fact]
    public async Task OseonGroupDetails_returns_BadRequest_for_empty_customer_order()
    {
        var ctrl = BuildController();
        var result = await ctrl.OseonGroupDetails("");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task OseonGroupDetails_returns_NotFound_when_no_sub_orders()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>());

        var ctrl = BuildController(oseonRepo);

        var result = await ctrl.OseonGroupDetails("UNKNOWN-12345");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OseonGroupDetails_returns_PartialView_with_built_group()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                "K-100", false, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>
            {
                new OseonProductionOrder { Id = 1, OseonOrderNumber = "K-100-1", CustomerOrderNumber = "K-100" }
            });

        var builder = new Mock<IOseonGroupViewModelBuilder>();
        var fakeGroup = new OseonOrderGroupViewModel
        {
            CustomerOrderNumber = "K-100",
            SubOrders = new List<OseonSubOrderViewModel>()
        };
        builder
            .Setup(b => b.BuildAsync("K-100", It.IsAny<IEnumerable<OseonProductionOrder>>(),
                                     true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeGroup);

        var ctrl = BuildController(oseonRepo, builder);

        var result = await ctrl.OseonGroupDetails("K-100");

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_OseonGroupDetails");
        partial.Model.Should().Be(fakeGroup);
    }

    [Fact]
    public async Task OseonGroupDetails_includes_finished_when_showFinished_true()
    {
        var oseonRepo = new Mock<IOseonProductionOrderRepository>();
        oseonRepo
            .Setup(r => r.GetSubOrdersForCustomerOrderAsync(
                "K-200", true, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OseonProductionOrder>
            {
                new OseonProductionOrder { Id = 2, OseonOrderNumber = "K-200-1", CustomerOrderNumber = "K-200", OseonStatus = 90 }
            });

        var builder = new Mock<IOseonGroupViewModelBuilder>();
        builder
            .Setup(b => b.BuildAsync(It.IsAny<string>(), It.IsAny<IEnumerable<OseonProductionOrder>>(),
                                     It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OseonOrderGroupViewModel { CustomerOrderNumber = "K-200" });

        var ctrl = BuildController(oseonRepo, builder);

        var result = await ctrl.OseonGroupDetails("K-200", showFinished: true);

        result.Should().BeOfType<PartialViewResult>();
        oseonRepo.Verify(r => r.GetSubOrdersForCustomerOrderAsync(
            "K-200", true, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Index (WMS-Teileverfolgung) Hierarchie ---

    private static WorkOperation Wop(ProductionOrder po, int seq, bool reported)
        => new()
        {
            ProductionOrderId = po.Id,
            ProductionOrder = po,
            OperationNumber = $"AG-{seq}",
            Name = $"Arbeitsgang {seq}",
            Sequence = seq,
            IsReportable = true,
            IsReported = reported,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        };

    private static ProductionOrder Po(int id, string orderNumber, string subOrderNumber, string? parentSub = null)
        => new()
        {
            Id = id,
            OrderNumber = orderNumber,
            SubOrderNumber = subOrderNumber,
            ParentSubOrderNumber = parentSub,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t",
        };

    [Fact]
    public async Task Index_Hierarchical_BuildsHauptFaTree_WithTwoLevelProgress()
    {
        // HauptFA H1: Sub H1-1 (beide AGs rückgemeldet = fertig), Sub H1-2 (ein AG offen).
        // HauptFA H2: Sub H2-1 (AG offen).
        var h1s1 = Po(1, "H1", "H1-1");
        var h1s2 = Po(2, "H1", "H1-2", parentSub: "H1-1");
        var h2s1 = Po(3, "H2", "H2-1");
        var ops = new List<WorkOperation>
        {
            Wop(h1s1, 1, reported: true),
            Wop(h1s1, 2, reported: true),
            Wop(h1s2, 1, reported: true),
            Wop(h1s2, 2, reported: false),
            Wop(h2s1, 1, reported: false),
        };

        var workOpRepo = new Mock<IWorkOperationRepository>();
        workOpRepo.Setup(r => r.GetAllWithOrderAndWorkplaceAsync()).ReturnsAsync(ops);
        var serviceSetting = new Mock<IServiceSettingRepository>();
        serviceSetting.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        serviceSetting
            .Setup(s => s.GetValueAsync(
                IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master))
            .ReturnsAsync("true");

        var ctrl = BuildController(workOpRepo: workOpRepo, serviceSetting: serviceSetting);

        // showReported=true, damit auch die fertigen Sub-FAs/AGs in der Auswahl bleiben.
        var result = await ctrl.Index(null, null, showReported: true);

        var vm = ((ViewResult)result).Model.Should().BeOfType<TrackingViewModel>().Subject;
        vm.Hierarchical.Should().BeTrue();
        vm.HauptGroups.Should().HaveCount(2);
        vm.HierarchicalRowCount.Should().Be(3);            // 3 Sub-FAs
        vm.Pagination.TotalCount.Should().Be(2);           // 2 HauptFA-Gruppen

        var h1 = vm.HauptGroups.Single(g => g.OrderNumber == "H1");
        h1.SubGroups.Select(s => s.SubOrderNumber).Should().ContainInOrder("H1-1", "H1-2"); // Z2
        h1.TotalSubOrders.Should().Be(2);
        h1.FinishedSubOrders.Should().Be(1);               // nur H1-1 komplett rückgemeldet
        h1.SubGroups.Single(s => s.SubOrderNumber == "H1-1").ReportedOperations.Should().Be(2);
        h1.SubGroups.Single(s => s.SubOrderNumber == "H1-2").ReportedOperations.Should().Be(1);
    }

    [Fact]
    public async Task Index_FlatMode_LeavesHierarchicalFalse()
    {
        var po = Po(1, "FA-001", "FA-001");
        var workOpRepo = new Mock<IWorkOperationRepository>();
        workOpRepo.Setup(r => r.GetAllWithOrderAndWorkplaceAsync())
            .ReturnsAsync(new List<WorkOperation> { Wop(po, 1, reported: false) });

        var ctrl = BuildController(workOpRepo: workOpRepo); // serviceSetting default null -> flach

        var vm = ((ViewResult)(await ctrl.Index(null, null, showReported: true)))
            .Model.Should().BeOfType<TrackingViewModel>().Subject;
        vm.Hierarchical.Should().BeFalse();
        vm.HauptGroups.Should().BeEmpty();
        vm.OrderGroups.Should().ContainSingle();
    }
}
