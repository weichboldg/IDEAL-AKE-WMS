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
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class WorkStepsControllerTests
{
    private static (WorkStepsController Ctrl, Mock<IWorkStepRepository> Repo) CreateController()
    {
        var repo = new Mock<IWorkStepRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        currentUser.Setup(x => x.GetDisplayName()).Returns("Tester");
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\tester");

        var ctrl = new WorkStepsController(repo.Object, currentUser.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return (ctrl, repo);
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var steps = new List<WorkStep>
        {
            new() { Id = 1, Code = "VL", Name = "Vormontage Luefter", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Code = "VK", Name = "Vormontage Kuehlung", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Code = "VE", Name = "Vormontage Elektrik", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var (ctrl, repo) = CreateController();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(steps);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_code=VL");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<WorkStep>;
        model!.Should().HaveCount(1);
        model[0].Code.Should().Be("VL");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_Blocked_WhenInUse()
    {
        var (ctrl, repo) = CreateController();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(
            new WorkStep { Id = 7, Code = "VL", Name = "Vormontage Luefter", CreatedBy = "t", CreatedByWindows = "t" });
        repo.Setup(r => r.DeleteAsync(7)).ReturnsAsync(false);

        var result = await ctrl.Delete(7);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(WorkStepsController.Index));
        ctrl.TempData["WarningMessage"].Should().Be(
            "Arbeitsgang wird verwendet und kann nicht geloescht werden — bitte deaktivieren.");
    }

    [Fact]
    public async Task Create_RejectsDuplicateCode()
    {
        var (ctrl, repo) = CreateController();
        repo.Setup(r => r.GetByCodeAsync("VL")).ReturnsAsync(
            new WorkStep { Id = 1, Code = "VL", Name = "Bestehender", CreatedBy = "t", CreatedByWindows = "t" });

        var result = await ctrl.Create(new WorkStep { Code = "VL", Name = "Neuer Arbeitsgang" });

        result.Should().BeOfType<ViewResult>();
        ctrl.ModelState.IsValid.Should().BeFalse();
        ctrl.ModelState[nameof(WorkStep.Code)]!.Errors.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<WorkStep>()), Times.Never);
    }
}
