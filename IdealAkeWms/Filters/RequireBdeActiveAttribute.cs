using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequireBdeActiveAttribute : TypeFilterAttribute
{
    public RequireBdeActiveAttribute() : base(typeof(RequireBdeActiveFilter)) { }
}

public class RequireBdeActiveFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireBdeActiveFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var bdeAktiv = (await _settings.GetValueAsync(AppSettingKeys.BdeAktiv))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (!bdeAktiv)
        {
            if (context.Controller is Controller mvcController)
            {
                mvcController.TempData["WarningMessage"] = "BDE ist nicht aktiviert.";
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            else
            {
                // API-Controller
                context.Result = new NotFoundResult();
            }
            return;
        }

        await next();
    }
}
