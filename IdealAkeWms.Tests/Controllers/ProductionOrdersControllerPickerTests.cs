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

/// <summary>
/// Controller-Tests fuer Picker-Assignment-Flows.
/// Seit Phase 1 (v1.11.0) leben Release-/Picker-/Priority-Werte in
/// <see cref="ProductionOrderPickingStatus"/>. Die Tests assertieren ueber
/// Mock-Verifies auf <see cref="IProductionOrderPickingStatusRepository"/>.
/// </summary>
public class ProductionOrdersControllerPickerTests
{
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<IProductionOrderPickingStatusRepository> _pickingStatusRepo = new();
    private readonly Mock<IProductionOrderAssemblyGroupRepository> _assemblyGroupRepo = new();
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
            _pickingStatusRepo.Object,
            _assemblyGroupRepo.Object,
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

    private static ProductionOrder MakeOrder(int id, string number, string? articleNumber = "ART-001", bool isDone = false) =>
        new()
        {
            Id = id,
            OrderNumber = number,
            ArticleNumber = articleNumber,
            IsDone = isDone,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    private static ProductionOrderPickingStatus MakePs(int productionOrderId, bool released = false,
        int? assignedPickerId = null, string? assignedPickerName = null, int? priority = null) =>
        new()
        {
            ProductionOrderId = productionOrderId,
            IsReleasedForPicking = released,
            AssignedPickerId = assignedPickerId,
            AssignedPickerName = assignedPickerName,
            PickingPriority = priority,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    // --- ToggleRelease Tests ---

    [Fact]
    public async Task ToggleRelease_ReleaseWithPicker_SetsAssignedPickerFields()
    {
        var order = MakeOrder(1, "WA-100");
        var ps = MakePs(productionOrderId: 1, released: false);
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
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _pickingStatusRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(picker);

        await _controller.ToggleRelease(1, assignedPickerId: 5, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, true, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            1, 5, "Max Mustermann",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRelease_Revoke_ClearsPickerFields()
    {
        var order = MakeOrder(1, "WA-100");
        var ps = MakePs(productionOrderId: 1, released: true, assignedPickerId: 5, assignedPickerName: "Max Mustermann");

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        // Revoke ruft SetReleaseAsync(released=false) auf; Repository selbst loescht AssignedPicker-Felder.
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, false, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        // Kein SetAssignedPickerAsync, da nicht released
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithoutPicker_WhenFeatureEnabled_ReturnsWarning()
    {
        var order = MakeOrder(1, "WA-100");
        var ps = MakePs(productionOrderId: 1, released: false);

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        var result = await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleRelease_ReleaseWithoutPicker_WhenFeatureDisabled_Succeeds()
    {
        var order = MakeOrder(1, "WA-100");
        var ps = MakePs(productionOrderId: 1, released: false);

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps);
        _pickingStatusRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("false");

        await _controller.ToggleRelease(1, assignedPickerId: null, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, true, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
        var order1 = MakeOrder(1, "WA-1", articleNumber: "ART-1");
        var order2 = MakeOrder(2, "WA-2", articleNumber: "ART-2");
        var ps1 = MakePs(productionOrderId: 1, released: false);
        var ps2 = MakePs(productionOrderId: 2, released: false);
        var picker = new User { Id = 5, Name = "Max Mustermann", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order1);
        _orderRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(order2);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps1);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(2)).ReturnsAsync(ps2);
        _pickingStatusRepo.Setup(r => r.GetReleasedForPickingAsync()).ReturnsAsync(new List<ProductionOrder>());
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");
        _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(picker);

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: 5, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, true, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            2, true, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            1, 5, "Max Mustermann",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            2, 5, "Max Mustermann",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task BulkRelease_Revoke_ClearsPickerFromAllOrders()
    {
        var order1 = MakeOrder(1, "WA-1", articleNumber: "ART-1");
        var order2 = MakeOrder(2, "WA-2", articleNumber: "ART-2");
        var ps1 = MakePs(productionOrderId: 1, released: true, assignedPickerId: 5, assignedPickerName: "Max");
        var ps2 = MakePs(productionOrderId: 2, released: true, assignedPickerId: 5, assignedPickerName: "Max");

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order1);
        _orderRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(order2);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(ps1);
        _pickingStatusRepo.Setup(r => r.GetByProductionOrderIdAsync(2)).ReturnsAsync(ps2);
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        await _controller.BulkRelease(new List<int> { 1, 2 }, release: false, assignedPickerId: null, returnUrl: null);

        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            1, false, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            2, false, It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        // Bei Revoke kein SetAssignedPickerAsync — Repository selbst loescht Felder
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BulkRelease_ReleaseWithoutPicker_WhenFeatureEnabled_ReturnsWarning()
    {
        _settingRepo.Setup(s => s.GetValueAsync("KommissionierungMitZuweisung")).ReturnsAsync("true");

        var result = await _controller.BulkRelease(new List<int> { 1, 2 }, release: true, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BulkRelease_EmptyIds_ReturnsRedirectWithoutProcessing()
    {
        var result = await _controller.BulkRelease(new List<int>(), release: true, assignedPickerId: null, returnUrl: null);

        result.Should().BeOfType<RedirectToActionResult>();
        _pickingStatusRepo.Verify(r => r.SetReleaseAsync(
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- ChangeAssignedPicker Tests ---

    [Fact]
    public async Task ChangeAssignedPicker_ValidOrderAndPicker_ReturnsOk()
    {
        var order = MakeOrder(1, "WA-100");
        var newPicker = new User { Id = 10, Name = "New Picker", IsActive = true, IsPicker = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test", CreatedByWindows = "test" };

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(newPicker);

        var result = await _controller.ChangeAssignedPicker(1, 10);

        result.Should().BeOfType<OkResult>();
        _pickingStatusRepo.Verify(r => r.SetAssignedPickerAsync(
            1, 10, "New Picker",
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
        var order = MakeOrder(1, "WA-100");

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var result = await _controller.ChangeAssignedPicker(1, 999);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).Value.Should().Be("Kommissionierer nicht gefunden.");
    }
}
