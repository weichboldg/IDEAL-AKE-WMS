using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// IDEAL Teil 4 — Beschichtungsauftrag (read-only Liste + Bildschirmdruck) auf den in Teil 1
/// importierten <see cref="Models.FaHierarchyNode"/>/<see cref="Models.FaHierarchyOrderInfo"/>-
/// Tabellen. <b>Erweitert</b> den in Teil 3 geschnittenen gemeinsamen FaHierarchy-Listen-/Druck-
/// Baustein (<see cref="FaHierarchyListBuilder"/>) ueber die duenne
/// <see cref="BeschichtungsauftragService"/>-Schicht: Filter <c>Beschichtet == true</c> ueber ALLE
/// Ebenen (kein Blattfilter, kein Anomalie-Banner), gruppiert nach <c>HauptFA</c>.
///
/// <para>Zugriff: Class-Level <see cref="RequireBeschichtungsauftragAccessAttribute"/> (neue Rolle
/// <c>beschichtungsauftrag</c>, Read-Ebene) + Feature-Gate
/// <see cref="RequireFaHierarchyBeschichtungAktivAttribute"/> (Toggle
/// <see cref="Models.AppSettingKeys.FaHierarchyBeschichtungAktiv"/>, invertierte Default-aus-
/// Semantik). Beide kumulativ.</para>
/// </summary>
[RequireBeschichtungsauftragAccess]
[RequireFaHierarchyBeschichtungAktiv]
public class FaHierarchyBeschichtungController : Controller
{
    private readonly BeschichtungsauftragService _service;
    private readonly IBarcodeService _barcode;
    private readonly ICurrentUserService _currentUserService;

    public FaHierarchyBeschichtungController(
        BeschichtungsauftragService service,
        IBarcodeService barcode,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _barcode = barcode;
        _currentUserService = currentUserService;
    }

    /// <summary>Bildschirmliste: gruppiert nach HauptFA, Gruppen-Paging, Server-Spaltenfilter.</summary>
    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildAsync(columnFilters, page, effectivePageSize, paginate: true);

        var vm = new FaHierarchyBeschichtungListViewModel
        {
            Groups = result.Groups,
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

    /// <summary>Bildschirmdruck: dieselben Query-Parameter (inkl. <c>colf_*</c>), aber die GESAMTE
    /// gefilterte Menge ohne Paging — ein Ausdruck je HauptFA-Gruppe (Seitenumbruch), Barcode =
    /// HauptFA. Razor-View direkt an den Browser (<c>return View</c>), gedruckt ueber den
    /// Browser-Druckdialog; kein PrintService.</summary>
    public async Task<IActionResult> Print()
    {
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        // paginate:false + int.MaxValue => ganze gefilterte Menge; Barcode je Gruppe vorberechnen.
        var result = await _service.BuildAsync(columnFilters, 1, int.MaxValue, paginate: false);

        var barcodes = result.Groups.ToDictionary(
            g => g.HauptFA,
            g => _barcode.EncodeCode128Base64(g.HauptFA.ToString(CultureInfo.InvariantCulture)));

        var vm = new FaHierarchyBeschichtungPrintViewModel
        {
            Groups = result.Groups,
            Barcodes = barcodes
        };

        return View(vm);
    }
}
