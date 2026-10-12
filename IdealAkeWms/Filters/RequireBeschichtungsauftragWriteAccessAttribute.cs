using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Beschichtungsauftrag-SCHREIBrecht (admin ODER Rolle <c>beschichtungsauftrag_schreiben</c>) —
/// Beschichtungsauftrag-Ausbau Teil 4. Action-Level an jeder schreibenden Action, zusaetzlich zum Class-Level-
/// Lesefilter <see cref="RequireBeschichtungsauftragAccessAttribute"/> (Read/Edit-Split, ADR 0006).
/// </summary>
public class RequireBeschichtungsauftragWriteAccessAttribute : TypeFilterAttribute
{
    public RequireBeschichtungsauftragWriteAccessAttribute() : base(typeof(RequireBeschichtungsauftragWriteAccessFilter)) { }
}

public class RequireBeschichtungsauftragWriteAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireBeschichtungsauftragWriteAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.HasBeschichtungsauftragWriteAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
