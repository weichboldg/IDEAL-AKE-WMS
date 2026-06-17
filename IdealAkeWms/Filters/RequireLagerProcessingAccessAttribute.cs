using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Lager-Mitarbeiter-Zugriff fuer Worklist-Sichten (eingehende
/// Lagerbestellungen, Lager-Fehlteile). Bewusst OHNE `picking` — picker
/// arbeiten an der Werkbank, nicht im Lager.
/// </summary>
public class RequireLagerProcessingAccessAttribute : TypeFilterAttribute
{
    public RequireLagerProcessingAccessAttribute() : base(typeof(RequireLagerProcessingAccessFilter)) { }
}

public class RequireLagerProcessingAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireLagerProcessingAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanProcessLagerAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
