using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequireFaCompletionAccessAttribute() : base(typeof(RequireFaCompletionAccessFilter)) { }
}

public class RequireFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settings;

    public RequireFaCompletionAccessFilter(ICurrentUserService currentUserService, IAppSettingRepository settings)
    {
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Feature gated: AppSetting FaCompletionAktiv must be true AND user must have the role.
        var aktiv = (await _settings.GetValueAsync(AppSettingKeys.FaCompletionAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!aktiv || !await _currentUserService.CanFaCompletionAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
