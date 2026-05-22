using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace IdealAkeWms.Tests.Filters;

/// <summary>
/// Tests fuer den 2-Rollen-Composite-Filter aus Phase 4 Task 6 (v1.13.0).
/// Picking ODER FaCompletion → Zugriff auf AssemblyGroupsApiController.ToggleApplicable.
/// FaCompletion-Pfad zusaetzlich von <c>FaCompletionAktiv</c>-AppSetting gegated.
/// </summary>
public class RequirePickingOrFaCompletionAccessFilterTests
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

    private static Mock<IAppSettingRepository> SettingsMock(bool faCompletionAktiv = true)
    {
        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(AppSettingKeys.FaCompletionAktiv))
            .ReturnsAsync(faCompletionAktiv ? "true" : "false");
        return settings;
    }

    [Fact]
    public async Task PickingOnly_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task FaCompletionOnly_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task BothRoles_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NoRole_RedirectsToAccessDenied()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }

    [Fact]
    public async Task FaCompletionOnly_ButFeatureInaktiv_RedirectsToAccessDenied()
    {
        // FA-Completion-User OHNE Picking, Feature ausgeschaltet -> kein Zugriff.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(false);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock(faCompletionAktiv: false).Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task Picker_FeatureInaktiv_StillAllowsAccess()
    {
        // Picker hat unabhaengig vom FA-Completion-Setting Zugriff.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);

        var filter = new RequirePickingOrFaCompletionAccessFilter(currentUserService.Object, SettingsMock(faCompletionAktiv: false).Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }
}
