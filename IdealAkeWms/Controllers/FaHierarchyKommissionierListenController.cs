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
/// IDEAL Teil 3 — Kommissionierlisten (read-only Liste + Bildschirmdruck) auf den in Teil 1
/// importierten <see cref="Models.FaHierarchyNode"/>/<see cref="Models.FaHierarchyOrderInfo"/>-
/// Tabellen. Zugleich <b>Referenzimplementierung des gemeinsamen FaHierarchy-Listen-/Druck-
/// Bausteins</b> (<see cref="FaHierarchyListBuilder"/> + <see cref="KommissionierListenService"/>),
/// den Teil 4 (Beschichtung) und Teil 5 (Vormontage) mit ihrem jeweiligen Flag erweitern.
///
/// <para>Zugriff: Class-Level <see cref="RequireLagerProcessingAccessAttribute"/> (identisch zum
/// bestehenden <c>WarehousePickingController</c>, am Code verifiziert) + Feature-Gate
/// <see cref="RequireFaHierarchyKommissionierlistenAktivAttribute"/> (Toggle
/// <see cref="Models.AppSettingKeys.FaHierarchyKommissionierlistenAktiv"/>, Default aus). Reine
/// Read-View ⇒ kein Edit-Split, keine neue Rolle.</para>
///
/// Der Controller-Name ist laut Spec provisorisch, aber im gesamten Etappen-Set konsistent verwendet.
/// </summary>
[RequireLagerProcessingAccess]
[RequireFaHierarchyKommissionierlistenAktiv]
public class FaHierarchyKommissionierListenController : Controller
{
    private readonly KommissionierListenService _service;
    private readonly IBarcodeService _barcode;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRazorViewRenderer _viewRenderer;
    private readonly IPdfRenderService _pdfRenderService;

    public FaHierarchyKommissionierListenController(
        KommissionierListenService service,
        IBarcodeService barcode,
        ICurrentUserService currentUserService,
        IRazorViewRenderer viewRenderer,
        IPdfRenderService pdfRenderService)
    {
        _service = service;
        _barcode = barcode;
        _currentUserService = currentUserService;
        _viewRenderer = viewRenderer;
        _pdfRenderService = pdfRenderService;
    }

    /// <summary>Bildschirmliste: gruppiert nach HauptFA, Gruppen-Paging, Server-Spaltenfilter,
    /// Ziel-Dropdown. (Nachtrag 2026-08-13: kein Blattfilter/Anomalie-Banner mehr.)</summary>
    public async Task<IActionResult> Index(string? target, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildAsync(target, columnFilters, page, effectivePageSize, paginate: true);
        var targets = await _service.GetTargetValuesAsync();

        var vm = new FaHierarchyKommissionierListViewModel
        {
            Groups = result.Groups,
            AvailableTargets = targets,
            SelectedTarget = target,
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

    /// <summary>Summiert-Ansicht (Nachtrag 2026-08-13, Aenderung 2): je HauptFA nach Artnr und
    /// Kommissionierziel aggregierte <c>Sollmenge</c>-Summe. KW-Zeitraum-Eingabe filtert die
    /// einzubeziehenden HauptFA ueber deren <c>KO_Termin</c> (ISO-Woche Mo–So). Ohne KW-Eingabe
    /// Hinweistext statt Gesamtsumme. Zugriff/Toggle identisch zu <see cref="Index"/>.</summary>
    public async Task<IActionResult> Summiert(
        string? target, string? kwVon, string? kwBis, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var kwRange = IsoWeekRange.Resolve(kwVon, kwBis);
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildSummiertAsync(
            target, kwRange, columnFilters, page, effectivePageSize, paginate: true);
        var targets = await _service.GetTargetValuesAsync();

        var vm = new FaHierarchyKommissionierSummiertViewModel
        {
            Rows = result.Rows,
            AvailableTargets = targets,
            SelectedTarget = target,
            KwVon = kwVon,
            KwBis = kwBis,
            HasKwFilter = kwRange is not null,
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

    /// <summary>Bildschirmdruck: dieselben Query-Parameter (inkl. <c>colf_*</c> + Ziel), aber die
    /// GESAMTE gefilterte Menge ohne Paging — ein Ausdruck je HauptFA-Gruppe (Seitenumbruch),
    /// Barcode = HauptFA. Bewusst ohne <c>id</c>-Parameter (anders als
    /// <c>WarehousePickingController.Print(int id)</c>, dessen Einzel-id-Muster hier nicht passt).</summary>
    public async Task<IActionResult> Print(string? target)
    {
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        // paginate:false + int.MaxValue => ganze gefilterte Menge; Barcode je Gruppe vorberechnen.
        var result = await _service.BuildAsync(target, columnFilters, 1, int.MaxValue, paginate: false);

        var barcodes = result.Groups.ToDictionary(
            g => g.HauptFA,
            g => _barcode.EncodeCode128Base64(g.HauptFA.ToString(CultureInfo.InvariantCulture)));

        var vm = new FaHierarchyKommissionierPrintViewModel
        {
            Groups = result.Groups,
            Barcodes = barcodes,
            SelectedTarget = target,
            IsFiltered = ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target)
        };

        return View(vm);
    }

    /// <summary>PDF-Download fuer GENAU EINE HauptFA-Gruppe (Spec 2026-08-06): dieselbe
    /// <c>Print.cshtml</c>, dieselben Query-Filter (<c>target</c> + <c>colf_*</c>) wie der
    /// Bildschirmdruck, aber auf eine Gruppe reduziert. <c>hauptFa</c> als Routen-Segment
    /// (Freigabe-Antwort 7). Fehler werden als <c>WarningMessage</c> mit Rueckfallweg gemeldet.</summary>
    [HttpGet("FaHierarchyKommissionierListen/Pdf/{hauptFa:int}")]
    public async Task<IActionResult> Pdf(int hauptFa, string? target)
    {
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var result = await _service.BuildAsync(target, columnFilters, 1, int.MaxValue, paginate: false);
        var group = result.Groups.FirstOrDefault(g => g.HauptFA == hauptFa);
        if (group is null) return NotFound();

        var vm = new FaHierarchyKommissionierPrintViewModel
        {
            Groups = new List<FaHierarchyListGroupViewModel> { group },
            Barcodes = new Dictionary<int, string> { [hauptFa] = _barcode.EncodeCode128Base64(hauptFa.ToString(CultureInfo.InvariantCulture)) },
            SelectedTarget = target,
            IsFiltered = ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target)
        };

        try
        {
            var html = await _viewRenderer.RenderToStringAsync(ControllerContext, "Print", vm);
            var pdfBytes = await _pdfRenderService.RenderHtmlToPdfAsync(html, HttpContext.RequestAborted);
            return File(pdfBytes, "application/pdf", PdfFileNameBuilder.Build("Kommissionierliste", hauptFa, DateTime.Now));
        }
        catch (PdfRenderException ex)
        {
            TempData["WarningMessage"] = ex.UserMessage;
            return RedirectToAction(nameof(Index));
        }
    }
}
