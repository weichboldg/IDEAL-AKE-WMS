using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class ProductionWorkplacesControllerTests
{
    private static ProductionWorkplacesController CreateController(ApplicationDbContext ctx)
    {
        var repo = new ProductionWorkplaceRepository(ctx);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(u => u.GetActiveUsersAsync()).ReturnsAsync(new List<User>());

        var userSvc = new Mock<ICurrentUserService>();
        userSvc.Setup(u => u.GetDisplayName()).Returns("tester");
        userSvc.Setup(u => u.GetWindowsUserName()).Returns("tester");

        var appSettings = new Mock<IAppSettingRepository>();
        appSettings.Setup(a => a.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var controller = new ProductionWorkplacesController(repo, userRepo.Object, userSvc.Object, appSettings.Object, ctx);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Edit_PersistsBdeUseCustomShiftPlan()
    {
        var ctx = TestDbContextFactory.Create();
        var wp = new ProductionWorkplace
        {
            Name = "WB",
            BdeAktiv = true,
            BdeUseCustomShiftPlan = false,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);
        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = wp.Id,
            Name = wp.Name,
            BdeAktiv = true,
            BdeUseCustomShiftPlan = true
        };
        await controller.Edit(wp.Id, vm);

        var updated = await ctx.ProductionWorkplaces.FindAsync(wp.Id);
        updated!.BdeUseCustomShiftPlan.Should().BeTrue();
    }
}
