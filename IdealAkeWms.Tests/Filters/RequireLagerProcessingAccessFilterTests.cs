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
/// Tests fuer den neuen Lager-Filter aus v1.20.0.
/// admin / stock / stock_keyuser → Zugriff auf Lager-Worklists.
/// Picker werden EXPLIZIT blockiert (Key-Assertion).
/// </summary>
public class RequireLagerProcessingAccessFilterTests
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
    public async Task HasLagerProcessingAccess_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanProcessLagerAsync()).ReturnsAsync(true);

        var filter = new RequireLagerProcessingAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NoLagerProcessingAccess_RedirectsToAccessDenied()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanProcessLagerAsync()).ReturnsAsync(false);

        var filter = new RequireLagerProcessingAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }

    [Fact]
    public async Task PickerOnly_RedirectsToAccessDenied_KeyAssertion()
    {
        // picker arbeitet an der Werkbank, nicht im Lager → Lager-Worklist gesperrt.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanProcessLagerAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);

        var filter = new RequireLagerProcessingAccessFilter(currentUserService.Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
    }
}
