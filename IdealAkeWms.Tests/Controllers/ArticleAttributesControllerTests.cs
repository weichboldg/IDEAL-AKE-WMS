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

public class ArticleAttributesControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var definitions = new List<ArticleAttributeDefinition>
        {
            new() { Id = 1, Name = "Laserschnitt", AttributeType = AttributeType.Boolean, SortOrder = 1, IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "Oberflaeche", AttributeType = AttributeType.Dropdown, SortOrder = 2, IsActive = true, CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "Kantung", AttributeType = AttributeType.Boolean, SortOrder = 3, IsActive = false, CreatedBy = "t", CreatedByWindows = "t" }
        };
        var repo = new Mock<IArticleAttributeRepository>();
        repo.Setup(r => r.GetAllDefinitionsAsync()).ReturnsAsync(definitions);
        repo.Setup(r => r.DefinitionHasValuesAsync(It.IsAny<int>())).ReturnsAsync(false);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new ArticleAttributesController(repo.Object, currentUser.Object);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_name=Oberflaeche");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<ArticleAttributeDefinition>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Oberflaeche");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
