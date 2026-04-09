using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class ProductionOrdersControllerPickerTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAppSettingRepository> _settingRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IBusinessDayService> _businessDayService = new();
    private readonly Mock<IEnaioDmsDocumentRepository> _enaioDmsRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly ProductionOrdersController _controller;

    public ProductionOrdersControllerPickerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");

        _controller = new ProductionOrdersController(
            _orderRepo.Object,
            _currentUser.Object,
            _settingRepo.Object,
            _holidayRepo.Object,
            _businessDayService.Object,
            _enaioDmsRepo.Object,
            _userRepo.Object);

        // TempData requires a provider
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        // UrlHelper for IsLocalUrl
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
        _controller.Url = urlHelper.Object;
    }

    // --- ToggleRelease Tests ---

    [Fact]
    public async Task ToggleRelease_ReleaseWithPicker_SetsAssignedPickerFields()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            ArticleNumber = "ART-001",
            IsReleasedForPicking = false,
            IsDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        var picker = new User
        {
            Id = 5,
            Name = "Max Mustermann",
            IsActive = true,
            IsPicker = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(picker);

        await _controller.ToggleRelease(1, assignedPickerId: 5, returnUrl: null);

        order.IsReleasedForPicking.Should().BeTrue();
        order.AssignedPickerId.Should().Be(5);
        order.AssignedPickerName.Should().Be("Max Mustermann");
        _orderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_Revoke_ClearsPickerFields()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            ArticleNumber = "ART-001",
            IsReleasedForPicking = true,
            AssignedPickerId = 5,
            AssignedPickerName = "Max Mustermann",
            IsDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        order.IsReleasedForPicking.Should().BeFalse();
        order.AssignedPickerId.Should().BeNull();
        order.AssignedPickerName.Should().BeNull();
        _orderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithoutPicker_WhenFeatureEnabled_ReturnsWarning()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            ArticleNumber = "ART-001",
            IsReleasedForPicking = false,
            IsDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        var result = await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        order.IsReleasedForPicking.Should().BeFalse(); // unchanged
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductionOrder>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithoutPicker_WhenFeatureDisabled_Succeeds()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            ArticleNumber = "ART-001",
            IsReleasedForPicking = false,
            IsDone = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("false");

        await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        order.IsReleasedForPicking.Should().BeTrue();
        order.AssignedPickerId.Should().BeNull();
        order.AssignedPickerName.Should().BeNull();
        _orderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_NonExistentOrder_ReturnsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.ToggleRelease(999, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<NotFoundResult>();
    }

    // --- BulkRelease Tests ---

    [Fact]
    public async Task BulkRelease_ReleaseWithPicker_AssignsPickerToAllOrders()
    {
        var orders = new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "WA-1", ArticleNumber = "ART-1", IsReleasedForPicking = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new() { Id = 2, OrderNumber = "WA-2", ArticleNumber = "ART-2", IsReleasedForPicking = false, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        };

        var picker = new User { Id = 5, Name = "Max Mustermann", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(orders[0]);
        _orderRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(orders[1]);
        _orderRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(picker);

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: 5, returnUrl: null);

        orders[0].AssignedPickerId.Should().Be(5);
        orders[0].AssignedPickerName.Should().Be("Max Mustermann");
        orders[1].AssignedPickerId.Should().Be(5);
        orders[1].AssignedPickerName.Should().Be("Max Mustermann");
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductionOrder>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkRelease_Revoke_ClearsPickerFromAllOrders()
    {
        var orders = new List<ProductionOrder>
        {
            new() { Id = 1, OrderNumber = "WA-1", ArticleNumber = "ART-1", IsReleasedForPicking = true, AssignedPickerId = 5, AssignedPickerName = "Max", CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" },
            new() { Id = 2, OrderNumber = "WA-2", ArticleNumber = "ART-2", IsReleasedForPicking = true, AssignedPickerId = 5, AssignedPickerName = "Max", CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" }
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(orders[0]);
        _orderRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(orders[1]);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: false, assignedPickerId: null, returnUrl: null);

        orders[0].AssignedPickerId.Should().BeNull();
        orders[0].AssignedPickerName.Should().BeNull();
        orders[1].AssignedPickerId.Should().BeNull();
        orders[1].AssignedPickerName.Should().BeNull();
    }

    [Fact]
    public async Task BulkRelease_ReleaseWithoutPicker_WhenFeatureEnabled_ReturnsWarning()
    {
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        var result = await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductionOrder>()), Times.Never);
    }

    [Fact]
    public async Task BulkRelease_EmptyIds_ReturnsRedirectWithoutProcessing()
    {
        var result = await _controller.BulkRelease(new List<int>(), release: true, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<ProductionOrder>()), Times.Never);
    }

    // --- ChangeAssignedPicker Tests ---

    [Fact]
    public async Task ChangeAssignedPicker_ValidOrderAndPicker_ReturnsOk()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            AssignedPickerId = 5,
            AssignedPickerName = "Old Picker",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        var newPicker = new User { Id = 10, Name = "New Picker", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(newPicker);

        var result = await _controller.ChangeAssignedPicker(1, 10);

        result.Should().BeOfType<OkResult>();
        order.AssignedPickerId.Should().Be(10);
        order.AssignedPickerName.Should().Be("New Picker");
        _orderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ChangeAssignedPicker_NonExistentOrder_ReturnsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.ChangeAssignedPicker(999, 10);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangeAssignedPicker_NonExistentPicker_ReturnsBadRequest()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "WA-100",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _controller.ChangeAssignedPicker(1, 999);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).Value.Should().Be("Kommissionierer nicht gefunden.");
    }
}
