using FluentAssertions;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace IdealAkeWms.Tests.Filters;

/// <summary>Schreibfilter der Beschichtungsauftraege (Teil 4, v1.51.0): ohne Schreibrecht → AccessDenied.</summary>
public class RequireBeschichtungsauftragWriteAccessFilterTests
{
    private static async Task<(ActionExecutingContext Ctx, bool Weiter)> Ausfuehren(bool darf)
    {
        var user = new Mock<ICurrentUserService>();
        user.Setup(x => x.HasBeschichtungsauftragWriteAccessAsync()).ReturnsAsync(darf);
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
        var weiter = false;
        await new RequireBeschichtungsauftragWriteAccessFilter(user.Object).OnActionExecutionAsync(ctx, () =>
        {
            weiter = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
        });
        return (ctx, weiter);
    }

    [Fact]
    public async Task MitSchreibrecht_Weiter()
    {
        var (ctx, weiter) = await Ausfuehren(true);
        weiter.Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task OhneSchreibrecht_AccessDenied()
    {
        var (ctx, weiter) = await Ausfuehren(false);
        weiter.Should().BeFalse();
        var r = ctx.Result.Should().BeOfType<RedirectToActionResult>().Subject;
        (r.ActionName, r.ControllerName).Should().Be(("AccessDenied", "Account"));
    }
}
