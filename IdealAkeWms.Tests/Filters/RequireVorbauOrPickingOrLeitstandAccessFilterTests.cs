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

/// <summary>
/// Tests fuer den Composite-Filter des Erledigt-Toggle-Endpoints
/// (<c>/api/fa-work-steps/toggle-completed</c>): admin / vorbau / picking / leitstand duerfen
/// togglen. Der Endpoint wird von der FA-Abarbeitungsliste (vorbau) UND den VK-VA-Spalten des
/// Leitstands Kommissionierung (picking/leitstand) genutzt.
/// </summary>
public class RequireVorbauOrPickingOrLeitstandAccessFilterTests
{
    private static (ActionExecutingContext context, ActionExecutionDelegate next, bool[] called) CreateFilterContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        var actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null!);

        var called = new[] { false };
        ActionExecutionDelegate next = () =>
        {
            called[0] = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
        };

        return (actionExecutingContext, next, called);
    }

    [Fact]
    public async Task Vorbau_AllowsAccess()
    {
        var svc = new Mock<ICurrentUserService>();
        svc.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(true);
        svc.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanManagePickingReleaseAsync()).ReturnsAsync(false);

        var filter = new RequireVorbauOrPickingOrLeitstandAccessFilter(svc.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task Picker_AllowsAccess()
    {
        var svc = new Mock<ICurrentUserService>();
        svc.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanPickAsync()).ReturnsAsync(true);
        svc.Setup(x => x.CanManagePickingReleaseAsync()).ReturnsAsync(false);

        var filter = new RequireVorbauOrPickingOrLeitstandAccessFilter(svc.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task LeitstandOnly_AllowsAccess()
    {
        var svc = new Mock<ICurrentUserService>();
        svc.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanManagePickingReleaseAsync()).ReturnsAsync(true);

        var filter = new RequireVorbauOrPickingOrLeitstandAccessFilter(svc.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NoRelevantRole_RedirectsToAccessDenied()
    {
        var svc = new Mock<ICurrentUserService>();
        svc.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        svc.Setup(x => x.CanManagePickingReleaseAsync()).ReturnsAsync(false);

        var filter = new RequireVorbauOrPickingOrLeitstandAccessFilter(svc.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }
}
