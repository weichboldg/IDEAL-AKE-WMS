using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class WorkstationsControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var workstations = new List<Workstation>
        {
            new() { Id = 1, Name = "WB-Montage", Location = "Halle 1", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "WB-Verpackung", Location = "Halle 2", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "WB-Versand", Location = "Halle 3", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var workstationRepo = new Mock<IWorkstationRepository>();
        workstationRepo.Setup(r => r.GetAllWithUsersAsync()).ReturnsAsync(workstations);
        var userRepo = new Mock<IUserRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new WorkstationsController(workstationRepo.Object, userRepo.Object, currentUser.Object);

        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpCtx.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_name=Verpackung");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<Workstation>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("WB-Verpackung");
        var pagination = ctrl.ViewBag.Pagination as IdealAkeWms.Models.ViewModels.PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
