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
    private static PickingController Setup(List<ProductionOrder> releasedOrders, string queryString)
    {
        var pickingStatus = new Mock<IProductionOrderPickingStatusRepository>();
        pickingStatus.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(releasedOrders);

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
            Mock.Of<IArticleAttributeRepository>());

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
