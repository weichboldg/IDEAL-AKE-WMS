using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
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

    public BfAuftraegeController(IBfAuftragRepository repo, BfAuftragWorkflow workflow, IAppSettingRepository settings,
        IOrderRecipientRepository groups, IStorageLocationRepository lagerplaetze, ICurrentUserService user)
    {
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
    public async Task<IActionResult> AddItem(string articleNumber, decimal menge, string? status, string? hauptlagerplatz) =>
        Weiter(await _workflow.HinzufuegenAsync(articleNumber, menge, status, hauptlagerplatz));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(int id, int positionId, decimal menge, string? status, string? hauptlagerplatz, string? rowVersion) =>
        Weiter(await _workflow.PositionAendernAsync(id, positionId, menge, status, hauptlagerplatz, alsSchachtler: false, Rv(rowVersion)), id);

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
    public async Task<IActionResult> UpdatePosition(int id, int positionId, decimal menge, string? hauptlagerplatz, string? rowVersion) =>
        Weiter(await _workflow.PositionAendernAsync(id, positionId, menge, null, hauptlagerplatz, alsSchachtler: true, Rv(rowVersion)), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireSchachtlerAccess]
    public async Task<IActionResult> StreichePosition(int id, int positionId, bool gestrichen, string? rowVersion) =>
        Weiter(await _workflow.StreichenAsync(id, positionId, gestrichen, Rv(rowVersion)), id);

    // ===================== Hilfen =====================

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
