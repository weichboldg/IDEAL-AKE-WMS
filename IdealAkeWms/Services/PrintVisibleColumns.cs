using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// Sichtbare Druckspalten fuer die FaHierarchy-Druckdokumente (Spec 2026-09-25-kommissionierliste-summierung-pdf,
/// Abschnitt B1a; seit v1.48.0 auch Beschichtungsauftrag summiert). Ist <c>visibleColumns</c> gesetzt (DOM-Stand
/// zur Klickzeit, PrintBom-Konvention, <c>print-visible-columns.js</c>), wird er direkt verwendet — kein DB-Zugriff.
/// Sonst Server-Rueckfall auf die gespeicherte <c>UserViewPreference</c> (nur Mitgliedschaft, keine Reihenfolge —
/// die ist im Druck-View fest verdrahtet). Locked-Spalten bleiben immer sichtbar. Kein Parameter, keine Praeferenz
/// oder kein eingeloggter Anwender ⇒ leere Liste = alle Spalten.
/// </summary>
public static class PrintVisibleColumns
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        string? visibleColumns, string viewKey, IReadOnlyList<ColumnDef> defs,
        ICurrentUserService currentUser, IUserViewPreferenceRepository viewPrefs)
    {
        if (!string.IsNullOrWhiteSpace(visibleColumns))
            return visibleColumns.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var userId = currentUser.GetCurrentAppUserId();
        if (!userId.HasValue) return Array.Empty<string>();

        var pref = await viewPrefs.GetByUserAndViewAsync(userId.Value, viewKey);
        var prefs = WarehousePickingPrintLayout.ParsePrefs(pref?.SettingsJson);
        if (prefs?.Columns == null || prefs.Columns.Count == 0) return Array.Empty<string>();

        var hidden = new HashSet<string>(prefs.Columns.Where(c => !c.Visible).Select(c => c.Key));
        return defs.Where(d => d.Locked || !hidden.Contains(d.Key)).Select(d => d.Key).ToList();
    }
}
