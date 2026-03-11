using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Filters;

public class RequireAdminAccessAttribute : TypeFilterAttribute
{
    public RequireAdminAccessAttribute() : base(typeof(RequireAdminAccessFilter)) { }
}

public class RequireAdminAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireAdminAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.IsAdminAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
