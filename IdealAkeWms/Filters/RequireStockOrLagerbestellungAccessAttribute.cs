using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Zugriff fuer admin, stock, stock_keyuser, picking oder lagerbestellung —
/// seit v1.25.0 auch glasbestellung.
/// </summary>
public class RequireStockOrLagerbestellungAccessAttribute : TypeFilterAttribute
{
    public RequireStockOrLagerbestellungAccessAttribute()
        : base(typeof(RequireStockOrLagerbestellungAccessFilter)) { }
}

public class RequireStockOrLagerbestellungAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockOrLagerbestellungAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanAccessLagerbestellungAsync()
            && !await _currentUserService.CanAccessGlasbestellungAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
