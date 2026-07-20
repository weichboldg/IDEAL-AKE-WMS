using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Master-Schalter-Gate (v1.25.0): sperrt das komplette Lagerbestellungs-Modul,
/// wenn AppSetting <c>LagerbestellungAktiv</c> == "false".
/// Default-Semantik: fehlend/"true" => aktiv, nur "false" => gesperrt (Default true,
/// damit bestehende Systeme beim Deploy nicht abgeschaltet werden).
/// MVC-Controller (erbt <see cref="Controller"/>) => Redirect Home + WarningMessage;
/// API-Controller ([ApiController], erbt nur <see cref="ControllerBase"/>) => 404.
/// Kumuliert mit den Rollen-Filtern (beide muessen passieren).
/// </summary>
public class RequireLagerbestellungAktivAttribute : TypeFilterAttribute
{
    public RequireLagerbestellungAktivAttribute() : base(typeof(RequireLagerbestellungAktivFilter)) { }
}

public class RequireLagerbestellungAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireLagerbestellungAktivFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = await _settings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv);
        var aktiv = !string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase);
        if (aktiv)
        {
            await next();
            return;
        }

        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Das Lagerbestellungs-Modul ist deaktiviert.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
