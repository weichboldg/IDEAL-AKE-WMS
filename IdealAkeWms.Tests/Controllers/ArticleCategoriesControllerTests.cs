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

public class ArticleCategoriesControllerTests
{
    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var categories = new List<ArticleCategory>
        {
            new() { Id = 1, Name = "Blechtafel_AKE", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 2, Name = "Kleinmaterial", CreatedBy = "t", CreatedByWindows = "t" },
            new() { Id = 3, Name = "Dichtungen", CreatedBy = "t", CreatedByWindows = "t" }
        };
        var repo = new Mock<IArticleCategoryRepository>();
        repo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(categories);
        repo.Setup(r => r.GetArticleCountByCategoryAsync()).ReturnsAsync(new Dictionary<int, int>());
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
        var ctrl = new ArticleCategoriesController(repo.Object, currentUser.Object);

        var httpCtx = new DefaultHttpContext();
        httpCtx.Request.QueryString = new QueryString("?colf_name=Kleinmaterial");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var model = result!.Model as List<ArticleCategory>;
        model!.Should().HaveCount(1);
        model[0].Name.Should().Be("Kleinmaterial");
        var pagination = ctrl.ViewBag.Pagination as PaginationState;
        pagination!.TotalCount.Should().Be(1);
    }
}
