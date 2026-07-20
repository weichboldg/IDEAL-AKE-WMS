using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Lese-Zugriff auf Lagerbestand (Bestände + Bewegungshistorie).
/// admin/stock/stock_keyuser/picking impliziert es; zusaetzlich die reine
/// Lese-Rolle stock_read (v1.25.0). Schreib-Actions verschaerfen mit
/// [RequireStockAccess]/[RequireStockKeyUserAccess].
/// </summary>
public class RequireStockReadAccessAttribute : TypeFilterAttribute
{
    public RequireStockReadAccessAttribute() : base(typeof(RequireStockReadAccessFilter)) { }
}

public class RequireStockReadAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockReadAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockReadAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
