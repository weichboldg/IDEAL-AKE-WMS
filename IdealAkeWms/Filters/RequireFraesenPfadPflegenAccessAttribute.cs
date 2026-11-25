using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

/// <summary>v1.99.72 (Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material F14): Fraesen-Pfad pflegen (admin, masterdata,
/// fraesenpfad_pflegen). Action-Level (ADR 0006 Read/Edit-Split), zusaetzlich zu <see cref="RequireFraesenPfadAktivAttribute"/>.</summary>
public class RequireFraesenPfadPflegenAccessAttribute : TypeFilterAttribute
{
    public RequireFraesenPfadPflegenAccessAttribute() : base(typeof(RequireFraesenPfadPflegenAccessFilter)) { }
}

public class RequireFraesenPfadPflegenAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireFraesenPfadPflegenAccessFilter(ICurrentUserService currentUserService) => _currentUserService = currentUserService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPflegeFraesenPfadAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }
        await next();
    }
}
