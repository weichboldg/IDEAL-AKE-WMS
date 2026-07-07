using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

// Zugriff: admin, picking ODER tracking ODER leitstand — seit v1.25.0 auch vorbau
// (FA-Liste + read-only Stueckliste-Button verlinkt fuer vorbau auf FaWorklist/Bom).
// Filter-NAME unveraendert.
public class RequirePickingOrTrackingOrLeitstandAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrTrackingOrLeitstandAccessAttribute() : base(typeof(RequirePickingOrTrackingOrLeitstandAccessFilter)) { }
}

public class RequirePickingOrTrackingOrLeitstandAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrTrackingOrLeitstandAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanViewTrackingAsync()
            && !await _currentUserService.CanManagePickingReleaseAsync()
            && !await _currentUserService.HasVorbauAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
