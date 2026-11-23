using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.HierarchischeStruktur;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// v1.99.61 (Gerald 2026-11-23): Feature-Gate der FA-Struktur am Master <see cref="HierarchischeStrukturKeys.Master"/>
/// (ServiceSetting <c>ProduktionsauftragHierarchisch</c>) – dieselbe Regel wie Menue/Kachel in NavigationSicht (ADR 0011, kumulativ zum
/// Rollen-Filter). Nur <c>"true"</c> schaltet frei. MVC =&gt; Redirect Home + WarningMessage, API =&gt; 404.
/// </summary>
public class RequireHierarchischeStrukturAktivAttribute : TypeFilterAttribute
{
    public RequireHierarchischeStrukturAktivAttribute() : base(typeof(RequireHierarchischeStrukturAktivFilter)) { }
}

public class RequireHierarchischeStrukturAktivFilter(IServiceSettingRepository settings) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.Equals((await settings.GetValueAsync(HierarchischeStrukturKeys.Master))?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        if (context.Controller is Controller mvc)
        {
            mvc.TempData["WarningMessage"] = "Die FA-Struktur ist nur bei hierarchischer Struktur (Einstellung „ProduktionsauftragHierarchisch“) verfügbar.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
