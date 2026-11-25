using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>
/// v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht F7, Rückfrage 12a): Lesen der Seite „Sage-Buchungen“ - <c>stock_read</c>-Kreis
/// (<see cref="ICurrentUserService.CanAccessStockReadAsync"/>) ODER Rolle <c>einkauf</c> (bzw. admin) bei eingeschaltetem Gate
/// <see cref="AppSettingKeys.EinkaufAktiv"/>. Was der Einkauf dort sieht (nur Sage-Wareneingänge, keine Knöpfe), regelt der Controller.
/// </summary>
public class RequireStockReadOrEinkaufAccessAttribute : TypeFilterAttribute
{
    public RequireStockReadOrEinkaufAccessAttribute() : base(typeof(RequireStockReadOrEinkaufAccessFilter)) { }
}

public class RequireStockReadOrEinkaufAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _settings;

    public RequireStockReadOrEinkaufAccessFilter(ICurrentUserService currentUserService, IAppSettingRepository settings)
    {
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanAccessStockReadAsync() && !await EinkaufAsync(_currentUserService, _settings))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }

    /// <summary>Einkauf-Lesekreis: Rolle einkauf/admin und <c>EinkaufAktiv</c> = "true" (wie <see cref="RequireEinkaufAktivAttribute"/>).</summary>
    public static async Task<bool> EinkaufAsync(ICurrentUserService user, IAppSettingRepository settings)
        => await user.CanAccessEinkaufAsync()
           && string.Equals(await settings.GetValueAsync(AppSettingKeys.EinkaufAktiv), "true", StringComparison.OrdinalIgnoreCase);
}
