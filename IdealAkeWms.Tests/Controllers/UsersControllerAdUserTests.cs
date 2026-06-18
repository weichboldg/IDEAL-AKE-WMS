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

public class UsersControllerAdUserTests
{
    private static UsersController BuildController(
        Mock<IUserRepository> userRepo,
        Mock<IActiveDirectoryService> ad,
        Mock<IRoleRepository>? roleRepo = null)
    {
        roleRepo ??= new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(new List<Role>());
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDisplayName()).Returns("Tester");
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("tester");
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var passwordService = new Mock<IPasswordService>();
        var viewPrefRepo = new Mock<IUserViewPreferenceRepository>();
        var workStepRepo = new Mock<IWorkStepRepository>();
        workStepRepo.Setup(x => x.GetActiveAsync()).ReturnsAsync(new List<WorkStep>());
        var workplaceRepo = new Mock<IProductionWorkplaceRepository>();
        workplaceRepo.Setup(x => x.GetAllOrderedAsync()).ReturnsAsync(new List<ProductionWorkplace>());

        var ctrl = new UsersController(
            userRepo.Object,
            roleRepo.Object,
            currentUser.Object,
            passwordService.Object,
            viewPrefRepo.Object,
            workStepRepo.Object,
            workplaceRepo.Object,
            ad.Object);

        var httpCtx = new DefaultHttpContext();
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        ctrl.TempData = new TempDataDictionary(httpCtx, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task CreateAdUser_Get_FiltersAlreadyImported()
    {
        var ad = new Mock<IActiveDirectoryService>();
        ad.Setup(x => x.GetAuthorizationGroupMembersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdUserCandidate>
            {
                new("sam1", "Sam One", "sam1@ake.at", true),
                new("sam2", "Sam Two", "sam2@ake.at", true)
            });

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllWithRolesAsync()).ReturnsAsync(new List<User>
        {
            new() { Id = 1, Name = "Sam One", WindowsUserName = "sam1", IsActive = true, CreatedBy = "t", CreatedByWindows = "t" }
        });

        var ctrl = BuildController(userRepo, ad);

        var result = await ctrl.CreateAdUser() as ViewResult;

        var model = result!.Model as AdUserCreateViewModel;
        model!.Candidates.Should().HaveCount(1);
        model.Candidates[0].SamAccountName.Should().Be("sam2");
        model.AdQueryFailed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAdUser_Post_CreatesUserWithRoles()
    {
        var ad = new Mock<IActiveDirectoryService>();
        ad.Setup(x => x.GetAuthorizationGroupMembersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdUserCandidate>
            {
                new("sam3", "Sam Three", "sam3@ake.at", true)
            });

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetActiveByWindowsUserNameAsync("sam3")).ReturnsAsync((User?)null);
        User? added = null;
        userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.Id = 42; added = u; })
            .ReturnsAsync((User u) => u);

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(new List<Role>());
        int? rolesUserId = null;
        List<int>? rolesAssigned = null;
        roleRepo.Setup(r => r.SetUserRolesAsync(It.IsAny<int>(), It.IsAny<List<int>>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<int, List<int>, string, string>((uid, rids, _, _) => { rolesUserId = uid; rolesAssigned = rids; })
            .Returns(Task.CompletedTask);

        var ctrl = BuildController(userRepo, ad, roleRepo);

        var vm = new AdUserCreateViewModel
        {
            SamAccountName = "sam3",
            DisplayName = "Sam Three",
            SelectedRoleIds = new List<int> { 7 }
        };

        var result = await ctrl.CreateAdUser(vm);

        result.Should().BeOfType<RedirectToActionResult>();
        added.Should().NotBeNull();
        added!.WindowsUserName.Should().Be("sam3");
        added.Name.Should().Be("Sam Three");
        added.IsActive.Should().BeTrue();
        rolesUserId.Should().Be(42);
        rolesAssigned.Should().Equal(new List<int> { 7 });
    }
}
