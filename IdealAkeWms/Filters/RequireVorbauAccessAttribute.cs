using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Vorbau-Zugriff (admin ODER vorbau) — FA-Abarbeitungsliste:
/// Vorbau-Arbeitsgaenge einsehen und abhaken (v1.22.0).
/// </summary>
public class RequireVorbauAccessAttribute : TypeFilterAttribute
{
    public RequireVorbauAccessAttribute() : base(typeof(RequireVorbauAccessFilter)) { }
}

public class RequireVorbauAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireVorbauAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.HasVorbauAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
