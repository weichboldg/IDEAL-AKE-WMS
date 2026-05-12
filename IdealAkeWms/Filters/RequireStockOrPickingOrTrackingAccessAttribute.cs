using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequireStockOrPickingOrTrackingAccessAttribute : TypeFilterAttribute
{
    public RequireStockOrPickingOrTrackingAccessAttribute()
        : base(typeof(RequireStockOrPickingOrTrackingAccessFilter)) { }
}

public class RequireStockOrPickingOrTrackingAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockOrPickingOrTrackingAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanViewTrackingAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
