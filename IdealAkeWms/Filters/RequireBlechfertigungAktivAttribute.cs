using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>v1.99.35 Feature-Gate (ADR 0011, kumulativ zu den Rollenfiltern): BF-Auftraege nur bei AppSetting <c>BlechfertigungAktiv</c> = "true".
/// MVC: WarningMessage + Startseite (auch fuer admin); API: 404.</summary>
public class RequireBlechfertigungAktivAttribute : TypeFilterAttribute
{
    public RequireBlechfertigungAktivAttribute() : base(typeof(RequireBlechfertigungAktivFilter)) { }
}

public class RequireBlechfertigungAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireBlechfertigungAktivFilter(IAppSettingRepository settings) => _settings = settings;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.Equals(await _settings.GetValueAsync(AppSettingKeys.BlechfertigungAktiv), "true", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Blechfertigung ist deaktiviert (Einstellung „Blechfertigung aktiv“).";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
