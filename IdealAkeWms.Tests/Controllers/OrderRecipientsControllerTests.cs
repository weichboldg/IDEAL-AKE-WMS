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

public class OrderRecipientsControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var groups = new List<OrderRecipientGroup>
        {
            new() { Id = 1, Name = "Einkauf", Description = "Bestellungen EK", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "Lagerleitung", Description = "Lager-Themen", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "Produktion", Description = "Fertigung", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var repo = new Mock<IOrderRecipientRepository>();
        repo.Setup(r => r.GetAllGroupsAsync()).ReturnsAsync(groups);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new OrderRecipientsController(repo.Object, currentUser.Object);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_name=Lagerleitung");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<OrderRecipientGroup>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Lagerleitung");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
