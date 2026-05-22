using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequirePickingOrFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrFaCompletionAccessAttribute() : base(typeof(RequirePickingOrFaCompletionAccessFilter)) { }
}

public class RequirePickingOrFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settings;

    public RequirePickingOrFaCompletionAccessFilter(ICurrentUserService currentUserService, IAppSettingRepository settings)
    {
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Picker passes always. FA-Completion only when feature is aktiv.
        if (await _currentUserService.CanPickAsync())
        {
            await next();
            return;
        }

        var aktiv = (await _settings.GetValueAsync(AppSettingKeys.FaCompletionAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (aktiv && await _currentUserService.CanFaCompletionAsync())
        {
            await next();
            return;
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
