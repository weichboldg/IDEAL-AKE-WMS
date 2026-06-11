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

/// <summary>
/// Controller-Tests fuer den nach Phase 2 (v1.12.0) abgespeckten <see cref="ProductionOrdersController"/>.
/// Index liefert nur noch <see cref="ProductionOrderListViewModel"/> (Sage-Master + Coating-Flags) und
/// die 4 alten Mutationen sind durch 301-Compat-Redirects auf PickingLeitstand ersetzt.
/// </summary>
public class ProductionOrdersControllerSlimTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<IProductionOrderPickingStatusRepository> _pickingStatusRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settingRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IBusinessDayService> _businessDayService = new();
    private readonly Mock<IEnaioDmsDocumentRepository> _enaioDmsRepo = new();
    private readonly ProductionOrdersController _controller;

    public ProductionOrdersControllerSlimTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");

        _settingRepo.Setup(s => s.GetIntValueAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string _, int defaultVal) => defaultVal);
        _settingRepo.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _holidayRepo.Setup(h => h.GetHolidayDatesAsync()).ReturnsAsync(new HashSet<DateTime>());
        _businessDayService.Setup(b => b.ParsePickupDays(It.IsAny<string>())).Returns(new HashSet<DayOfWeek>());
        _enaioDmsRepo.Setup(e => e.GetByOrderNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, List<EnaioDmsDocumentLink>>());

        _controller = new ProductionOrdersController(
            _orderRepo.Object,
            _pickingStatusRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object);

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    private static ProductionOrder MakeOrder(int id, string number, bool isDone = false) =>
        new()
        {
            Id = id,
            OrderNumber = number,
            IsDone = isDone,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    private static LeitstandOrderRow MakeRow(int id, string number, bool isDone = false) =>
        new(id, number, 1m, null, "ART-001", null, null, null, null, isDone, false, null);

    private static LeitstandOrderPage MakePage(params LeitstandOrderRow[] rows) =>
        new(rows.ToList(), rows.Length);

    [Fact]
    public async Task Index_ReturnsSlimViewModel_NoStatusPivot()
    {
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));

        var ps = new ProductionOrderPickingStatus
        {
            ProductionOrderId = 1,
            HasCoatingParts = true,
            IsCoatingDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus> { { 1, ps } });

        _currentUser.Setup(u => u.CanPickAsync()).ReturnsAsync(true);

        var result = await _controller.Index(null, null, null, false, 1, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var vm = viewResult.Model.Should().BeOfType<ProductionOrderListViewModel>().Subject;
        vm.Items.Should().HaveCount(1);

        var item = vm.Items.Single();
        // ProductionOrderListItem hat ABSICHTLICH keine PickingStatus-Felder ausser Coating (Spec 6.1)
        typeof(ProductionOrderListItem).GetProperty("PickingStatus").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("IsReleasedForPicking").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasGlass").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasExternalPurchase").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasCooling").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("HasFan").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("PickingPriority").Should().BeNull();
        typeof(ProductionOrderListItem).GetProperty("AssignedPickerId").Should().BeNull();

        item.HasCoatingParts.Should().BeTrue();
        item.IsCoatingDone.Should().BeFalse();
        vm.CanPick.Should().BeTrue();
    }

    [Fact]
    public async Task Index_FilterByOrderNumber_AppliesContainsFilter()
    {
        // Server-side filtering happens in the repo; controller relays the filter
        // and uses the rows that came back.
        _orderRepo.Setup(r => r.GetForLeitstandAsync(
                "100", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<string, string>?>()))
            .ReturnsAsync(MakePage(MakeRow(1, "FA-100")));
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, ProductionOrderPickingStatus>());

        var result = await _controller.Index("100", null, null, false, 1, null);

        var vm = (ProductionOrderListViewModel)((ViewResult)result).Model!;
        vm.Items.Should().HaveCount(1);
        vm.Items.Single().OrderNumber.Should().Be("FA-100");
        vm.FilterOrderNumber.Should().Be("100");
    }

    [Fact]
    public void ToggleRelease_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.ToggleRelease();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void BulkRelease_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.BulkRelease();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void SetPriority_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.SetPriority();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }

    [Fact]
    public void ChangeAssignedPicker_Redirects301_ToPickingLeitstand()
    {
        var result = _controller.ChangeAssignedPicker();

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.Permanent.Should().BeTrue();
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("PickingLeitstand");
    }
}
