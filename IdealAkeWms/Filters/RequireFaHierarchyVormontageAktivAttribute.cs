using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Feature-Gate fuer das IDEAL-Vormontage-Modul (Teil 5): sperrt den kompletten
/// <see cref="Controllers.FaHierarchyVormontageController"/> (beide Sichten — Einzelne Teile UND
/// Summiert), solange AppSetting <see cref="AppSettingKeys.FaHierarchyVormontageAktiv"/> nicht
/// <c>"true"</c> ist.
///
/// <para><b>Invertierte Default-aus-Semantik</b> — 1:1 nach dem Muster von
/// <see cref="RequireFaHierarchyKommissionierlistenAktivAttribute"/> (Teil 3) bzw.
/// <see cref="RequireFaHierarchyBeschichtungAktivAttribute"/> (Teil 4): <b>fehlend/nicht "true"
/// =&gt; inaktiv</b> (Default-aus-Konvention fuer neue Features, gegenteilig zu
/// <see cref="RequireLagerbestellungAktivAttribute"/>). Nur der exakte Wert <c>"true"</c>
/// (case-insensitive) schaltet frei.</para>
///
/// MVC-Controller (erbt <see cref="Controller"/>) =&gt; Redirect Home + WarningMessage;
/// API-Controller ([ApiController], erbt nur <see cref="ControllerBase"/>) =&gt; 404.
/// Kumuliert mit dem Rollen-Filter (<see cref="RequireVorbauAccessAttribute"/>) — beide muessen
/// passieren.
/// </summary>
public class RequireFaHierarchyVormontageAktivAttribute : TypeFilterAttribute
{
    public RequireFaHierarchyVormontageAktivAttribute()
        : base(typeof(RequireFaHierarchyVormontageAktivFilter)) { }
}

public class RequireFaHierarchyVormontageAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireFaHierarchyVormontageAktivFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = await _settings.GetValueAsync(AppSettingKeys.FaHierarchyVormontageAktiv);
        // Default-aus: nur der explizite Wert "true" aktiviert das Modul.
        var aktiv = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        if (aktiv)
        {
            await next();
            return;
        }

        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Das IDEAL-Vormontage-Modul ist deaktiviert.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
