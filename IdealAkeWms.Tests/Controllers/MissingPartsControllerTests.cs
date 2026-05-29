using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class MissingPartsControllerTests
{
    private static (MissingPartsController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        user.Setup(u => u.GetDisplayName()).Returns("test");
        user.Setup(u => u.GetWindowsUserName()).Returns("test\\test");
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        wp.Setup(w => w.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        var ctrl = new MissingPartsController(repo.Object, wp.Object, user.Object);
        // HttpContext fuer ColumnFilterHelper
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return (ctrl, repo, wp, user);
    }

    [Fact]
    public async Task Index_NoMineOnly_PassesWorkplaceIdToRepoUnchanged()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(workplaceId: 5, mineOnly: false);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock, 5,
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            null, null, 1, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Index_MineOnly_SingleUserWorkplace_FiltersByThatId()
    {
        var (ctrl, repo, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        await ctrl.Index(workplaceId: null, mineOnly: true);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock, 7,
            It.IsAny<IReadOnlyDictionary<string, string>?>(),
            null, null, 1, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Index_MineOnly_WithInconsistentWorkplaceId_ReturnsEmpty()
    {
        var (ctrl, _, wp, user) = Build();
        user.Setup(u => u.GetCurrentAppUserId()).Returns(42);
        wp.Setup(w => w.GetByUserIdAsync(42)).ReturnsAsync(new List<ProductionWorkplace>
            { new ProductionWorkplace { Id = 7, Name = "WB7" } });
        var result = await ctrl.Index(workplaceId: 99, mineOnly: true);
        var vm = (result as ViewResult)?.Model as MissingPartsListViewModel;
        vm!.Items.Should().BeEmpty();
        vm.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_ReturnsViewWithModel()
    {
        var (ctrl, _, _, _) = Build();
        var result = await ctrl.Index(null, false);
        result.Should().BeOfType<ViewResult>();
    }
}
