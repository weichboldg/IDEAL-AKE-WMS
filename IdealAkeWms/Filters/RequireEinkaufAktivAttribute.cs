using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// v1.99.20 Feature-Gate (Spec 2026-11-08-einkauf-bestellvorschlaege, ADR 0011): Menue „Einkauf“ und <c>/PurchaseProposals</c> nur bei
/// AppSetting <see cref="AppSettingKeys.EinkaufAktiv"/> = exakt "true" (Default aus, auch fuer admin). Muster
/// <see cref="RequireFaHierarchyKommissionierlistenAktivAttribute"/>: MVC → Redirect Home + WarningMessage, sonst 404.
/// </summary>
public class RequireEinkaufAktivAttribute : TypeFilterAttribute
{
    public RequireEinkaufAktivAttribute() : base(typeof(RequireEinkaufAktivFilter)) { }
}

public class RequireEinkaufAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireEinkaufAktivFilter(IAppSettingRepository settings) => _settings = settings;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.Equals(await _settings.GetValueAsync(AppSettingKeys.EinkaufAktiv), "true", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Einkauf – Bestellvorschläge ist deaktiviert (Einstellung „Einkauf aktiv“).";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
