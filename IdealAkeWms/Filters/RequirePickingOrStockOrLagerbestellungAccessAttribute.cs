using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Zugriff fuer admin, picking, stock, stock_keyuser oder lagerbestellung —
/// seit v1.25.0 auch glasbestellung.
/// </summary>
public class RequirePickingOrStockOrLagerbestellungAccessAttribute : TypeFilterAttribute
{
    public RequirePickingOrStockOrLagerbestellungAccessAttribute()
        : base(typeof(RequirePickingOrStockOrLagerbestellungAccessFilter)) { }
}

public class RequirePickingOrStockOrLagerbestellungAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequirePickingOrStockOrLagerbestellungAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync()
            && !await _currentUserService.CanAccessGlasbestellungAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
