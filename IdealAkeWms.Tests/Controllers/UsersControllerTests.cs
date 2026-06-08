using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class UsersControllerTests
{
    private static UsersController BuildController()
    {
        var userRepo = new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var passwordService = new Mock<IPasswordService>();
        var viewPrefRepo = new Mock<IUserViewPreferenceRepository>();
        return new UsersController(
            userRepo.Object,
            roleRepo.Object,
            currentUser.Object,
            passwordService.Object,
            viewPrefRepo.Object);
    }

    [Fact]
    public void RoleOverview_ReturnsViewResult()
    {
        var ctrl = BuildController();
        var result = ctrl.RoleOverview();
        result.Should().BeOfType<ViewResult>();
    }
}
