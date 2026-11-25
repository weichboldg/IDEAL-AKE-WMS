using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>v1.99.72 Feature-Gate (ADR 0011, kumulativ zu den Rollenfiltern): Fraesen-Pfad pflegen nur bei AppSetting
/// <c>FraesenPfadAktiv</c> = "true". MVC: WarningMessage + Startseite (auch fuer admin); API: 404.</summary>
public class RequireFraesenPfadAktivAttribute : TypeFilterAttribute
{
    public RequireFraesenPfadAktivAttribute() : base(typeof(RequireFraesenPfadAktivFilter)) { }
}

public class RequireFraesenPfadAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireFraesenPfadAktivFilter(IAppSettingRepository settings) => _settings = settings;

    /// <summary>Einzige Lesestelle des Schalters (Filter + Controller-Flags): nur exakt "true" (Gross/Klein egal) = an.</summary>
    public static async Task<bool> IstAktivAsync(IAppSettingRepository settings)
        => string.Equals(await settings.GetValueAsync(AppSettingKeys.FraesenPfadAktiv), "true", StringComparison.OrdinalIgnoreCase);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await IstAktivAsync(_settings))
        {
            await next();
            return;
        }
        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Fräsen-Pfad ist deaktiviert (Einstellung „Fräsen-Pfad anzeigen und pflegen“) – nichts gespeichert.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
