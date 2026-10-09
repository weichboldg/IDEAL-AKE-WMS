using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Controllers;

/// <summary>
/// Controller-Tests der Summiert-Ansicht des Beschichtungsauftrags (Spec
/// 2026-10-06-beschichtungsauftrag-ausbau-teil-1, v1.48.0): Gruppen-Paging, eigener viewKey fuer die
/// Druckspalten, PDF je HauptFA (Dateiname, NotFound, Fehler-Rueckweg auf Summiert). Rendering gemockt.
/// </summary>
public class FaHierarchyBeschichtungControllerTests
{
    private static FaHierarchyNode Node(int hauptFa, string artnr, decimal sollmenge = 1m) => new()
    {
        HauptFA = hauptFa, SubFA = 0, Beschichtet = true, Artnr = artnr, Sollmenge = sollmenge
    };

    private sealed class Fixture
    {
        public Mock<IUserViewPreferenceRepository> ViewPrefs { get; } = new();
        public Mock<ICurrentUserService> User { get; } = new();
        public Mock<IRazorViewRenderer> ViewRenderer { get; } = new();
        public Mock<IPdfRenderService> PdfRenderService { get; } = new();
        public FaHierarchyBeschichtungController Controller { get; }

        public Fixture(IEnumerable<FaHierarchyNode> nodes, string? queryString = null)
        {
            var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
            nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes.ToList());
            var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
            infoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FaHierarchyOrderInfo>());
            var service = new BeschichtungsauftragService(nodeRepo.Object, infoRepo.Object,
                new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance),
                NullLogger<BeschichtungsauftragService>.Instance);

            User.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
            User.Setup(x => x.GetCurrentAppUserId()).Returns((int?)null);
            var barcode = new Mock<IBarcodeService>();
            barcode.Setup(b => b.EncodeCode128Base64(It.IsAny<string>())).Returns("barcode");
            var pdfStatus = new PdfRenderStatus(new ConfigurationBuilder().Build(), NullLogger<PdfRenderStatus>.Instance);

            Controller = new FaHierarchyBeschichtungController(
                service, barcode.Object, User.Object, ViewRenderer.Object, PdfRenderService.Object,
                pdfStatus, ViewPrefs.Object);

            var httpContext = new DefaultHttpContext();
            if (!string.IsNullOrEmpty(queryString))
                httpContext.Request.QueryString = new QueryString(queryString);
            Controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            Controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            Controller.Url = Mock.Of<IUrlHelper>();
        }
    }

    [Fact]
    public async Task Summiert_PaginationCountsGroups()
    {
        var fx = new Fixture(new[] { Node(100, "A1"), Node(100, "A2"), Node(200, "A1") });

        var result = await fx.Controller.Summiert(page: 1, pageSize: 100);

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaHierarchyBeschichtungSummiertViewModel>().Subject;
        vm.Pagination.TotalCount.Should().Be(2);
        vm.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task PrintSummiert_UsesSummiertViewKey_ForSavedColumns()
    {
        var fx = new Fixture(new[] { Node(100, "A1") });
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, "FaHierarchyBeschichtungSummiert"))
            .ReturnsAsync(new UserViewPreference
            {
                UserId = 7, ViewKey = "FaHierarchyBeschichtungSummiert",
                SettingsJson = """{"columns":[{"key":"tiefe","visible":false}]}"""
            });

        var result = await fx.Controller.PrintSummiert(null);

        var vm = ((ViewResult)result).Model.Should().BeOfType<FaHierarchyBeschichtungSummiertPrintViewModel>().Subject;
        vm.VisibleColumns.Should().NotContain("tiefe").And.Contain("matchcode");
        fx.ViewPrefs.Verify(v => v.GetByUserAndViewAsync(7, "FaHierarchyBeschichtung"), Times.Never);
    }

    [Fact]
    public async Task PdfSummiert_UnknownOrFilteredOutHauptFa_NotFound()
    {
        var fx = new Fixture(new[] { Node(100, "A1"), Node(200, "B1") }, "?colf_hauptfa=100");

        var result = await fx.Controller.PdfSummiert(200, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PdfSummiert_Success_FileNameStartsWithBeschichtungsauftrag()
    {
        var fx = new Fixture(new[] { Node(100, "A1") });
        fx.ViewRenderer.Setup(v => v.RenderToStringAsync(It.IsAny<ControllerContext>(), "PrintSummiert", It.IsAny<object>()))
            .ReturnsAsync("<html></html>");
        fx.PdfRenderService.Setup(p => p.RenderHtmlToPdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1 });

        var result = await fx.Controller.PdfSummiert(100, null);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().MatchRegex(@"^Beschichtungsauftrag_100_\d{8}-\d{4}\.pdf$");
    }

    [Fact]
    public async Task PdfSummiert_RenderFails_RedirectsToSummiertWithWarning()
    {
        var fx = new Fixture(new[] { Node(100, "A1") });
        fx.ViewRenderer.Setup(v => v.RenderToStringAsync(It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html></html>");
        fx.PdfRenderService.Setup(p => p.RenderHtmlToPdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(PdfRenderException.Failed("boom"));

        var result = await fx.Controller.PdfSummiert(100, null);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(FaHierarchyBeschichtungController.Summiert));
        fx.Controller.TempData["WarningMessage"].Should().Be(PdfRenderException.FailedMessage);
        fx.ViewRenderer.Verify(v => v.RenderToStringAsync(It.IsAny<ControllerContext>(), "PrintSummiert", It.IsAny<object>()), Times.Once);
    }
}
