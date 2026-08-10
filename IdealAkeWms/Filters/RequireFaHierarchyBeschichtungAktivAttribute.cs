using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Feature-Gate fuer das IDEAL-Beschichtungsauftrag-Modul (Teil 4): sperrt den kompletten
/// <see cref="Controllers.FaHierarchyBeschichtungController"/>, solange AppSetting
/// <see cref="AppSettingKeys.FaHierarchyBeschichtungAktiv"/> nicht <c>"true"</c> ist.
///
/// <para><b>Invertierte Default-aus-Semantik</b> — bewusst gegenteilig zu
/// <see cref="RequireLagerbestellungAktivAttribute"/>: dort gilt fehlend/"true" =&gt; aktiv
/// (Default-ein aus Bestandsschutz). Hier gilt <b>fehlend/nicht "true" =&gt; inaktiv</b>
/// (Default-aus-Konvention fuer neue Features; eine 1:1-Kopie des Lagerbestellungs-Filters wuerde
/// Beschichtung faelschlich per Default freischalten). Nur der exakte Wert <c>"true"</c>
/// (case-insensitive) schaltet frei — identisch zu
/// <see cref="RequireFaHierarchyKommissionierlistenAktivAttribute"/> (Teil 3).</para>
///
/// MVC-Controller (erbt <see cref="Controller"/>) =&gt; Redirect Home + WarningMessage;
/// API-Controller ([ApiController], erbt nur <see cref="ControllerBase"/>) =&gt; 404.
/// Kumuliert mit dem Rollen-Filter (<see cref="RequireBeschichtungsauftragAccessAttribute"/>) —
/// beide muessen passieren.
/// </summary>
public class RequireFaHierarchyBeschichtungAktivAttribute : TypeFilterAttribute
{
    public RequireFaHierarchyBeschichtungAktivAttribute()
        : base(typeof(RequireFaHierarchyBeschichtungAktivFilter)) { }
}

public class RequireFaHierarchyBeschichtungAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireFaHierarchyBeschichtungAktivFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = await _settings.GetValueAsync(AppSettingKeys.FaHierarchyBeschichtungAktiv);
        // Default-aus: nur der explizite Wert "true" aktiviert das Modul.
        var aktiv = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        if (aktiv)
        {
            await next();
            return;
        }

        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Das IDEAL-Beschichtungsauftrag-Modul ist deaktiviert.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
