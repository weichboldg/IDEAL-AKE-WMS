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
        Mock<IOseonOperationConfigRepository>? opConfigRepo = null)
    {
        oseonRepo ??= new Mock<IOseonProductionOrderRepository>();
        builder ??= new Mock<IOseonGroupViewModelBuilder>();
        opConfigRepo ??= new Mock<IOseonOperationConfigRepository>();
        opConfigRepo
            .Setup(r => r.GetAllAsDictionaryAsync())
            .ReturnsAsync(new Dictionary<string, OseonOperationConfig>());

        var workOpRepo = new Mock<IWorkOperationRepository>();
        var workplaceRepo = new Mock<IProductionWorkplaceRepository>();
        var currentUser = new FakeCurrentUserService();
        var trafficLight = new Mock<IOseonTrafficLightService>();
        var businessDays = new Mock<IBusinessDayService>();
        var holidayRepo = new Mock<IHolidayRepository>();
        holidayRepo.Setup(r => r.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());

        return new TrackingController(
            workOpRepo.Object,
            workplaceRepo.Object,
            currentUser,
            oseonRepo.Object,
            trafficLight.Object,
            opConfigRepo.Object,
            businessDays.Object,
            holidayRepo.Object,
            builder.Object);
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
}
