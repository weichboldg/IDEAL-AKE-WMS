using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// Feature-Gate fuer das IDEAL-Kommissionierlisten-Modul (Teil 3): sperrt den kompletten
/// <see cref="Controllers.FaHierarchyKommissionierListenController"/>, solange AppSetting
/// <see cref="AppSettingKeys.FaHierarchyKommissionierlistenAktiv"/> nicht <c>"true"</c> ist.
///
/// <para><b>Invertierte Default-aus-Semantik</b> — bewusst gegenteilig zu
/// <see cref="RequireLagerbestellungAktivAttribute"/>: dort gilt fehlend/"true" =&gt; aktiv
/// (Default-ein aus Bestandsschutz). Hier gilt <b>fehlend/nicht "true" =&gt; inaktiv</b>
/// (Default-aus-Konvention fuer neue Features). Nur der exakte Wert <c>"true"</c>
/// (case-insensitive) schaltet frei.</para>
///
/// MVC-Controller (erbt <see cref="Controller"/>) =&gt; Redirect Home + WarningMessage;
/// API-Controller ([ApiController], erbt nur <see cref="ControllerBase"/>) =&gt; 404.
/// Kumuliert mit dem Rollen-Filter (<see cref="RequireLagerProcessingAccessAttribute"/>) —
/// beide muessen passieren.
///
/// <para><b>Erweiterungs-Naht Teil 4/5:</b> Fuer Beschichtung/Vormontage entsteht je ein
/// analoges Attribut (<c>FaHierarchyBeschichtungAktiv</c> / <c>FaHierarchyVormontageAktiv</c>)
/// nach exakt diesem Muster — nur der AppSetting-Key und die Meldung wechseln.</para>
/// </summary>
public class RequireFaHierarchyKommissionierlistenAktivAttribute : TypeFilterAttribute
{
    public RequireFaHierarchyKommissionierlistenAktivAttribute()
        : base(typeof(RequireFaHierarchyKommissionierlistenAktivFilter)) { }
}

public class RequireFaHierarchyKommissionierlistenAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireFaHierarchyKommissionierlistenAktivFilter(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = await _settings.GetValueAsync(AppSettingKeys.FaHierarchyKommissionierlistenAktiv);
        // Default-aus: nur der explizite Wert "true" aktiviert das Modul.
        var aktiv = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        if (aktiv)
        {
            await next();
            return;
        }

        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Das IDEAL-Kommissionierlisten-Modul ist deaktiviert.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
