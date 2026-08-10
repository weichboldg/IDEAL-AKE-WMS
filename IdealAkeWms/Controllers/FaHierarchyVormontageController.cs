using System;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// IDEAL Teil 5 — Vormontage-Listen (read-only) auf den in Teil 1 importierten
/// <see cref="Models.FaHierarchyNode"/>/<see cref="Models.FaHierarchyOrderInfo"/>-Tabellen. Zwei
/// Sichten auf dieselbe Blattbasis (Blattpositionen <c>SubFA = 0</c> mit gesetztem <c>VMBedarf</c>),
/// je Arbeitsbereich (<c>VMBedarf</c>-Reiter):
/// <list type="bullet">
///   <item><see cref="Index"/> — <b>Sicht 1 (Einzelne Teile)</b>: gruppiert nach <c>HauptFA</c>
///     ueber den gemeinsamen <see cref="FaHierarchyListBuilder"/> (wie Teil 3).</item>
///   <item><see cref="Summiert"/> — <b>Sicht 2 (Summiert)</b>: nach <c>Matchcode</c> aggregiert.</item>
/// </list>
///
/// <para>Zugriff: Class-Level <see cref="RequireVorbauAccessAttribute"/> (identisch zur bestehenden
/// FA-Abarbeitungsliste <c>FaWorklistController</c>, am Code verifiziert — Read-Ebene genuegt, reine
/// Liste ohne Bearbeitung, keine neue Rolle) + Feature-Gate
/// <see cref="RequireFaHierarchyVormontageAktivAttribute"/> (Toggle
/// <see cref="Models.AppSettingKeys.FaHierarchyVormontageAktiv"/>, Default aus).</para>
/// </summary>
[RequireVorbauAccess]
[RequireFaHierarchyVormontageAktiv]
public class FaHierarchyVormontageController : Controller
{
    private readonly VormontageService _service;
    private readonly ICurrentUserService _currentUserService;

    public FaHierarchyVormontageController(
        VormontageService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Loest den aktiven Reiter auf: der angefragte <c>bereich</c>, sofern er unter den vorkommenden
    /// Werten ist; sonst der erste vorhandene Bereich; sonst null (kein Bereich vorhanden). So
    /// aggregiert Sicht 2 nie versehentlich ueber alle Bereiche und es gibt keinen leeren Reiter.
    /// </summary>
    private static string? ResolveBereich(string? requested, System.Collections.Generic.IReadOnlyList<string> available)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var match = available.FirstOrDefault(
                b => string.Equals(b, requested.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        return available.Count > 0 ? available[0] : null;
    }

    /// <summary>Sicht 1 (Einzelne Teile): gruppiert nach HauptFA, Gruppen-Paging,
    /// Server-Spaltenfilter, VMBedarf-Reiter, Anomalie-Banner.</summary>
    public async Task<IActionResult> Index(string? bereich, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var bereiche = await _service.GetBereicheAsync();
        var selected = ResolveBereich(bereich, bereiche);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildEinzelteileAsync(selected, columnFilters, page, effectivePageSize, paginate: true);

        var vm = new FaHierarchyVormontageListViewModel
        {
            Groups = result.Groups,
            AvailableBereiche = bereiche,
            SelectedBereich = selected,
            Anomalies = result.Anomalies,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = result.TotalGroupCount
            }
        };

        return View(vm);
    }

    /// <summary>Sicht 2 (Summiert): nach Matchcode aggregiert je VMBedarf-Reiter, zwei getrennte
    /// Summenspalten, Paging auf Aggregatzeilen-Ebene, Server-Spaltenfilter auf der Aggregat-Projektion.</summary>
    public async Task<IActionResult> Summiert(string? bereich, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var bereiche = await _service.GetBereicheAsync();
        var selected = ResolveBereich(bereich, bereiche);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildSummiertAsync(selected, columnFilters, page, effectivePageSize, paginate: true);

        var vm = new FaHierarchyVormontageSummiertViewModel
        {
            Rows = result.Rows,
            AvailableBereiche = bereiche,
            SelectedBereich = selected,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = result.TotalRowCount
            }
        };

        return View(vm);
    }
}
