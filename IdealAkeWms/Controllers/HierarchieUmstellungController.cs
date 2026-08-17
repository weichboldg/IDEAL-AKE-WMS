using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Teil 7 — dedizierte Umschalt-Seite für das FA-Hierarchie-Einwegtor
/// (<see cref="HierarchischeStrukturKeys.Master"/>). Einziger sanktionierter UI-Schreibweg;
/// die generische ServiceSettings-Maske zeigt den Master nur read-only. Die eigentliche
/// Sicherung ist der <see cref="GuardedServiceSettingRepository"/>-Choke-Point — dieser
/// Controller validiert zusätzlich vorab für eine saubere Meldung und protokolliert.
/// </summary>
[RequireAdminAccess]
public class HierarchieUmstellungController : Controller
{
    private readonly IServiceSettingRepository _settings;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly HierarchischeStrukturStatus _status;
    private readonly ISyncLogger _syncLogger;
    private readonly ICurrentUserService _currentUser;

    public HierarchieUmstellungController(
        IServiceSettingRepository settings,
        IProductionOrderRepository productionOrders,
        HierarchischeStrukturStatus status,
        ISyncLogger syncLogger,
        ICurrentUserService currentUser)
    {
        _settings = settings;
        _productionOrders = productionOrders;
        _status = status;
        _syncLogger = syncLogger;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        // Anzeige-Zustand bei jedem Aufruf auffrischen (Freigabe-Antwort 3): der
        // Materialisierungs-Sync laeuft im Service-Prozess und kann den Web-Cache nicht direkt
        // aktualisieren. Admin-only + niederfrequent, der Live-Read ist unkritisch.
        await _status.RefreshAsync();
        var value = await _settings.GetValueAsync(HierarchischeStrukturKeys.Master);
        return View(new HierarchieUmstellungViewModel
        {
            MasterEnabled = ParseBool(value),
            IsLocked = _status.HierarchicalDataExists,
            StatusRefreshedUtc = _status.LastRefreshedUtc,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Enable() => SetMasterAsync(true);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Disable() => SetMasterAsync(false);

    private async Task<IActionResult> SetMasterAsync(bool target)
    {
        var current = ParseBool(await _settings.GetValueAsync(HierarchischeStrukturKeys.Master));
        if (current == target)
        {
            TempData["WarningMessage"] = "Keine Änderung: der Schalter stand bereits so.";
            return RedirectToAction(nameof(Index));
        }

        // Live-Sperrprüfung für eine saubere Meldung (Guard-Decorator sichert zusätzlich hart ab).
        var dataExists = await _productionOrders.HierarchicalDataExistsAsync();
        var decision = HierarchischeStrukturGuard.Evaluate(
            GuardedSetting.Master, current, target, dataExists);
        if (!decision.Allowed)
        {
            await using var rejectRun = await _syncLogger.BeginRunAsync(SyncLogServices.HierarchieUmstellung);
            await rejectRun.LogWarningAsync(
                $"Master-Umschaltung abgelehnt ({(target ? "aktivieren" : "deaktivieren")}) durch {_currentUser.GetWindowsUserName()}: {decision.RejectionReason}");
            await rejectRun.FinishSuccessAsync();
            TempData["WarningMessage"] = decision.RejectionReason;
            return RedirectToAction(nameof(Index));
        }

        await _settings.UpsertAsync(HierarchischeStrukturKeys.Master, target ? "true" : "false",
            "FA-Hierarchie", "EINWEGTOR (siehe Umschalt-Seite)");

        // Beim Einschalten einen ggf. aktiven Auto-Erledigt-Schalter abschalten (defensiv;
        // No-op solange der Key (Etappe D) nicht existiert) + protokollieren.
        var autoOff = false;
        if (target && ParseBool(await _settings.GetValueAsync(HierarchischeStrukturKeys.AutoErledigt)))
        {
            await _settings.UpsertAsync(HierarchischeStrukturKeys.AutoErledigt, "false", "Sync",
                "Automatisch abgeschaltet beim Umlegen des FA-Hierarchie-Masters.");
            autoOff = true;
        }

        await using (var run = await _syncLogger.BeginRunAsync(SyncLogServices.HierarchieUmstellung))
        {
            await run.LogInfoAsync(
                $"FA-Hierarchie-Master auf '{(target ? "aktiviert" : "deaktiviert")}' gesetzt durch {_currentUser.GetWindowsUserName()}."
                + (autoOff ? " Auto-Erledigt automatisch abgeschaltet." : string.Empty));
            await run.FinishSuccessAsync();
        }

        await _status.RefreshAsync();
        TempData["SuccessMessage"] = target
            ? "Umstellung auf hierarchische Produktionsaufträge aktiviert."
            : "Umstellung deaktiviert (es lagen noch keine hierarchischen Daten vor).";
        return RedirectToAction(nameof(Index));
    }

    private static bool ParseBool(string? v) =>
        string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
}
