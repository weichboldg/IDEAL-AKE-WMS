using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class BdeMasterDataControllerTests
{
    private static BdeMasterDataController CreateController(
        List<BdeOperator>? operators = null,
        List<BdeActivity>? activities = null,
        List<BdeTerminal>? terminals = null)
    {
        var opRepo = new Mock<IBdeOperatorRepository>();
        opRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(operators ?? new List<BdeOperator>());
        var actRepo = new Mock<IBdeActivityRepository>();
        actRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(activities ?? new List<BdeActivity>());
        var termRepo = new Mock<IBdeTerminalRepository>();
        termRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(terminals ?? new List<BdeTerminal>());
        var userRepo = new Mock<IUserRepository>();
        var wpRepo = new Mock<IProductionWorkplaceRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        return new BdeMasterDataController(
            opRepo.Object, actRepo.Object, termRepo.Object,
            userRepo.Object, wpRepo.Object, currentUser.Object);
    }

    private static BdeMasterDataController WithQuery(BdeMasterDataController ctrl, string queryString)
    {
        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString(queryString);
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return ctrl;
    }

    [Fact]
    public async Task Index_OperatorsTab_ColumnFilter_FiltersAcrossAllRows()
    {
        var operators = new List<BdeOperator>
        {
            new() { Id = 1, PersonnelNumber = "100", FirstName = "Max", LastName = "Huber", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, PersonnelNumber = "200", FirstName = "Anna", LastName = "Maier", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, PersonnelNumber = "300", FirstName = "Tom", LastName = "Bauer", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var ctrl = WithQuery(CreateController(operators: operators), "?colf_name=Maier");

        var result = await ctrl.Index(tab: "operators") as ViewResult;

        var model = ctrl.ViewBag.Operators as List<BdeOperator>;
        model!.Should().HaveCount(1);
        model[0].LastName.Should().Be("Maier");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Index_ActivitiesTab_ColumnFilter_FiltersAcrossAllRows()
    {
        var activities = new List<BdeActivity>
        {
            new() { Id = 1, Code = "RUEST", Name = "Ruesten", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Code = "PROD", Name = "Produktion", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Code = "WART", Name = "Wartung", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var ctrl = WithQuery(CreateController(activities: activities), "?tab=activities&colf_code=PROD");

        await ctrl.Index(tab: "activities");

        var model = ctrl.ViewBag.Activities as List<BdeActivity>;
        model!.Should().HaveCount(1);
        model[0].Code.Should().Be("PROD");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_TerminalsTab_ColumnFilter_FiltersAcrossAllRows()
    {
        var terminals = new List<BdeTerminal>
        {
            new() { Id = 1, UserId = 1, User = new User { Id = 1, Name = "terminal-halle1" }, Description = "Halle 1", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, UserId = 2, User = new User { Id = 2, Name = "terminal-halle2" }, Description = "Halle 2", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var ctrl = WithQuery(CreateController(terminals: terminals), "?tab=terminals&colf_user=halle2");

        await ctrl.Index(tab: "terminals");

        var model = ctrl.ViewBag.Terminals as List<BdeTerminal>;
        model!.Should().HaveCount(1);
        model[0].User.Name.Should().Be("terminal-halle2");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
