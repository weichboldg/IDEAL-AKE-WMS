using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequirePickingOrTrackingAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrTrackingAccessAttribute() : base(typeof(RequirePickingOrTrackingAccessFilter)) { }
}

public class RequirePickingOrTrackingAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrTrackingAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync() && !await _currentUserService.CanViewTrackingAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
