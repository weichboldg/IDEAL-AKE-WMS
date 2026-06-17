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
/// Tests fuer den Composite-Filter aus v1.22.0 (PrintBom):
/// admin / picking / vorbau → Zugriff auf den Stuecklisten-Druck.
/// Der Druck-Button ist auch in der read-only Stueckliste der
/// FA-Abarbeitungsliste sichtbar (Spec §7 "Druck bleibt").
/// </summary>
public class RequirePickingOrVorbauAccessFilterTests
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
    public async Task Picker_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrVorbauAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task VorbauOnly_AllowsAccess_KeyAssertion()
    {
        // vorbau-User ohne picking darf den Stuecklisten-Druck aus der
        // read-only Stueckliste oeffnen (v1.22.0-Fix).
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrVorbauAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NeitherPickingNorVorbau_RedirectsToAccessDenied()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrVorbauAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }
}
