using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Filters;

/// <summary>v1.99.35 BF-Auftraege: Rollen-Filter (Bestellen = Lagerbestellung ODER schachtler; Schachtler-Aktionen) und Gate BlechfertigungAktiv.</summary>
public class BfAuftraegeFilterTests
{
    private class Mvc : Controller { }
    private class Api : ControllerBase { }

    private static async Task<(ActionExecutingContext Ctx, bool Weiter)> Run(IAsyncActionFilter filter, object controller)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        if (controller is Controller c) c.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller);
        var weiter = false;
        await filter.OnActionExecutionAsync(ctx, () =>
        {
            weiter = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller));
        });
        return (ctx, weiter);
    }

    private static ICurrentUserService User(bool bestellen = false, bool schachteln = false)
    {
        var u = new Mock<ICurrentUserService>();
        u.Setup(x => x.CanBfBestellenAsync()).ReturnsAsync(bestellen);
        u.Setup(x => x.CanSchachtelnAsync()).ReturnsAsync(schachteln);
        return u.Object;
    }

    [Fact]
    public async Task Bestellen_Erlaubt_Weiter()
    {
        var (ctx, weiter) = await Run(new RequireBfBestellenAccessFilter(User(bestellen: true)), new Mvc());
        weiter.Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Bestellen_Verboten_MvcAccessDenied_ApiForbid()
    {
        var (mvc, w1) = await Run(new RequireBfBestellenAccessFilter(User()), new Mvc());
        w1.Should().BeFalse();
        mvc.Result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AccessDenied");
        var (api, w2) = await Run(new RequireBfBestellenAccessFilter(User()), new Api());
        w2.Should().BeFalse();
        api.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SchachtlerAktion_NurMitSchachtlerRecht()
    {
        (await Run(new RequireSchachtlerAccessFilter(User(bestellen: true)), new Mvc())).Weiter.Should().BeFalse("Besteller allein darf nicht freigeben");
        (await Run(new RequireSchachtlerAccessFilter(User(schachteln: true)), new Mvc())).Weiter.Should().BeTrue();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(null, false)] // Default aus, auch fuer admin
    [InlineData("false", false)]
    [InlineData("1", false)]
    public async Task Gate_NurExaktTrue(string? wert, bool weiter)
    {
        var s = new Mock<IAppSettingRepository>();
        s.Setup(x => x.GetValueAsync(AppSettingKeys.BlechfertigungAktiv)).ReturnsAsync(wert);
        var (ctx, w) = await Run(new RequireBlechfertigungAktivFilter(s.Object), new Mvc());
        w.Should().Be(weiter);
        if (!weiter) ctx.Result.Should().BeOfType<RedirectToActionResult>().Which.ControllerName.Should().Be("Home");
        var (api, _) = await Run(new RequireBlechfertigungAktivFilter(s.Object), new Api());
        if (!weiter) api.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>AK 8: Schachtler-Aktionen per Filter gesperrt (nicht nur ausgeblendet); Stornieren bewusst ohne (Besteller duerfen, Regel im Workflow).</summary>
    [Theory]
    [InlineData("UpdatePosition", true)]
    [InlineData("StreichePosition", true)]
    [InlineData("Freigeben", true)]
    [InlineData("CsvErneutSchreiben", true)]
    [InlineData("TestCsv", true)]
    [InlineData("FertigMelden", true)]
    [InlineData("FertigMeldenListe", true)]
    [InlineData("Stornieren", false)]
    [InlineData("Submit", false)]
    public void SchachtlerAktionen_TragenSchachtlerFilter(string action, bool erwartet) =>
        typeof(IdealAkeWms.Controllers.BfAuftraegeController).GetMethods().Where(m => m.Name == action)
            .Should().NotBeEmpty().And.OnlyContain(m => m.IsDefined(typeof(RequireSchachtlerAccessAttribute), false) == erwartet);
}
