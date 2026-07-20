using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Vorbau- ODER Picking- ODER Leitstand-Zugriff
/// (admin ODER vorbau ODER picking ODER leitstand) — der Erledigt-Status (Status, 3-Wert)
/// der Vorbau-Arbeitsgaenge wird sowohl in der FA-Abarbeitungsliste (vorbau) als auch in
/// den VK-VA-Spalten des Leitstands Kommissionierung (picking/leitstand) gesetzt (seit v1.22.0, 3-State seit v1.24.0).
/// </summary>
public class RequireVorbauOrPickingOrLeitstandAccessAttribute : TypeFilterAttribute
{
    public RequireVorbauOrPickingOrLeitstandAccessAttribute() : base(typeof(RequireVorbauOrPickingOrLeitstandAccessFilter)) { }
}

public class RequireVorbauOrPickingOrLeitstandAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireVorbauOrPickingOrLeitstandAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await _currentUserService.HasVorbauAccessAsync()
            || await _currentUserService.CanPickAsync()
            || await _currentUserService.CanManagePickingReleaseAsync())
        {
            await next();
            return;
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
