using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// BF-Auftraege der Blechfertigung (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv): Warenkorb, Abschicken, Liste, Detail und die
/// Schachtler-Aktionen. Regeln im <see cref="BfAuftragWorkflow"/>; ein Workflow-Aufruf je Request, danach Redirect.
/// <para>Zugriff (ADR 0006/0011): Class-Level Bestellrollen ODER schachtler + Gate <c>BlechfertigungAktiv</c>; Schachtler-Aktionen zusaetzlich
/// <see cref="RequireSchachtlerAccessAttribute"/>. Besteller sehen nur eigene Auftraege.</para>
/// </summary>
[RequireBfBestellenAccess]
[RequireBlechfertigungAktiv]
public class BfAuftraegeController : Controller
{
    private readonly IBfAuftragRepository _repo;
    private readonly BfAuftragWorkflow _workflow;
    private readonly IAppSettingRepository _settings;
    private readonly IOrderRecipientRepository _groups;
    private readonly IStorageLocationRepository _lagerplaetze;
    private readonly ICurrentUserService _user;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ISyncLogger _syncLogger;

    public BfAuftraegeController(IBfAuftragRepository repo, BfAuftragWorkflow workflow, IAppSettingRepository settings,
        IOrderRecipientRepository groups, IStorageLocationRepository lagerplaetze, ICurrentUserService user,
        IServiceSettingRepository serviceSettings, ISyncLogger syncLogger)
    {
        _serviceSettings = serviceSettings;
        _syncLogger = syncLogger;
        _repo = repo;
        _workflow = workflow;
        _settings = settings;
        _groups = groups;
        _lagerplaetze = lagerplaetze;
        _user = user;
    }

    // ===================== Lesen =====================

    public async Task<IActionResult> Index(BfAuftragStatus? status, DateTime? von, DateTime? bis, bool nurMeine = false, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefault = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefault);
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var schachtler = await _user.CanSchachtelnAsync();
        var userId = _user.GetCurrentAppUserId();
        var nurEigene = nurMeine || !schachtler;

        var rows = (await _repo.GetAlleAsync())
            .Where(a => !nurEigene || BfAuftragWorkflow.IstEigentuemer(a, userId))
            .Where(a => status is null || a.Status == status)
            .Where(a => von is null || a.BestelltAm >= von.Value.Date)
            .Where(a => bis is null || a.BestelltAm < bis.Value.Date.AddDays(1))
            .OrderByDescending(a => a.Nummer)
            .Select(BfAuftragListRow.Von);
        var gefiltert = ColumnFilterHelper.Apply(rows, columnFilters, BfAuftragListRow.ColumnMap, ColumnFilterTypes.For("BfAuftraege/Index")).ToList();

        return View(new BfAuftragListViewModel
        {
            Rows = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Status = status, Von = von, Bis = bis, NurMeine = nurMeine, IstSchachtler = schachtler,
            HatWarenkorb = userId is not null && await _repo.GetEntwurfAsync(userId.Value) is { Positionen.Count: > 0 },
            Pagination = new PaginationState
            {
                CurrentPage = page, PageSize = effectivePageSize, PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault), TotalCount = gefiltert.Count,
            },
        });
    }

    /// <summary>Warenkorb des Benutzers (Entwurf); ohne Entwurf ein leerer.</summary>
    public async Task<IActionResult> Warenkorb(string? q)
    {
        var userId = _user.GetCurrentAppUserId();
        var entwurf = userId is null ? null : await _repo.GetEntwurfAsync(userId.Value);
        if (entwurf is not null) return RedirectToAction(nameof(Details), new { id = entwurf.Id, q });
        return View(nameof(Details), await DetailModelAsync(new BfAuftrag { Status = BfAuftragStatus.Entwurf, BestellerUserId = userId }, q));
    }

    public async Task<IActionResult> Details(int id, string? q)
    {
        var a = await _repo.GetDetailAsync(id);
        if (a is null) return NotFound();
        if (!await _user.CanSchachtelnAsync() && !BfAuftragWorkflow.IstEigentuemer(a, _user.GetCurrentAppUserId())) return Forbid();
        return View(await DetailModelAsync(a, q));
    }

    private async Task<BfAuftragDetailViewModel> DetailModelAsync(BfAuftrag a, string? q = null)
    {
        var schachtler = await _user.CanSchachtelnAsync();
        var eigentuemer = BfAuftragWorkflow.IstEigentuemer(a, _user.GetCurrentAppUserId());
        string? gesperrt = null;
        if (a.Status == BfAuftragStatus.Entwurf)
        {
            var gid = await _settings.GetIntValueAsync(AppSettingKeys.BfEmpfaengergruppeId, 0);
            if (gid <= 0 || await _groups.GetGroupByIdAsync(gid) is null)
                gesperrt = "Abschicken gesperrt: keine gültige Empfängergruppe für BF-Aufträge eingestellt (Einstellungen > Blechfertigung).";
        }
        var bearbeitbar = (a.Status == BfAuftragStatus.Entwurf && eigentuemer) || (a.Status == BfAuftragStatus.Bestellt && schachtler);
        // v1.99.42: Freigabe-Sperren zeigen das aktuelle Kuerzel (ungetrackt, nichts gespeichert)
        if (a.Status == BfAuftragStatus.Bestellt) a.BestellerKuerzel = await _repo.GetBenutzerKuerzelAsync(a.BestellerUserId);
        return new BfAuftragDetailViewModel
        {
            Suche = q,
            Treffer = a.Status == BfAuftragStatus.Entwurf && eigentuemer && !string.IsNullOrWhiteSpace(q) ? await _repo.SucheArtikelAsync(q, 30) : new(),
            Auftrag = a, IstEigentuemer = eigentuemer, IstSchachtler = schachtler, AbschickenGesperrt = gesperrt,
            Statuswerte = await _workflow.GetStatuswerteAsync(),
            Lagerplaetze = bearbeitbar ? (await _lagerplaetze.GetActiveOrderedExcludingPickingTransportAsync()).Select(l => l.Code).ToList() : new(),
        };
    }

    // ===================== Besteller =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(string articleNumber, string? menge, string? status, string? hauptlagerplatz) =>
        Weiter(await _workflow.HinzufuegenAsync(articleNumber, Menge(menge), status, hauptlagerplatz));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(int id, int positionId, string? menge, string? status, string? hauptlagerplatz, string? rowVersion) =>
        Weiter(await _workflow.PositionAendernAsync(id, positionId, Menge(menge), status, hauptlagerplatz, alsSchachtler: false, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int id, int positionId, string? rowVersion) =>
        Weiter(await _workflow.PositionEntfernenAsync(id, positionId, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, string? rowVersion) =>
        Weiter(await _workflow.AbschickenAsync(id, Rv(rowVersion)), id);

    // ===================== Schachtler =====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> UpdatePosition(int id, int positionId, string? menge, string? hauptlagerplatz, string? rowVersion) =>
        Weiter(await _workflow.PositionAendernAsync(id, positionId, Menge(menge), null, hauptlagerplatz, alsSchachtler: true, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> StreichePosition(int id, int positionId, bool gestrichen, string? rowVersion) =>
        Weiter(await _workflow.StreichenAsync(id, positionId, gestrichen, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> Freigeben(int id, string? rowVersion) =>
        Weiter(await _workflow.FreigebenAsync(id, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> CsvErneutSchreiben(int id, string? rowVersion) =>
        Weiter(await _workflow.CsvErneutSchreibenAsync(id, Rv(rowVersion)), id);

    /// <summary>Test-CSV herunterladen (Entscheidung B-1, Gerald 2026-11-17 18:44): derselbe Erzeuger wie der Dienst
    /// (<see cref="BfCsv.Inhalt"/>, <see cref="BfCsv.KopfAsync"/>, <see cref="BfCsv.Kodierung"/>), aber KEIN Statuswechsel und nichts in den
    /// Pool. Audit-Eintrag im Aktivitaets-Protokoll (<c>BfAuftragCsv</c>). Vor der Freigabe sind Termine und Bemerkung noch leer.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> TestCsv(int id)
    {
        var a = await _repo.GetMitPositionenAsync(id);
        if (a is null) { TempData["WarningMessage"] = "BF-Auftrag nicht gefunden."; return RedirectToAction(nameof(Index)); }
        if (a.Nummer is null)
        {
            TempData["WarningMessage"] = "Test-CSV erst nach dem Abschicken (der Entwurf hat noch keine BF-Nummer).";
            return RedirectToAction(nameof(Details), new { id });
        }
        byte[] bytes;
        try { bytes = BfCsv.Kodierung().GetBytes(BfCsv.Inhalt(a, await BfCsv.KopfAsync(_serviceSettings.GetValueAsync))); }
        catch (System.Text.EncoderFallbackException ex)
        {
            TempData["WarningMessage"] = $"Test-CSV von {a.Nummer} nicht erzeugt: Zeichen nicht in Windows-1252 ({ex.Message}).";
            return RedirectToAction(nameof(Details), new { id });
        }
        await using (var run = await _syncLogger.BeginRunAsync(SyncLogServices.BfAuftragCsv))
        {
            await run.LogInfoAsync($"Test-CSV {a.Nummer}.csv heruntergeladen durch {_user.GetDisplayName()} ({_user.GetWindowsUserName()}) – " +
                                   "kein Statuswechsel, nicht in den WiCAM-Pool geschrieben.", reference: a.Nummer);
            await run.FinishSuccessAsync(new Dictionary<string, int> { ["heruntergeladen"] = 1 });
        }
        return File(bytes, "text/csv; charset=windows-1252", a.Nummer + ".csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> FertigMelden(int id, string? rowVersion) =>
        Weiter(await _workflow.FertigMeldenAsync(new[] { id }, Rv(rowVersion)), id);

    /// <summary>Mehrfachauswahl aus der Liste; zurueck zur Liste.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> FertigMeldenListe(int[] ids)
    {
        var e = await _workflow.FertigMeldenAsync(ids);
        TempData[e.Ok ? "SuccessMessage" : "WarningMessage"] = e.Text;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Besteller bis zur Freigabe, Schachtler bis Fertig – die Regel prueft der Workflow (kein Schachtler-Filter, Besteller duerfen).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stornieren(int id, string? grund, string? rowVersion) =>
        Weiter(await _workflow.StornierenAsync(id, grund, await _user.CanSchachtelnAsync(), Rv(rowVersion)), id);

    // ===================== Hilfen =====================

    // Menge als Text binden und kulturunabhaengig lesen: der Default-Binder (de-DE) macht aus "40.000" 40000 und aus "1.5" 15
    // (fallstricke "Decimal/Culture-Bug in Form-Inputs"). Nicht lesbar = 0 -> Mengenregel meldet "Menge muss groesser als 0 sein".
    internal static decimal Menge(string? text) => BeschichtungsauftraegeController.TryMenge(text, out var m) ? m : 0;

    private static byte[]? Rv(string? b64)
    {
        if (string.IsNullOrEmpty(b64)) return null;
        try { return Convert.FromBase64String(b64); }
        catch (FormatException) { return null; }
    }

    private IActionResult Weiter(BfErgebnis e, int? fallbackId = null)
    {
        TempData[e.Ok ? "SuccessMessage" : "WarningMessage"] = e.Text;
        var id = e.AuftragId ?? fallbackId;
        return id is > 0 ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Warenkorb));
    }
}
