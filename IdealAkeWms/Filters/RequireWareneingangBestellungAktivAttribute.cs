using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// v1.84.0 Feature-Gate (Spec 2026-11-08-wareneingang-aus-sage-bestellungen F2, ADR 0011): „Offene Bestellungen“ und „Wareneingang zu
/// Bestellung“ nur bei AppSetting <c>WareneingangBestellungAktiv</c> = true (Default aus); sonst 404. Kumulativ zu den Rollen-Filtern.
/// </summary>
public class RequireWareneingangBestellungAktivAttribute : TypeFilterAttribute
{
    public RequireWareneingangBestellungAktivAttribute() : base(typeof(RequireWareneingangBestellungAktivFilter)) { }
}

public class RequireWareneingangBestellungAktivFilter : IAsyncActionFilter
{
    private readonly IAppSettingRepository _settings;

    public RequireWareneingangBestellungAktivFilter(IAppSettingRepository settings) => _settings = settings;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var raw = await _settings.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv);
        if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        context.Result = new NotFoundResult();
    }
}
