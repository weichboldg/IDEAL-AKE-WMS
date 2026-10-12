using System.Globalization;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Arbeitsgang-Stammdaten — Reiter „Arbeitsgänge" der BDE-Stammdaten (Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen).
/// Nur Admin (<see cref="RequireAdminAccessAttribute"/>) und bewusst OHNE <c>[RequireBdeActive]</c> (Entscheidung 1: die Offsets
/// braucht OSEON und die FA-Struktur auch ohne BDE) — deshalb eigener Controller statt Action im gegateten
/// <c>BdeMasterDataController</c>; Muster <c>BdeShiftCalendarController</c>. Loest <c>SettingsController.OperationConfig</c> ab.
/// </summary>
[RequireAdminAccess]
public class OperationConfigsController : Controller
{
    private readonly IOperationConfigRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public OperationConfigsController(IOperationConfigRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    /// <summary>Offset wie angezeigt: Vorzeichen bei positiven Werten („+2"), sonst „0" bzw. „-1".</summary>
    public static string FormatOffset(int offset) => offset > 0 ? "+" + offset.ToString(CultureInfo.InvariantCulture) : offset.ToString(CultureInfo.InvariantCulture);

    /// <summary>Server-Spaltenfilter (ADR 0005): Getter liefern den gerenderten Text.</summary>
    public static readonly IReadOnlyDictionary<string, Func<OperationConfig, string?>> ColumnMap =
        new Dictionary<string, Func<OperationConfig, string?>>
        {
            ["name"] = c => c.OperationName,
            ["display"] = c => c.DisplayName,
            ["offset"] = c => FormatOffset(c.DueDateOffsetDays),
            ["relevant"] = c => c.IsExternalSystemRelevant ? "Ja" : "Nein",
        };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var all = await _repo.GetAllAsync();
        var filtered = ColumnFilterHelper.Apply(all, columnFilters, ColumnMap).ToList();

        return View(new OperationConfigIndexViewModel
        {
            Configs = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Unconfigured = await _repo.GetUnconfiguredOperationsAsync(),
            DuplicateNames = all.GroupBy(c => c.OperationName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => string.Join(" / ", g.Select(c => c.OperationName))).ToList(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = filtered.Count,
            },
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string? operationName, string? displayName, int dueDateOffsetDays, bool isExternalSystemRelevant)
    {
        var name = (operationName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            TempData["WarningMessage"] = "Kürzel darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }
        if (await _repo.ExistsAsync(name))
        {
            TempData["WarningMessage"] = $"Kürzel „{name}“ existiert bereits (Groß-/Kleinschreibung und Leerzeichen zählen nicht).";
            return RedirectToAction(nameof(Index));
        }

        await _repo.AddAsync(new OperationConfig
        {
            OperationName = name,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            DueDateOffsetDays = dueDateOffsetDays,
            IsExternalSystemRelevant = isExternalSystemRelevant,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUser.GetDisplayName(),
            CreatedByWindows = _currentUser.GetWindowsUserName(),
        });
        TempData["SuccessMessage"] = $"Arbeitsgang „{name}“ angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? displayName, int dueDateOffsetDays, bool isExternalSystemRelevant)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config == null) return NotFound();

        config.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        config.DueDateOffsetDays = dueDateOffsetDays;
        config.IsExternalSystemRelevant = isExternalSystemRelevant;
        config.ModifiedAt = DateTime.Now;
        config.ModifiedBy = _currentUser.GetDisplayName();
        config.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _repo.UpdateAsync(config);
        TempData["SuccessMessage"] = $"Arbeitsgang „{config.OperationName}“ gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config == null) return NotFound();

        await _repo.DeleteAsync(id);
        TempData["SuccessMessage"] = $"Arbeitsgang „{config.OperationName}“ gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
