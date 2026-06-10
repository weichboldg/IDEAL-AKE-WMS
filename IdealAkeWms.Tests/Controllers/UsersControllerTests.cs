using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class UsersControllerTests
{
    private static UsersController BuildController(Mock<IUserRepository>? userRepo = null)
    {
        userRepo ??= new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
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

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice Admin", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "Bob Builder", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "Carla Clark", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" }
        };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllWithRolesAsync()).ReturnsAsync(users);
        var ctrl = BuildController(userRepo);

        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpCtx.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_name=Bob");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<User>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Bob Builder");
        var pagination = ctrl.ViewBag.Pagination as IdealAkeWms.Models.ViewModels.PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
