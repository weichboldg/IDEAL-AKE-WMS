using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Tests fuer den neuen <see cref="BdeStatusApiController"/> (Phase 1 / v1.11.0).
/// Spec 11.2.
/// </summary>
public class BdeStatusApiControllerTests
{
    private readonly Mock<IProductionOrderBdeStatusRepository> _bdeStatus = new();
    private readonly Mock<IProductionOrderRepository> _orderRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly BdeStatusApiController _controller;

    public BdeStatusApiControllerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");
        _controller = new BdeStatusApiController(
            _bdeStatus.Object, _orderRepo.Object, _currentUser.Object);
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

    private static ProductionOrderBdeStatus MakeBde(int productionOrderId) =>
        new()
        {
            ProductionOrderId = productionOrderId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    [Fact]
    public async Task Toggle_HappyPath_ReturnsOk()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeOrder(1, "WA-1"));
        _bdeStatus.Setup(r => r.GetByProductionOrderIdAsync(1)).ReturnsAsync(MakeBde(1));

        var result = await _controller.Toggle(new BdeStatusToggleRequest
        {
            ProductionOrderId = 1,
            Field = "IsDoneBde",
            Value = true
        });

        result.Should().BeOfType<OkResult>();
        _bdeStatus.Verify(r => r.SetIsDoneBdeAsync(
            1, true,
            "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task Toggle_UnknownField_Returns400()
    {
        var result = await _controller.Toggle(new BdeStatusToggleRequest
        {
            ProductionOrderId = 1,
            Field = "NotAField",
            Value = true
        });

        result.Should().BeOfType<BadRequestObjectResult>();
        _bdeStatus.Verify(r => r.SetIsDoneBdeAsync(
            It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Toggle_UnknownOrder_Returns404()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ProductionOrder?)null);

        var result = await _controller.Toggle(new BdeStatusToggleRequest
        {
            ProductionOrderId = 999,
            Field = "IsDoneBde",
            Value = true
        });

        result.Should().BeOfType<NotFoundResult>();
    }
}
