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
/// Tests fuer den Composite-Filter (PrintBom):
/// admin / picking / vorbau / fa_completion → Zugriff auf den Stuecklisten-Druck.
/// Der Druck-Button ist auch in der read-only Stueckliste der
/// FA-Vervollstaendigung (Rolle fa_completion) sichtbar.
/// </summary>
public class RequirePickingOrVorbauOrFaCompletionAccessFilterTests
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
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrVorbauOrFaCompletionAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task FaCompletionOnly_AllowsAccess_KeyAssertion()
    {
        // fa_completion-User ohne picking/vorbau darf den Stuecklisten-Druck aus
        // der read-only Stueckliste der FA-Vervollstaendigung oeffnen.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrVorbauOrFaCompletionAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task VorbauOnly_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(true);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrVorbauOrFaCompletionAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NoRelevantRole_RedirectsToAccessDenied()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.HasVorbauAccessAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrVorbauOrFaCompletionAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }
}
