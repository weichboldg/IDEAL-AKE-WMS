using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege): Einkauf → Bestellvorschlaege nur fuer admin oder Rolle <c>einkauf</c>.
/// Kumulativ zum Gate <see cref="RequireEinkaufAktivAttribute"/>.</summary>
public class RequireEinkaufAccessAttribute : TypeFilterAttribute
{
    public RequireEinkaufAccessAttribute() : base(typeof(RequireEinkaufAccessFilter)) { }
}

public class RequireEinkaufAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireEinkaufAccessFilter(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessEinkaufAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
