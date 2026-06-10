using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class RolesControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Key = "admin", Name = "Administrator", SortOrder = 1, IsSystem = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Key = "picking", Name = "Kommissionierung", SortOrder = 2, IsSystem = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Key = "stock", Name = "Lager", SortOrder = 3, IsSystem = true, CreatedBy = "t", CreatedByWindows = "t" }
        };
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(roles);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new RolesController(roleRepo.Object, currentUser.Object);

        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpCtx.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_key=picking");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<RoleEditViewModel>;
        model!.Should().HaveCount(1);
        model[0].Key.Should().Be("picking");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
