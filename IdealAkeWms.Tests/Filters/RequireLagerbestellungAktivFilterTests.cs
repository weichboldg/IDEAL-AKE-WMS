using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

public class RequireLagerbestellungAktivFilterTests
{
    // Dummy-MVC-Controller (erbt Controller -> hat TempData) fuer den Redirect-Zweig.
    private class DummyMvcController : Controller { }
    // Dummy-API-Controller (erbt nur ControllerBase) fuer den 404-Zweig.
    private class DummyApiController : ControllerBase { }

    private static (ActionExecutingContext ctx, ActionExecutionDelegate next, bool[] called) MakeContext(object controller)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object?>(), controller);
        var called = new[] { false };
        ActionExecutionDelegate next = () =>
        {
            called[0] = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller));
        };
        return (ctx, next, called);
    }

    private static RequireLagerbestellungAktivFilter MakeFilter(string? settingValue)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.LagerbestellungAktiv)).ReturnsAsync(settingValue);
        return new RequireLagerbestellungAktivFilter(settings.Object);
    }

    [Fact]
    public async Task Aktiv_true_LaesstDurch()
    {
        var filter = MakeFilter("true");
        var (ctx, next, called) = MakeContext(new DummyMvcController());
        await filter.OnActionExecutionAsync(ctx, next);
        called[0].Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Fehlend_null_DefaultAktiv_LaesstDurch()
    {
        var filter = MakeFilter(null); // Default true
        var (ctx, next, called) = MakeContext(new DummyMvcController());
        await filter.OnActionExecutionAsync(ctx, next);
        called[0].Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Inaktiv_false_Mvc_RedirectHome()
    {
        var mvc = new DummyMvcController
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        var filter = MakeFilter("false");
        var (ctx, next, called) = MakeContext(mvc);
        await filter.OnActionExecutionAsync(ctx, next);
        called[0].Should().BeFalse();
        ctx.Result.Should().BeOfType<RedirectToActionResult>();
        var r = (RedirectToActionResult)ctx.Result!;
        r.ActionName.Should().Be("Index");
        r.ControllerName.Should().Be("Home");
        mvc.TempData["WarningMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Inaktiv_false_Api_NotFound()
    {
        var filter = MakeFilter("false");
        var (ctx, next, called) = MakeContext(new DummyApiController());
        await filter.OnActionExecutionAsync(ctx, next);
        called[0].Should().BeFalse();
        ctx.Result.Should().BeOfType<NotFoundResult>();
    }
}
