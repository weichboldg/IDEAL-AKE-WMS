using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Beschichtungsauftrag-Zugriff (admin ODER Rolle <c>beschichtungsauftrag</c>) — IDEAL
/// Teil 4: Beschichtungsauftrag-Liste + Bildschirmdruck einsehen. Reine Read-Ebene (keine
/// Bearbeitung), daher Class-Level-Filter ohne Edit-Split (ADR 0006). Kumuliert mit dem
/// Feature-Gate <see cref="RequireFaHierarchyBeschichtungAktivAttribute"/> — beide muessen passieren.
/// </summary>
public class RequireBeschichtungsauftragAccessAttribute : TypeFilterAttribute
{
    public RequireBeschichtungsauftragAccessAttribute() : base(typeof(RequireBeschichtungsauftragAccessFilter)) { }
}

public class RequireBeschichtungsauftragAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireBeschichtungsauftragAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.HasBeschichtungsauftragAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
