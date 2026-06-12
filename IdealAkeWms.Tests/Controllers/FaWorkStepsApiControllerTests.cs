using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Tests fuer den <see cref="FaWorkStepsApiController"/> (FA-Vorbau v1.22.0, Plan Task 6).
/// </summary>
public class FaWorkStepsApiControllerTests
{
    private readonly Mock<IFaWorkStepRepository> _faWorkSteps = new();
    private readonly Mock<IWorkStepRepository> _workSteps = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly FaWorkStepsApiController _controller;

    public FaWorkStepsApiControllerTests()
    {
        _currentUser.Setup(x => x.GetDisplayName()).Returns("TestUser");
        _currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\testuser");
        _controller = new FaWorkStepsApiController(
            _faWorkSteps.Object, _workSteps.Object, _currentUser.Object);
    }

    private static WorkStep MakeWorkStep(int id, string code, bool isActive = true) =>
        new()
        {
            Id = id,
            Code = code,
            Name = code,
            IsActive = isActive,
            CreatedAt = DateTime.Now,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    [Fact]
    public async Task Toggle_CreatesRow_ForValidWorkStepCode()
    {
        _workSteps.Setup(r => r.GetByCodeAsync("VL")).ReturnsAsync(MakeWorkStep(10, "VL"));

        var result = await _controller.Toggle(
            new FaWorkStepsApiController.ToggleRequest(1, "VL", true));

        result.Should().BeOfType<OkResult>();
        _faWorkSteps.Verify(r => r.SetActiveAsync(
            1, 10, true, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task Toggle_ReturnsBadRequest_ForUnknownCode()
    {
        _workSteps.Setup(r => r.GetByCodeAsync("XX")).ReturnsAsync((WorkStep?)null);

        var result = await _controller.Toggle(
            new FaWorkStepsApiController.ToggleRequest(1, "XX", true));

        result.Should().BeOfType<BadRequestObjectResult>();
        _faWorkSteps.Verify(r => r.SetActiveAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompleted_SetsIsCompleted()
    {
        _faWorkSteps.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new FaWorkStep
        {
            Id = 5,
            ProductionOrderId = 1,
            WorkStepId = 10
        });

        var result = await _controller.ToggleCompleted(
            new FaWorkStepsApiController.ToggleCompletedRequest(5, true));

        result.Should().BeOfType<OkResult>();
        _faWorkSteps.Verify(r => r.SetIsCompletedAsync(
            5, true, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }
}
