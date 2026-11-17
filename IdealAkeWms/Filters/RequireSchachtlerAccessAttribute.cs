using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): Schachtler-Aktionen der BF-Auftraege (admin, schachtler).
/// Action-Level (ADR 0006 Read/Edit-Split), zusaetzlich zu <see cref="RequireBfBestellenAccessAttribute"/> und <see cref="RequireBlechfertigungAktivAttribute"/>.</summary>
public class RequireSchachtlerAccessAttribute : TypeFilterAttribute
{
    public RequireSchachtlerAccessAttribute() : base(typeof(RequireSchachtlerAccessFilter)) { }
}

public class RequireSchachtlerAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireSchachtlerAccessFilter(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanSchachtelnAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}

/// <summary>v1.99.35: Lesen und Bestellen der BF-Auftraege = Rollen der Lagerbestellung ODER schachtler (Freigabe-Antwort 6). Class-Level.</summary>
public class RequireBfBestellenAccessAttribute : TypeFilterAttribute
{
    public RequireBfBestellenAccessAttribute() : base(typeof(RequireBfBestellenAccessFilter)) { }
}

public class RequireBfBestellenAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireBfBestellenAccessFilter(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanBfBestellenAsync())
        {
            context.Result = context.Controller is Controller
                ? new RedirectToActionResult("AccessDenied", "Account", null)
                : new ForbidResult();
            return;
        }
        await next();
    }
}
