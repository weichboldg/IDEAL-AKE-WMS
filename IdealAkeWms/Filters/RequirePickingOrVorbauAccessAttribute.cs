using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Picking- ODER Vorbau-Zugriff (admin ODER picking ODER vorbau) —
/// Stuecklisten-Druck (PickingController.PrintBom): der Druck-Button ist auch
/// in der read-only Stueckliste der FA-Abarbeitungsliste sichtbar
/// (v1.22.0, Spec §7 "Druck bleibt").
/// </summary>
public class RequirePickingOrVorbauAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrVorbauAccessAttribute() : base(typeof(RequirePickingOrVorbauAccessFilter)) { }
}

public class RequirePickingOrVorbauAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrVorbauAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await _currentUserService.CanPickAsync() || await _currentUserService.HasVorbauAccessAsync())
        {
            await next();
            return;
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
