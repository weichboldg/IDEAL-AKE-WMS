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
/// Tests fuer den neuen Single-Rolle-Filter aus Phase 4 (v1.13.0).
/// fa_completion (oder admin via CanFaCompletionAsync) → Zugriff auf FaCompletionController.
/// </summary>
public class RequireFaCompletionAccessFilterTests
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
    public async Task FaCompletionRole_AllowsAccess()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequireFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task NoFaCompletionRole_RedirectsToAccessDenied()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequireFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
        var redirect = (RedirectToActionResult)context.Result!;
        redirect.ActionName.Should().Be("AccessDenied");
        redirect.ControllerName.Should().Be("Account");
    }

    [Fact]
    public async Task PickingOnly_WithoutFaCompletion_RedirectsToAccessDenied()
    {
        // Picker hat keine FA-Completion-Berechtigung -> Filter blockt.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanPickAsync()).ReturnsAsync(true);
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(false);

        var filter = new RequireFaCompletionAccessFilter(currentUserService.Object, SettingsMock().Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task FaCompletionRole_ButFeatureInaktiv_RedirectsToAccessDenied()
    {
        // Rolle vorhanden, aber AppSetting FaCompletionAktiv=false -> Zugriff verwehrt.
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.CanFaCompletionAsync()).ReturnsAsync(true);

        var filter = new RequireFaCompletionAccessFilter(currentUserService.Object, SettingsMock(faCompletionAktiv: false).Object);
        var (context, next, called) = CreateFilterContext();

        await filter.OnActionExecutionAsync(context, next);

        called[0].Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>();
    }
}
