using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Tests fuer den neuen <see cref="AssemblyGroupsApiController"/> (Phase 1 / v1.11.0).
/// Spec 11.2.
/// </summary>
public class AssemblyGroupsApiControllerTests
{
    private readonly Mock<IProductionOrderAssemblyGroupRepository> _groups = new();
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly AssemblyGroupsApiController _controller;

    public AssemblyGroupsApiControllerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");
        _controller = new AssemblyGroupsApiController(
            _groups.Object, _orderRepo.Object, _currentUser.Object);
    }

    private static ProductionOrder MakeOrder(int id, string number) =>
        new()
        {
            Id = id,
            OrderNumber = number,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    private static ProductionOrderAssemblyGroup MakeGroup(int productionOrderId, string groupKey) =>
        new()
        {
            ProductionOrderId = productionOrderId,
            GroupKey = groupKey,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    [Fact]
    public async Task ToggleApplicable_HappyPath_VK_ReturnsOk()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder(1, "WA-1"));
        _groups.Setup(r => r.GetByPoAndKeyAsync(1, "VK")).ReturnsAsync(MakeGroup(1, "VK"));

        var result = await _controller.ToggleApplicable(new AssemblyGroupToggleRequest
        {
            ProductionOrderId = 1,
            GroupKey = "VK",
            Value = true
        });

        result.Should().BeOfType<OkResult>();
        _groups.Verify(r => r.SetIsApplicableAsync(
            1, "VK", true,
            "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task ToggleApplicable_UnknownGroupKey_Returns400()
    {
        var result = await _controller.ToggleApplicable(new AssemblyGroupToggleRequest
        {
            ProductionOrderId = 1,
            GroupKey = "XX",
            Value = true
        });

        result.Should().BeOfType<BadRequestObjectResult>();
        _groups.Verify(r => r.SetIsApplicableAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleApplicable_UnknownOrder_Returns404()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.ToggleApplicable(new AssemblyGroupToggleRequest
        {
            ProductionOrderId = 999,
            GroupKey = "VK",
            Value = true
        });

        result.Should().BeOfType<NotFoundResult>();
        _groups.Verify(r => r.SetIsApplicableAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleApplicable_MissingAssemblyGroupRow_Returns404()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder(1, "WA-1"));
        _groups.Setup(r => r.GetByPoAndKeyAsync(1, "VK")).ReturnsAsync((ProductionOrderAssemblyGroup?)null);

        var result = await _controller.ToggleApplicable(new AssemblyGroupToggleRequest
        {
            ProductionOrderId = 1,
            GroupKey = "VK",
            Value = true
        });

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
