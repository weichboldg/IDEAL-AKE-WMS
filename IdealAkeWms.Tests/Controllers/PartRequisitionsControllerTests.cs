using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

public class PartRequisitionsControllerTests
{
    private static (PartRequisitionsController ctrl, ApplicationDbContext ctx) Setup()
    {
        var ctx = TestDbContextFactory.Create();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
        currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");
        currentUser.Setup(s => s.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);

        var requisitions = new PartRequisitionRepository(ctx);

        var ctrl = new PartRequisitionsController(requisitions, currentUser.Object);
        ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return (ctrl, ctx);
    }

    private static PartRequisition MakeRequisition(int productionOrderId, string articleNumber) => new()
    {
        ProductionOrderId = productionOrderId,
        ArticleNumber = articleNumber,
        ArticleDescription = "Desc " + articleNumber,
        Quantity = 1,
        Unit = "Stk",
        Status = PartRequisitionStatus.Offen,
        Priority = PartRequisitionPriority.Normal,
        CreatedAt = DateTime.Now,
        CreatedBy = "tester",
        CreatedByWindows = "t"
    };

    [Fact]
    public async Task Index_ColumnFilter_FiltersAcrossAllRows()
    {
        var (ctrl, ctx) = Setup();
        var po = new ProductionOrder { OrderNumber = "FA-1000", Customer = "Kunde X", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.ProductionOrders.Add(po);
        ctx.SaveChanges();

        ctx.PartRequisitions.AddRange(
            MakeRequisition(po.Id, "ART-A1"),
            MakeRequisition(po.Id, "ART-B2"),
            MakeRequisition(po.Id, "ART-C3"));
        await ctx.SaveChangesAsync();

        var httpCtx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpCtx.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?colf_resource-number=A1");
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        var result = await ctrl.Index() as ViewResult;

        var vm = result!.Model as PartRequisitionIndexViewModel;
        vm!.Items.Should().HaveCount(1);
        vm.Items[0].ArticleNumber.Should().Be("ART-A1");
        vm.Pagination.TotalCount.Should().Be(1);
    }
}
