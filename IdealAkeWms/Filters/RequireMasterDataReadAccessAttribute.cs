using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

/// <summary>
/// Erfordert Read-Zugriff auf Stammdaten (Listing-Sichten). Edit-Rolle
/// `masterdata` impliziert Read. Wird typisch class-level appliziert,
/// Action-Level [RequireMasterDataAccess] verschaerft fuer Edits.
/// </summary>
public class RequireMasterDataReadAccessAttribute : TypeFilterAttribute
{
    public RequireMasterDataReadAccessAttribute() : base(typeof(RequireMasterDataReadAccessFilter)) { }
}

public class RequireMasterDataReadAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireMasterDataReadAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.HasMasterDataReadAccessAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
