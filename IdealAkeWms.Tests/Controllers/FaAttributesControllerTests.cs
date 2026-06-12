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

public class FaAttributesControllerTests
{
    private static (FaAttributesController Ctrl, Mock<IFaAttributeRepository> Repo, Mock<IWorkStepRepository> WorkStepRepo) CreateController()
    {
        var repo = new Mock<IFaAttributeRepository>();
        var workStepRepo = new Mock<IWorkStepRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        currentUser.Setup(x => x.GetDisplayName()).Returns("Tester");
        currentUser.Setup(x => x.GetWindowsUserName()).Returns("DOMAIN\\tester");

        var ctrl = new FaAttributesController(repo.Object, workStepRepo.Object, currentUser.Object);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return (ctrl, repo, workStepRepo);
    }

    private static FaAttributeDefinition CreateDefinition(int id, string name, params string[] workStepCodes)
    {
        var def = new FaAttributeDefinition
        {
            Id = id,
            Name = name,
            AttributeType = AttributeType.Dropdown,
            SortOrder = id,
            IsActive = true,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };
        foreach (var code in workStepCodes)
        {
            def.WorkSteps.Add(new FaAttributeWorkStep
            {
                FaAttributeDefinitionId = id,
                Definition = def,
                WorkStep = new WorkStep { Code = code, Name = code, CreatedBy = "t", CreatedByWindows = "t" }
            });
        }
        return def;
    }

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var definitions = new List<FaAttributeDefinition>
        {
            CreateDefinition(1, "Verdampfergroesse", "VK"),
            CreateDefinition(2, "Leitungsausgang", "VK", "VL"),
            CreateDefinition(3, "Ventil aussenliegend")
        };
        var (ctrl, repo, _) = CreateController();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(definitions);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_work-steps=VL");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<FaAttributeDefinition>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Leitungsausgang");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task EditPost_SyncsWorkStepAssignment()
    {
        var (ctrl, repo, _) = CreateController();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(CreateDefinition(5, "Verdampfergroesse"));

        var result = await ctrl.Edit(5, "Verdampfergroesse", sortOrder: 2, isActive: true, workStepIds: new[] { 10, 11 });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(FaAttributesController.Index));
        repo.Verify(r => r.UpdateDefinitionAsync(It.Is<FaAttributeDefinition>(d => d.Id == 5 && d.SortOrder == 2)), Times.Once);
        repo.Verify(r => r.SetWorkStepsAsync(5, It.Is<List<int>>(l =>
            l.Count == 2 && l.Contains(10) && l.Contains(11)),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteOption_ShowsWarning_WhenInUse()
    {
        var (ctrl, repo, _) = CreateController();
        repo.Setup(r => r.DeleteOptionAsync(50)).ReturnsAsync(false);

        var result = await ctrl.DeleteOption(50, definitionId: 5);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Which;
        redirect.ActionName.Should().Be(nameof(FaAttributesController.Edit));
        redirect.RouteValues!["id"].Should().Be(5);
        ctrl.TempData["WarningMessage"].Should().Be("Option wird verwendet — bitte stattdessen deaktivieren.");
    }
}
