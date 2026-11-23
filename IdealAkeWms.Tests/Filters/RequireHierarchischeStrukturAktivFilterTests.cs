using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services.HierarchischeStruktur;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

/// <summary>v1.99.61: FA-Struktur nur bei Master ProduktionsauftragHierarchisch = true (auch Direktaufruf).</summary>
public class RequireHierarchischeStrukturAktivFilterTests
{
    private class Mvc : Controller { }
    private class Api : ControllerBase { }

    private static async Task<(ActionExecutingContext Ctx, bool Weiter)> Run(string? wert, object controller)
    {
        var s = new Mock<IServiceSettingRepository>();
        s.Setup(x => x.GetValueAsync(HierarchischeStrukturKeys.Master)).ReturnsAsync(wert);
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        if (controller is Controller c) c.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller);
        var weiter = false;
        await new RequireHierarchischeStrukturAktivFilter(s.Object).OnActionExecutionAsync(ctx, () =>
        {
            weiter = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller));
        });
        return (ctx, weiter);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData(" TRUE ", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public async Task Mvc_NurBeiMasterTrue_SonstHomeMitWarnung(string? wert, bool weiter)
    {
        var (ctx, w) = await Run(wert, new Mvc());
        w.Should().Be(weiter);
        if (!weiter)
        {
            ctx.Result.Should().BeOfType<RedirectToActionResult>().Which.ControllerName.Should().Be("Home");
            ((Controller)ctx.Controller).TempData["WarningMessage"].Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Api_MasterAus_404()
        => (await Run("false", new Api())).Ctx.Result.Should().BeOfType<NotFoundResult>();

    [Fact]
    public void FaHierarchyController_TraegtGateUndRollenFilter()
    {
        var a = typeof(IdealAkeWms.Controllers.FaHierarchyController).GetCustomAttributes(true);
        a.Should().ContainSingle(x => x is RequireHierarchischeStrukturAktivAttribute);
        a.Should().ContainSingle(x => x is RequirePickingOrTrackingOrLeitstandAccessAttribute);
    }
}
