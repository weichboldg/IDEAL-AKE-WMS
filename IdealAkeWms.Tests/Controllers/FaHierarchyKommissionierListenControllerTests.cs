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
        public Mock<IAppSettingRepository> Settings { get; } = new();
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
            // v1.99.43: Einstellung „Standard-Ziele“ leer = alle Ziele (die aelteren Tests filtern nicht nach Ziel)
            Settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync("");

            var barcode = new Mock<IBarcodeService>();
            barcode.Setup(b => b.EncodeCode128Base64(It.IsAny<string>())).Returns("barcode");

            var pdfStatus = new PdfRenderStatus(new ConfigurationBuilder().Build(), NullLogger<PdfRenderStatus>.Instance);

            Controller = new FaHierarchyKommissionierListenController(
                service, barcode.Object, User.Object, ViewRenderer.Object, PdfRenderService.Object,
                pdfStatus, ViewPrefs.Object, Settings.Object);

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
        const string query = "?target=LAGER&target=WERK&kwVon=kein-gueltiges-format&kwBis=auch-nicht";
        var fx = new Fixture(nodes, queryString: query);
        fx.ViewRenderer.Setup(v => v.RenderToStringAsync(
                It.IsAny<ControllerContext>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<html></html>");
        fx.PdfRenderService.Setup(p => p.RenderHtmlToPdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(PdfRenderException.Failed("boom"));

        // kwVon/kwBis bewusst KEIN gueltiges YYYY-Www-Format (IsoWeekRange.Parse -> null, keine
        // KW-Einschraenkung) -- der Test prueft nur den Rueckweg, nicht die KW-Aufloesung; die Fixture
        // fuehrt keine FaHierarchyOrderInfo, ein gueltiger KW-Filter wuerde HauptFA 100 vorher ausschliessen.
        var result = await fx.Controller.PdfSummiert(100, new[] { "LAGER", "WERK" }, "kein-gueltiges-format", "auch-nicht", null);

        // v1.99.43: Rueckweg mit der Query der Anfrage (mehrwertiges target bleibt erhalten); Url.Action ist gemockt (null).
        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be(query);
        fx.Controller.TempData["WarningMessage"].Should().Be(PdfRenderException.FailedMessage);

        // View-Name-Pin: ein Tippfehler hier wuerde beim echten Rendern eine InvalidOperationException
        // werfen, die PdfRenderException-Fallback faengt nur Fehler des Edge-Renderers ab.
        fx.ViewRenderer.Verify(v => v.RenderToStringAsync(
            It.IsAny<ControllerContext>(), "PrintSummiert", It.IsAny<object>()), Times.Once);
    }

    // ----- v1.99.43 A1: Zielfilter mehrwertig + Gedaechtnis + Einstellung -----

    private static List<FaHierarchyNode> ZielKnoten() => new()
    {
        Node(100, 0, "K-01", artnr: "A1", position: 1), Node(200, 0, "K-02", artnr: "A2", position: 1),
        Node(300, 0, "K-03", artnr: "A3", position: 1),
    };

    [Fact]
    public async Task Index_MitTargetInQuery_FiltertUndMerkt()
    {
        var fx = new Fixture(ZielKnoten(), queryString: "?target=&target=K-01&target=K-03");
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.User.Setup(x => x.GetDisplayName()).Returns("T");
        fx.User.Setup(x => x.GetWindowsUserName()).Returns("W");

        var vm = (FaHierarchyKommissionierListViewModel)((ViewResult)await fx.Controller.Index(new[] { "", "K-01", "K-03" })).Model!;

        vm.Groups.Select(g => g.HauptFA).Should().Equal(100, 300);
        vm.SelectedTargets.Should().Equal("K-01", "K-03");
        vm.ZielQuery.Should().Be("?target=K-01&target=K-03");
        fx.ViewPrefs.Verify(p => p.SaveAsync(7, FaHierarchyKommissionierListenController.ZieleViewKey,
            """{"ziele":["K-01","K-03"]}""", "T", "W"), Times.Once);
    }

    [Fact]
    public async Task Index_NurLeeresTarget_IstAlleZiele_UndWirdGemerkt()
    {
        var fx = new Fixture(ZielKnoten(), queryString: "?target=");
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);

        var vm = (FaHierarchyKommissionierListViewModel)((ViewResult)await fx.Controller.Index(new[] { "" })).Model!;

        vm.Groups.Should().HaveCount(3);
        fx.ViewPrefs.Verify(p => p.SaveAsync(7, FaHierarchyKommissionierListenController.ZieleViewKey,
            """{"ziele":["*"]}""", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Summiert_OhneTarget_GemerkteZiele_LeitenUmMitZielenUndRestQuery()
    {
        var fx = new Fixture(ZielKnoten(), queryString: "?kwVon=2026-W47");
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);
        fx.ViewPrefs.Setup(v => v.GetByUserAndViewAsync(7, FaHierarchyKommissionierListenController.ZieleViewKey))
            .ReturnsAsync(Pref(FaHierarchyKommissionierListenController.ZieleViewKey, """{"ziele":["K-02"]}"""));

        var result = await fx.Controller.Summiert(null, "2026-W47", null);

        result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("?target=K-02&kwVon=2026-W47");
    }

    [Theory]
    [InlineData("K-03", new[] { 300 })]
    [InlineData("", new[] { 100, 200, 300 })]
    [InlineData(null, new[] { 100, 200 })] // keine Zeile = Default K-01,K-02
    public async Task Index_OhneTargetOhneGedaechtnis_NimmtEinstellung(string? wert, int[] erwartet)
    {
        var fx = new Fixture(ZielKnoten());
        fx.Settings.Setup(s => s.GetValueAsync(AppSettingKeys.FaHierarchyKommissionierlisteVorfilterZiele)).ReturnsAsync(wert);

        var vm = (FaHierarchyKommissionierListViewModel)((ViewResult)await fx.Controller.Index(null)).Model!;

        vm.Groups.Select(g => g.HauptFA).Should().Equal(erwartet);
        fx.ViewPrefs.Verify(p => p.SaveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never, "die Einstellung wird nicht als Benutzerwert gemerkt");
    }

    [Fact]
    public async Task ZieleZuruecksetzen_LoeschtGedaechtnis()
    {
        var fx = new Fixture(ZielKnoten());
        fx.User.Setup(x => x.GetCurrentAppUserId()).Returns(7);

        var r = await fx.Controller.ZieleZuruecksetzen("Summiert");

        fx.ViewPrefs.Verify(p => p.DeleteByUserAndViewAsync(7, FaHierarchyKommissionierListenController.ZieleViewKey), Times.Once);
        r.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Summiert");
        FaHierarchyKommissionierListenController.ZieleViewKey.Length.Should().BeLessThanOrEqualTo(50);
    }

    [Fact]
    public void BuildToggleQuery_TraegtMehrwertigesTarget()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?target=K-01&target=K-02&colf_artnr=x&page=3");
        KommissionierListenService.BuildToggleQuery(ctx.Request).Should().Be("?target=K-01&target=K-02&colf_artnr=x");
    }
}
