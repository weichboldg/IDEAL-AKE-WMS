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
/// Controller-Tests fuer Task 3 der Spec 2026-09-25-kommissionierliste-summierung-pdf:
/// sichtbare Druckspalten (<c>ResolveVisibleColumnsAsync</c>, DOM-Parameter mit Server-Rueckfall
/// auf die gespeicherte <see cref="UserViewPreference"/>), Gruppen-Paging der Summiert-Ansicht
/// (<c>TotalGroupCount</c>) und die neuen Actions <c>PrintSummiert</c>/<c>PdfSummiert</c>.
/// PDF-Rendering selbst wird gemockt (<see cref="IRazorViewRenderer"/>/<see cref="IPdfRenderService"/>) —
/// <c>PrintSummiert.cshtml</c> existiert erst nach Task 4, es wird keine echte View gerendert.
/// </summary>
public class FaHierarchyKommissionierListenControllerTests
{
    private static FaHierarchyListBuilder NewBuilder() =>
        new FaHierarchyListBuilder(NullLogger<FaHierarchyListBuilder>.Instance);

    private static FaHierarchyNode Node(int hauptFa, int subFa, string? komm,
        string? artnr = null, int? position = null, decimal sollmenge = 0m,
        string? matchcode = null, string? hauptlagerplatz = null) =>
        new FaHierarchyNode
        {
            HauptFA = hauptFa,
            SubFA = subFa,
            Kommissionieren = komm,
            Artnr = artnr,
            Position = position,
            Sollmenge = sollmenge,
            Matchcode = matchcode,
            Hauptlagerplatz = hauptlagerplatz
        };

    private sealed class Fixture
    {
        public Mock<IUserViewPreferenceRepository> ViewPrefs { get; } = new();
        public Mock<ICurrentUserService> User { get; } = new();
        public Mock<IRazorViewRenderer> ViewRenderer { get; } = new();
        public Mock<IPdfRenderService> PdfRenderService { get; } = new();
        public FaHierarchyKommissionierListenController Controller { get; }

        public Fixture(IEnumerable<FaHierarchyNode> nodes, IEnumerable<FaHierarchyOrderInfo>? infos = null,
            string? queryString = null)
        {
            var nodeRepo = new Mock<IFaHierarchyNodeRepository>();
            nodeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes.ToList());
            var infoRepo = new Mock<IFaHierarchyOrderInfoRepository>();
            infoRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync((infos ?? Enumerable.Empty<FaHierarchyOrderInfo>()).ToList());
            var service = new KommissionierListenService(nodeRepo.Object, infoRepo.Object, NewBuilder());

            User.Setup(x => x.GetDefaultPageSizeAsync()).ReturnsAsync((int?)null);
            User.Setup(x => x.GetCurrentAppUserId()).Returns((int?)null);

            var barcode = new Mock<IBarcodeService>();
            barcode.Setup(b => b.EncodeCode128Base64(It.IsAny<string>())).Returns("barcode");

            var pdfStatus = new PdfRenderStatus(new ConfigurationBuilder().Build(), NullLogger<PdfRenderStatus>.Instance);

            Controller = new FaHierarchyKommissionierListenController(
                service, barcode.Object, User.Object, ViewRenderer.Object, PdfRenderService.Object,
                pdfStatus, ViewPrefs.Object);

            var httpContext = new DefaultHttpContext();
            if (!string.IsNullOrEmpty(queryString))
                httpContext.Request.QueryString = new QueryString(queryString);

            Controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            Controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            Controller.Url = Mock.Of<IUrlHelper>();
        }
    }

    private static UserViewPreference Pref(string viewKey, string json) => new()
    {
        UserId = 7,
        ViewKey = viewKey,
        SettingsJson = json
    };

    // ----- Print: visibleColumns-Parameter direkt verwendet, kein DB-Zugriff -----

    [Fact]
    public async Task Print_WithVisibleColumnsParam_UsesItVerbatim_NoPrefRead()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1") };
        var fx = new Fixture(nodes);

        var result = await fx.Controller.Print(null, "artnr,sollmenge");

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierPrintViewModel;
        vm!.VisibleColumns.Should().BeEquivalentTo(new[] { "artnr", "sollmenge" }, o => o.WithStrictOrdering());
        fx.ViewPrefs.Verify(
            v => v.GetByUserAndViewAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // ----- Print: kein Parameter -> Server-Rueckfall auf gespeicherte Praeferenz -----

    [Fact]
    public async Task Print_WithoutParam_FallsBackToSavedPref()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1") };
        var fx = new Fixture(nodes);
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierListen"))
            .ReturnsAsync(Pref("FaHierarchyKommissionierListen",
                """{"columns":[{"key":"material","visible":false},{"key":"artnr","visible":false}]}"""));

        var result = await fx.Controller.Print(null, null);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierPrintViewModel;
        var expected = ColumnDefinitions.FaHierarchyKommissionierListen.Columns
            .Select(c => c.Key)
            .Where(k => k != "artnr" && k != "material")
            .ToList();
        vm!.VisibleColumns.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Print_WithoutParam_LockedColumnsNeverHidden()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1") };
        var fx = new Fixture(nodes);
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        // matchcode ist Locked (ColumnDefinitions.FaHierarchyKommissionierListen) -> darf trotz
        // visible:false nicht verschwinden.
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierListen"))
            .ReturnsAsync(Pref("FaHierarchyKommissionierListen",
                """{"columns":[{"key":"matchcode","visible":false}]}"""));

        var result = await fx.Controller.Print(null, null);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierPrintViewModel;
        vm!.VisibleColumns.Should().Contain("matchcode");
    }

    [Fact]
    public async Task Print_WithoutParam_NoPref_ReturnsEmpty()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1") };
        var fx = new Fixture(nodes);
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierListen"))
            .ReturnsAsync((UserViewPreference?)null);

        var result = await fx.Controller.Print(null, null);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierPrintViewModel;
        vm!.VisibleColumns.Should().BeEmpty();
    }

    [Fact]
    public async Task Print_WithoutParam_NoUser_ReturnsEmpty()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1") };
        var fx = new Fixture(nodes);
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns((int?)null);

        var result = await fx.Controller.Print(null, null);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierPrintViewModel;
        vm!.VisibleColumns.Should().BeEmpty();
        fx.ViewPrefs.Verify(
            v => v.GetByUserAndViewAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // ----- PrintSummiert liest den eigenen viewKey -----

    [Fact]
    public async Task PrintSummiert_UsesSummiertViewKey()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, 0, "LAGER", artnr: "A1", sollmenge: 1m, matchcode: "MC-1", hauptlagerplatz: "L-1")
        };
        var fx = new Fixture(nodes);
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierSummiert"))
            .ReturnsAsync(Pref("FaHierarchyKommissionierSummiert",
                """{"columns":[{"key":"hauptlagerplatz","visible":false}]}"""));

        var result = await fx.Controller.PrintSummiert(null, null, null, null);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierSummiertPrintViewModel;
        vm!.VisibleColumns.Should().NotContain("hauptlagerplatz");
        fx.ViewPrefs.Verify(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierSummiert"), Times.Once);
        fx.ViewPrefs.Verify(v => v.GetByUserAndViewAsync(7, "FaHierarchyKommissionierListen"), Times.Never);
    }

    // ----- Summiert: Gruppen-Paging zaehlt HauptFA, nicht Aggregatzeilen -----

    [Fact]
    public async Task Summiert_PaginationCountsGroups()
    {
        var nodes = new List<FaHierarchyNode>
        {
            Node(100, 0, "LAGER", artnr: "A1", sollmenge: 1m),
            Node(100, 0, "LAGER", artnr: "A2", sollmenge: 1m),
            Node(200, 0, "LAGER", artnr: "A1", sollmenge: 1m),
            Node(200, 0, "LAGER", artnr: "A2", sollmenge: 1m),
        };
        var fx = new Fixture(nodes);

        var result = await fx.Controller.Summiert(null, null, null, page: 1, pageSize: 100);

        var vm = ((ViewResult)result).Model as FaHierarchyKommissionierSummiertViewModel;
        vm!.Pagination.TotalCount.Should().Be(2);
    }

    // ----- PdfSummiert -----

    [Fact]
    public async Task PdfSummiert_UnknownHauptFa_NotFound()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1", sollmenge: 1m) };
        var fx = new Fixture(nodes);

        var result = await fx.Controller.PdfSummiert(999, null, null, null, null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PdfSummiert_RenderFails_RedirectsToSummiertWithParams()
    {
        var nodes = new List<FaHierarchyNode> { Node(100, 0, "LAGER", artnr: "A1", sollmenge: 1m) };
        var fx = new Fixture(nodes);
        fx.ViewRenderer.Setup(v => v.RenderToStringAsync(
                It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html></html>");
        fx.PdfRenderService.Setup(p => p.RenderHtmlToPdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(PdfRenderException.Failed("boom"));

        // kwVon/kwBis bewusst KEIN gueltiges YYYY-Www-Format (IsoWeekRange.Parse -> null, keine
        // KW-Einschraenkung) -- der Test prueft nur den Rueckweg, nicht die KW-Aufloesung; die Fixture
        // fuehrt keine FaHierarchyOrderInfo, ein gueltiger KW-Filter wuerde HauptFA 100 vorher ausschliessen.
        var result = await fx.Controller.PdfSummiert(100, "LAGER", "kein-gueltiges-format", "auch-nicht", null);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(FaHierarchyKommissionierListenController.Summiert));
        redirect.RouteValues.Should().ContainKey("target").WhoseValue.Should().Be("LAGER");
        redirect.RouteValues.Should().ContainKey("kwVon").WhoseValue.Should().Be("kein-gueltiges-format");
        redirect.RouteValues.Should().ContainKey("kwBis").WhoseValue.Should().Be("auch-nicht");
        fx.Controller.TempData["WarningMessage"].Should().Be(PdfRenderException.FailedMessage);
    }
}
