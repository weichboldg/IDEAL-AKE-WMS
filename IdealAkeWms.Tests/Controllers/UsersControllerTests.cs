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
    private static UsersController BuildController(
        Mock<IUserRepository>? userRepo = null,
        Mock<IWorkStepRepository>? workStepRepo = null)
    {
        userRepo ??= new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var passwordService = new Mock<IPasswordService>();
        var viewPrefRepo = new Mock<IUserViewPreferenceRepository>();
        workStepRepo ??= new Mock<IWorkStepRepository>();
        workStepRepo.Setup(x => x.GetActiveAsync()).ReturnsAsync(new List<WorkStep>());
        return new UsersController(
            userRepo.Object,
            roleRepo.Object,
            currentUser.Object,
            passwordService.Object,
            viewPrefRepo.Object,
            workStepRepo.Object);
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

    [Fact]
    public async Task Edit_Post_SavesDefaultWorkStep()
    {
        var existing = new User
        {
            Id = 5,
            Name = "Dora",
            IsActive = true,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        User? saved = null;
        userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => saved = u)
            .Returns(Task.CompletedTask);

        var workStepRepo = new Mock<IWorkStepRepository>();
        workStepRepo.Setup(x => x.GetActiveAsync()).ReturnsAsync(new List<WorkStep>
        {
            new() { Id = 10, Code = "VE", Name = "Vormontage Elektrik", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" }
        });

        var ctrl = BuildController(userRepo, workStepRepo);
        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpCtx, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        var vm = new IdealAkeWms.Models.ViewModels.UserEditViewModel
        {
            Id = 5,
            Name = "Dora",
            IsActive = true,
            DefaultWorkStepId = 10
        };

        await ctrl.Edit(5, vm, null);

        saved.Should().NotBeNull();
        saved!.DefaultWorkStepId.Should().Be(10);
    }
}
