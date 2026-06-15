using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Picking- ODER Vorbau- ODER FA-Vervollstaendigungs-Zugriff
/// (admin ODER picking ODER vorbau ODER fa_completion) — Stuecklisten-Druck
/// (PickingController.PrintBom): der Druck-Button ist auch in der read-only
/// Stueckliste der FA-Vervollstaendigung (Rolle fa_completion) sichtbar.
/// </summary>
public class RequirePickingOrVorbauOrFaCompletionAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrVorbauOrFaCompletionAccessAttribute() : base(typeof(RequirePickingOrVorbauOrFaCompletionAccessFilter)) { }
}

public class RequirePickingOrVorbauOrFaCompletionAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrVorbauOrFaCompletionAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await _currentUserService.CanPickAsync()
            || await _currentUserService.HasVorbauAccessAsync()
            || await _currentUserService.CanFaCompletionAsync())
        {
            await next();
            return;
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
