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

public class MissingPartsLagerControllerTests
{
    private static (MissingPartsLagerController ctrl, Mock<IWarehouseRequisitionRepository> repo,
                    Mock<IProductionWorkplaceRepository> wp, Mock<ICurrentUserService> user) Build()
    {
        var repo = new Mock<IWarehouseRequisitionRepository>();
        var wp = new Mock<IProductionWorkplaceRepository>();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        user.Setup(u => u.GetCurrentAppUserId()).Returns(1);
        wp.Setup(w => w.GetAllAsync()).ReturnsAsync(new List<ProductionWorkplace>());
        repo.Setup(r => r.GetMissingPartsAsync(It.IsAny<ShortageStatus>(),
                It.IsAny<int?>(),
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(((IReadOnlyList<MissingPartRow>)new List<MissingPartRow>(), 0));
        var ctrl = new MissingPartsLagerController(repo.Object, wp.Object, user.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (ctrl, repo, wp, user);
    }

    [Fact]
    public async Task Index_DefaultReturnsViewModel()
    {
        var (ctrl, _, _, _) = Build();
        var result = await ctrl.Index();
        result.Should().BeOfType<ViewResult>();
        var vm = (result as ViewResult)!.Model as MissingPartsListViewModel;
        vm!.MineOnly.Should().BeFalse();
        vm.ActiveTab.Should().Be(ShortageStatus.WillBeRestocked);
    }

    [Fact]
    public async Task Index_TabParam_PassedToRepo()
    {
        var (ctrl, repo, _, _) = Build();
        await ctrl.Index(tab: ShortageStatus.NoRestock);
        repo.Verify(r => r.GetMissingPartsAsync(ShortageStatus.NoRestock,
            It.IsAny<int?>(), It.IsAny<IReadOnlyDictionary<string, string>?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Index_DoesNotApplyMineOnly()
    {
        var (ctrl, _, wp, _) = Build();
        await ctrl.Index();
        wp.Verify(w => w.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
    }
}
