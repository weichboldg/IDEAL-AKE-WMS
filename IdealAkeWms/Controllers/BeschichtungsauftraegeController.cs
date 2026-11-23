using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Bedienung des gespeicherten Beschichtungsauftrags (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4): Liste, Detail,
/// Entwurf bearbeiten, Positionen hinzufuegen, Bemerkung, Status, Reklamation, Druck/PDF. Die fachlichen Regeln liegen
/// ausschliesslich im <see cref="BeschichtungsauftragWorkflow"/> (Teil 3).
///
/// <para><b>Ein Workflow-Aufruf je Request, danach Redirect:</b> nach einem gescheiterten Speichern verwirft der Workflow den
/// Change-Tracker des Request-Contexts — ein zweiter schreibender Aufruf im selben Request saehe einen leeren Context.</para>
///
/// <para>Zugriff (ADR 0006 Read/Edit-Split): Class-Level Lesen (<c>beschichtungsauftrag</c> oder Schreibrolle) + Feature-Gate
/// <see cref="RequireFaHierarchyBeschichtungAktivAttribute"/>; jede schreibende Action zusaetzlich
/// <see cref="RequireBeschichtungsauftragWriteAccessAttribute"/> (<c>beschichtungsauftrag_schreiben</c>).</para>
/// </summary>
[RequireBeschichtungsauftragAccess]
[RequireFaHierarchyBeschichtungAktiv]
public class BeschichtungsauftraegeController : Controller
{
    private const string Dokumentart = "Beschichtungsauftrag";

    private readonly IBeschichtungsauftragRepository _repo;
    private readonly BeschichtungsauftragWorkflow _workflow;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IBarcodeService _barcode;
    private readonly IRazorViewRenderer _viewRenderer;
    private readonly IPdfRenderService _pdfRenderService;
    private readonly PdfRenderStatus _pdfRenderStatus;
    private readonly IUserViewPreferenceRepository _viewPrefs;

    public BeschichtungsauftraegeController(
        IBeschichtungsauftragRepository repo,
        BeschichtungsauftragWorkflow workflow,
        IFaHierarchyOrderInfoRepository orderInfos,
        IUserRepository users,
        ICurrentUserService currentUser,
        IBarcodeService barcode,
        IRazorViewRenderer viewRenderer,
        IPdfRenderService pdfRenderService,
        PdfRenderStatus pdfRenderStatus,
        IUserViewPreferenceRepository viewPrefs)
    {
        _repo = repo;
        _workflow = workflow;
        _orderInfos = orderInfos;
        _users = users;
        _currentUser = currentUser;
        _barcode = barcode;
        _viewRenderer = viewRenderer;
        _pdfRenderService = pdfRenderService;
        _pdfRenderStatus = pdfRenderStatus;
        _viewPrefs = viewPrefs;
    }

    // ===================== Lesen =====================

    /// <summary>Liste (ADR 0005): Statusfilter, Server-Spaltenfilter auf der Projektion (Datumsspalten danach), Pagination.</summary>
    public async Task<IActionResult> Index(BeschichtungsauftragStatus? status, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var rows = (await _repo.GetAlleAsync())
            .Where(a => status is null || a.Status == status)
            .OrderByDescending(a => a.Jahr).ThenByDescending(a => a.LaufendeNummer)
            .Select(BeschichtungsauftragListRow.Von);
        var gefiltert = ColumnFilterHelper.Apply(rows, columnFilters, BeschichtungsauftragListRow.ColumnMap, ColumnFilterTypes.For("Beschichtungsauftraege/Index")).ToList();

        return View(new BeschichtungsauftragListViewModel
        {
            Rows = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Status = status,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = gefiltert.Count,
            },
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var a = await _repo.GetDetailAsync(id);
        if (a is null) return NotFound();

        var darfSchreiben = await _currentUser.HasBeschichtungsauftragWriteAccessAsync();
        var kandidaten = darfSchreiben && BeschichtungsauftragWorkflow.PositionenAenderbar(a.Status)
            ? await _workflow.GetHinzufuegbarePositionenAsync(id)
            : new List<PositionsKandidat>();

        var benutzerIds = new[] { a.UebergebenByUserId, a.VersendetByUserId, a.ErledigtByUserId, a.StorniertByUserId }.Where(i => i.HasValue).ToList();
        var benutzer = benutzerIds.Count == 0
            ? new Dictionary<int, string>()
            : (await _users.GetAllWithRolesAsync()).ToDictionary(u => u.Id, u => u.Name);

        return View(new BeschichtungsauftragDetailViewModel
        {
            Auftrag = a,
            DarfSchreiben = darfSchreiben,
            PdfVerfuegbar = _pdfRenderStatus.IsAvailable,
            SageDienstleisterAbweichend = await SageDienstleisterAbweichendAsync(a),
            Kandidaten = kandidaten,
            Benutzer = benutzer,
            // A9: Auswahl im Entwurf; v1.99.42: auch in Uebergeben, solange kein Kreditor gesetzt ist (Nachtrag Altauftraege)
            Beschichter = darfSchreiben && (a.Status == BeschichtungsauftragStatus.Entwurf
                                            || (a.Status == BeschichtungsauftragStatus.Uebergeben && a.CreditorId is null))
                ? await _repo.GetBeschichterAsync() : new List<Creditor>(),
            DarfStammdaten = await _currentUser.HasMasterDataAccessAsync(),
        });
    }

    /// <summary>Sage-Wert (PPS-View) neben dem Auftrag, wenn er abweicht (Anforderung 6). Fehlt der HauptFA im Cache: kein Vergleich.</summary>
    private async Task<string?> SageDienstleisterAbweichendAsync(Beschichtungsauftrag a)
    {
        var sage = (await _orderInfos.GetByHauptFaAsync(a.HauptFA))
            .Select(i => i.Dienstleister?.Trim()).Where(d => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (sage.Count == 0) return null;
        var text = string.Join(" / ", sage);
        // A10: mit Kreditor steht dessen Name am Auftrag — gleich ist auch Sage-Text = Kurzname des Kreditors
        return string.Equals(text, a.Dienstleister?.Trim(), StringComparison.OrdinalIgnoreCase)
               || string.Equals(text, a.Creditor?.ShortName?.Trim(), StringComparison.OrdinalIgnoreCase) ? null : text;
    }

    // ===================== Schreiben (je Request EIN Workflow-Aufruf) =====================

    /// <summary>„Aus Vorschlagsliste erstellen" (v1.99.41 A1-A4): nur die angehakten Positionen (<paramref name="schluessel"/>,
    /// Ansicht Liste), Summiert-Zeilen (<paramref name="artnr"/> = alle freien Positionen dieser Artnr) und freie Positionen
    /// ohne FA. Nichts angehakt = Warnung, kein Auftrag.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Create(int hauptFa, string? zurueck, string[]? schluessel = null, string[]? artnr = null,
        string?[]? freiArtikelId = null, string?[]? freiMenge = null)
    {
        IActionResult Zurueck(string text)
        {
            TempData["WarningMessage"] = text;
            return zurueck == "Index"
                ? RedirectToAction("Index", "FaHierarchyBeschichtung")
                : RedirectToAction("Summiert", "FaHierarchyBeschichtung");
        }

        var keys = new List<PositionsSchluessel>();
        foreach (var s in schluessel ?? Array.Empty<string>())
        {
            if (!TrySchluessel(s, out var k)) return Zurueck($"Nichts angelegt: ungültiger Positionsschlüssel „{s}“.");
            keys.Add(k);
        }
        if (!TryFreiePositionen(freiArtikelId, freiMenge, out var frei, out var fehler)) return Zurueck("Nichts angelegt: " + fehler);
        var artnrListe = (artnr ?? Array.Empty<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        var r = await _workflow.AnlegenAsync(hauptFa, new VorschlagAuswahl(keys, artnrListe, frei));
        if (!r.Erfolg) return Zurueck(string.Join(" ", r.Fehler));
        TempData["SuccessMessage"] = $"Beschichtungsauftrag {r.Auftrag!.Nummer} für HauptFA {hauptFa} angelegt ({r.Auftrag.Positionen.Count} Positionen).";
        if (r.Hinweise.Count > 0) TempData["WarningMessage"] = string.Join(" ", r.Hinweise);
        return RedirectToAction(nameof(Details), new { id = r.Auftrag.Id });
    }

    /// <summary>EIN Speichern am Auftrag (v1.99.41 A8): Kopf (nur Entwurf; <paramref name="nurPositionen"/> = Uebergeben, Kopf
    /// unveraendert), Mengen, Entfernen, Haken Verpackt (<paramref name="hakenGesendet"/>: ungehakte Checkboxen senden nichts),
    /// Kandidaten (<paramref name="schluessel"/>) und freie Positionen (<paramref name="freiArtikelId"/> + <paramref name="freiMenge"/>
    /// paarweise). Mengen kommen als Text (Punkt oder Komma) und werden kulturunabhaengig gelesen.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Bearbeiten(int id, string? dienstleister, string? ral, DateTime? startBeschichtung,
        DateTime? beschichtenRetour, Dictionary<int, string>? mengen, int[]? entfernen, string? rowVersion,
        bool nurPositionen = false, bool hakenGesendet = false, int[]? verpackt = null, string[]? schluessel = null,
        string?[]? freiArtikelId = null, string?[]? freiMenge = null, int? creditorId = null, BeschichtungsauftragStatus? ziel = null)
    {
        if (!ModelState.IsValid)
            return Warnung(id, "Eingabe nicht gespeichert: ungültiger Wert in " +
                string.Join(", ", ModelState.Where(kv => kv.Value!.Errors.Count > 0).Select(kv => kv.Key)) + ".");
        if (!TryRowVersion(rowVersion, out var rv)) return VeralteterStand(id);

        var mengenWerte = new Dictionary<int, decimal>();
        foreach (var (posId, text) in mengen ?? new Dictionary<int, string>())
        {
            if (!TryMenge(text, out var m))
                return Warnung(id, $"Eingabe nicht gespeichert: Menge „{text}“ ist keine Zahl.");
            mengenWerte[posId] = m;
        }

        var keys = new List<PositionsSchluessel>();
        foreach (var s in schluessel ?? Array.Empty<string>())
        {
            if (!TrySchluessel(s, out var k)) return Warnung(id, $"Eingabe nicht gespeichert: ungültiger Positionsschlüssel „{s}“.");
            keys.Add(k);
        }
        if (!TryFreiePositionen(freiArtikelId, freiMenge, out var frei, out var fehler)) return Warnung(id, "Eingabe nicht gespeichert: " + fehler);

        var kopf = nurPositionen ? null : new KopfDaten(Leer(dienstleister), Leer(ral), startBeschichtung, beschichtenRetour, creditorId);
        var r = await _workflow.BearbeitenAsync(id, kopf, mengenWerte, entfernen ?? Array.Empty<int>(), rv,
            hakenGesendet ? verpackt ?? Array.Empty<int>() : null, keys, frei, kreditorNachtragen: nurPositionen ? creditorId : null);
        var neu = keys.Distinct().Count() + frei.Count;
        var gespeichert = neu > 0 ? $"Gespeichert, {neu} Position(en) hinzugefügt." : "Gespeichert.";
        if (ziel is null || !r.Erfolg) return Ergebnis(id, r, gespeichert);

        // v1.99.56: Status-Knopf sendet entwurf-form mit – erst speichern, dann Statuswechsel auf dem gespeicherten Stand
        // (neue RowVersion nach SaveChanges). Scheitert nur der Wechsel: „Gespeichert.“ + Warnung, nie still.
        var st = await _workflow.StatusWechselnAsync(id, ziel.Value, r.Auftrag?.RowVersion ?? rv);
        TempData["SuccessMessage"] = st.Erfolg
            ? gespeichert.TrimEnd('.') + $", Status auf „{BeschichtungsauftragWorkflow.Anzeige(ziel.Value)}“ gesetzt."
            : gespeichert;
        var warnungen = r.Hinweise.Concat(st.Fehler).Concat(st.Hinweise).ToList();
        if (warnungen.Count > 0) TempData["WarningMessage"] = string.Join(" ", warnungen);
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Freie Positionen aus dem Formular (A4/A7): Zeilen paarweise; ganz leere Zeilen (kein Artikel, keine Menge) werden
    /// uebersprungen, Artikel ohne Menge oder Menge ohne Artikel sind ein Fehler — nie still verworfen.</summary>
    internal static bool TryFreiePositionen(string?[]? artikelIds, string?[]? mengen, out List<FreiePosition> frei, out string fehler)
    {
        frei = new();
        fehler = string.Empty;
        var a = artikelIds ?? Array.Empty<string?>();
        var m = mengen ?? Array.Empty<string?>();
        for (var i = 0; i < Math.Max(a.Length, m.Length); i++)
        {
            var art = i < a.Length ? a[i]?.Trim() : null;
            var men = i < m.Length ? m[i]?.Trim() : null;
            if (string.IsNullOrEmpty(art) && string.IsNullOrEmpty(men)) continue;
            if (!int.TryParse(art, NumberStyles.Integer, CultureInfo.InvariantCulture, out var artikelId))
            { fehler = $"Position ohne FA {i + 1}: kein Artikel ausgewählt."; return false; }
            if (!TryMenge(men, out var menge))
            { fehler = $"Position ohne FA {i + 1}: Menge „{men}“ ist keine Zahl."; return false; }
            frei.Add(new FreiePosition(artikelId, menge));
        }
        return true;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Bemerkung(int id, string? bemerkung, string? rowVersion)
    {
        if (!TryRowVersion(rowVersion, out var rv)) return VeralteterStand(id);
        return Ergebnis(id, await _workflow.BemerkungAendernAsync(id, Leer(bemerkung), rv), "Bemerkung gespeichert.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Status(int id, BeschichtungsauftragStatus ziel, string? rowVersion)
    {
        if (!TryRowVersion(rowVersion, out var rv)) return VeralteterStand(id);
        var r = await _workflow.StatusWechselnAsync(id, ziel, rv);
        return Ergebnis(id, r, $"Status auf „{BeschichtungsauftragWorkflow.Anzeige(ziel)}“ gesetzt.");
    }

    /// <summary>Teileauswahl der Reklamation — nur aus Erledigt (Teil-3-Antwort 11), nichts vorausgewaehlt.</summary>
    [HttpGet]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Reklamation(int id)
    {
        var a = await _repo.GetDetailAsync(id);
        if (a is null) return NotFound();
        if (a.Status != BeschichtungsauftragStatus.Erledigt)
            return Warnung(id, $"Reklamation nur aus „Erledigt“ — {a.Nummer} steht auf {BeschichtungsauftragWorkflow.Anzeige(a.Status)}.");
        return View(new BeschichtungsauftragReklamationViewModel { Ursprung = a });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireBeschichtungsauftragWriteAccess]
    public async Task<IActionResult> Reklamation(int id, int[]? positionIds)
    {
        var r = await _workflow.ReklamationAnlegenAsync(id, positionIds ?? Array.Empty<int>());
        if (!r.Erfolg) return Warnung(id, string.Join(" ", r.Fehler));
        TempData["SuccessMessage"] = $"Reklamation {r.Auftrag!.Nummer} mit {r.Auftrag.Positionen.Count} Position(en) angelegt.";
        if (r.Hinweise.Count > 0) TempData["WarningMessage"] = string.Join(" ", r.Hinweise);
        return RedirectToAction(nameof(Details), new { id = r.Auftrag.Id });
    }

    // ===================== Druck / PDF (ab Uebergeben, Antwort 8) =====================

    public async Task<IActionResult> Print(int id, string? visibleColumns)
    {
        var (vm, abweisung) = await PrintModelAsync(id, visibleColumns);
        return abweisung ?? View(vm);
    }

    [HttpGet("Beschichtungsauftraege/Pdf/{id:int}")]
    public async Task<IActionResult> Pdf(int id, string? visibleColumns)
    {
        var (vm, abweisung) = await PrintModelAsync(id, visibleColumns);
        if (abweisung is not null) return abweisung;
        try
        {
            var html = await _viewRenderer.RenderToStringAsync(ControllerContext, "Print", vm!);
            var pdfBytes = await _pdfRenderService.RenderHtmlToPdfAsync(html, HttpContext?.RequestAborted ?? default);
            return File(pdfBytes, "application/pdf", PdfFileNameBuilder.Build(Dokumentart, vm!.Auftrag.Nummer, DateTime.Now));
        }
        catch (PdfRenderException ex)
        {
            return Warnung(id, ex.UserMessage);
        }
    }

    private async Task<(BeschichtungsauftragPrintViewModel? Vm, IActionResult? Abweisung)> PrintModelAsync(int id, string? visibleColumns)
    {
        var a = await _repo.GetDetailAsync(id);
        if (a is null) return (null, NotFound());
        if (!BeschichtungsauftraegeDruck.IstDruckbar(a.Status))
            return (null, Warnung(id, a.Status == BeschichtungsauftragStatus.Entwurf
                ? "Im Entwurf nicht druckbar — erst übergeben."
                : $"{a.Nummer} ist storniert und nicht druckbar."));

        return (new BeschichtungsauftragPrintViewModel
        {
            Auftrag = a,
            Positionen = BeschichtungsauftraegeDruck.Sortiert(a.Positionen),
            Barcode = _barcode.EncodeCode128Base64(a.Nummer),
            VisibleColumns = await PrintVisibleColumns.ResolveAsync(visibleColumns,
                ColumnDefinitions.BeschichtungsauftragPositionen.ViewKey, ColumnDefinitions.BeschichtungsauftragPositionen.Columns,
                _currentUser, _viewPrefs),
        }, null);
    }

    // ===================== Hilfen =====================

    private IActionResult Ergebnis(int id, BeschichtungsauftragErgebnis r, string erfolg)
    {
        if (r.Erfolg) TempData["SuccessMessage"] = erfolg;
        var warnungen = r.Fehler.Concat(r.Hinweise).ToList();
        if (warnungen.Count > 0) TempData["WarningMessage"] = string.Join(" ", warnungen);
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult Warnung(int id, string text)
    {
        TempData["WarningMessage"] = text;
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult VeralteterStand(int id) =>
        Warnung(id, "Der Stand der Seite ist nicht mehr gültig — bitte neu laden und erneut versuchen.");

    private static string? Leer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    internal static bool TryRowVersion(string? text, out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();
        if (text is null) return false; // leer = leere RowVersion (nur InMemory); SQL Server meldet dann den Konflikt
        var buffer = new byte[text.Length];
        if (!Convert.TryFromBase64String(text, buffer, out var len)) return false;
        rowVersion = buffer[..len];
        return true;
    }

    internal static bool TryMenge(string? text, out decimal menge) =>
        decimal.TryParse((text ?? string.Empty).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out menge);

    /// <summary>„VaterFA|SubFA|Position", leere Teile = null (VaterFA/Position sind im Cache nullable). v1.99.34: flache Quelle
    /// (AKE) fuenfteilig „VaterFA|SubFA|Position|Artnr|PositionText", Teile 4 und 5 URL-kodiert (ein „|" im Positionstext
    /// zerlegt den Schluessel nicht); fuenf Teile = Positionstext gesetzt, auch leer.</summary>
    public static string SchluesselText(PositionsSchluessel k)
    {
        var basis = $"{k.VaterFA?.ToString(CultureInfo.InvariantCulture)}|{k.SubFA.ToString(CultureInfo.InvariantCulture)}|{k.Position?.ToString(CultureInfo.InvariantCulture)}";
        return k.PositionText is null ? basis : $"{basis}|{Uri.EscapeDataString(k.Artnr ?? "")}|{Uri.EscapeDataString(k.PositionText)}";
    }

    internal static bool TrySchluessel(string? text, out PositionsSchluessel key)
    {
        key = default;
        var teile = (text ?? string.Empty).Split('|');
        if (teile.Length is not (3 or 5) || !int.TryParse(teile[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var subFa)) return false;
        if (!TryOptional(teile[0], out var vater) || !TryOptional(teile[2], out var pos)) return false;
        key = teile.Length == 3
            ? new PositionsSchluessel(vater, subFa, pos)
            : PositionsSchluessel.Bilden(vater, subFa, pos, Uri.UnescapeDataString(teile[3]), Uri.UnescapeDataString(teile[4]));
        return true;

        static bool TryOptional(string s, out int? wert)
        {
            wert = null;
            if (s.Length == 0) return true;
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return false;
            wert = v;
            return true;
        }
    }
}
