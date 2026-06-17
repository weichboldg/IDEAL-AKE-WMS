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

public class AccountControllerTests
{
    private static AccountController BuildController(
        Mock<IUserRepository> userRepo,
        Mock<IWorkStepRepository>? workStepRepo = null,
        Mock<IProductionWorkplaceRepository>? workplaceRepo = null,
        int? currentUserId = 1)
    {
        var passwordService = new Mock<IPasswordService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetCurrentAppUserId()).Returns(currentUserId);
        currentUser.Setup(x => x.GetCurrentAppUserName()).Returns("tester");
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("win-tester");
        workStepRepo ??= new Mock<IWorkStepRepository>();
        workStepRepo.Setup(x => x.GetActiveAsync()).ReturnsAsync(new List<WorkStep>());
        workplaceRepo ??= new Mock<IProductionWorkplaceRepository>();
        workplaceRepo.Setup(x => x.GetAllOrderedAsync()).ReturnsAsync(new List<ProductionWorkplace>());

        var ctrl = new AccountController(
            userRepo.Object,
            passwordService.Object,
            currentUser.Object,
            workStepRepo.Object,
            workplaceRepo.Object);
        var httpContext = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        ctrl.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task Profile_Post_SavesDefaultWorkStep()
    {
        var user = new User
        {
            Id = 1,
            Name = "Tester",
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
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
        var vm = new ProfileViewModel
        {
            Name = "Tester",
            DefaultWorkStepId = 10
        };

        await ctrl.Profile(vm, null);

        saved.Should().NotBeNull();
        saved!.DefaultWorkStepId.Should().Be(10);
    }

    [Fact]
    public async Task Profile_Post_SavesDefaultWorkplace()
    {
        var user = new User
        {
            Id = 1,
            Name = "Tester",
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        User? saved = null;
        userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => saved = u)
            .Returns(Task.CompletedTask);

        var workplaceRepo = new Mock<IProductionWorkplaceRepository>();
        workplaceRepo.Setup(x => x.GetAllOrderedAsync()).ReturnsAsync(new List<ProductionWorkplace>
        {
            new() { Id = 7, Name = "Werkbank 7", CreatedBy = "t", CreatedByWindows = "t" }
        });

        var ctrl = BuildController(userRepo, workplaceRepo: workplaceRepo);
        var vm = new ProfileViewModel
        {
            Name = "Tester",
            DefaultWorkplaceId = 7
        };

        await ctrl.Profile(vm, null);

        saved.Should().NotBeNull();
        saved!.DefaultWorkplaceId.Should().Be(7);
    }
}
