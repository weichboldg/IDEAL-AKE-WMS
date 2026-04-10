using FluentAssertions;
using IdealAkeWms.Controllers.Api;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests;

public class UserViewPreferencesApiControllerTests
{
    private readonly Mock<IUserViewPreferenceRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userServiceMock = new();

    private UserViewPreferencesApiController CreateController()
    {
        _userServiceMock.Setup(s => s.GetCurrentAppUserId()).Returns(42);
        _userServiceMock.Setup(s => s.IsLoggedIn()).Returns(true);
        _userServiceMock.Setup(s => s.GetDisplayName()).Returns("TestUser");
        _userServiceMock.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\testuser");
        return new UserViewPreferencesApiController(_repoMock.Object, _userServiceMock.Object);
    }

    [Fact]
    public async Task Get_Returns204_WhenNoPreference()
    {
        var controller = CreateController();
        _repoMock.Setup(r => r.GetByUserAndViewAsync(42, "ProductionOrders"))
            .ReturnsAsync((UserViewPreference?)null);

        var result = await controller.Get("ProductionOrders");
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Get_Returns200WithJson_WhenPreferenceExists()
    {
        var controller = CreateController();
        var pref = new UserViewPreference { SettingsJson = """{"columns":[]}""" };
        _repoMock.Setup(r => r.GetByUserAndViewAsync(42, "ProductionOrders"))
            .ReturnsAsync(pref);

        var result = await controller.Get("ProductionOrders");
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("""{"columns":[]}""");
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_ForInvalidViewKey()
    {
        var controller = CreateController();
        var result = await controller.Get("InvalidView");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Put_CallsSave_WithCorrectParams()
    {
        var controller = CreateController();
        var json = """{"columns":[{"key":"OrderNumber","visible":true}]}""";

        var result = await controller.Put("ProductionOrders", json);
        result.Should().BeOfType<OkResult>();
        _repoMock.Verify(r => r.SaveAsync(42, "ProductionOrders", json, "TestUser", "DOMAIN\\testuser"), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsDeleteByUserAndView()
    {
        var controller = CreateController();
        var result = await controller.Delete("ProductionOrders");
        result.Should().BeOfType<OkResult>();
        _repoMock.Verify(r => r.DeleteByUserAndViewAsync(42, "ProductionOrders"), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenNotLoggedIn()
    {
        _userServiceMock.Setup(s => s.IsLoggedIn()).Returns(false);
        _userServiceMock.Setup(s => s.GetCurrentAppUserId()).Returns((int?)null);
        var controller = new UserViewPreferencesApiController(_repoMock.Object, _userServiceMock.Object);

        var result = await controller.Get("ProductionOrders");
        result.Should().BeOfType<UnauthorizedResult>();
    }
}
