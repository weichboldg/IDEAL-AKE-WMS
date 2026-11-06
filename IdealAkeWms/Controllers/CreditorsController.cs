using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Kreditoren-Stamm (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10/E8a + Anhang 2026-10-06-kreditoren-stammdaten).
/// Read/Edit-Split wie <see cref="ProductionWorkplacesController"/>: Klasse <c>masterdata_read</c>, schreibende Actions <c>masterdata</c>.
/// Kein Loeschen, nur Deaktivieren. Bei Herkunft Sage sind Name, Adresse, Kontakt und Sage-Konto schreibgeschuetzt — entschieden am
/// GESPEICHERTEN Wert, nie am Formular.
/// </summary>
[RequireMasterDataReadAccess]
public class CreditorsController : Controller
{
    public const string StatusAktiv = "aktiv", StatusInaktiv = "inaktiv", StatusAlle = "alle";

    /// <summary>Formularfelder, die bei Herkunft Sage nicht pflegbar sind (ModelState wird fuer sie ignoriert).</summary>
    private static readonly string[] SageGefuehrteFelder =
    [
        nameof(Creditor.Name), nameof(Creditor.Street), nameof(Creditor.PostalCode), nameof(Creditor.City), nameof(Creditor.Country),
        nameof(Creditor.Email), nameof(Creditor.Phone), nameof(Creditor.ContactPerson), nameof(Creditor.SageKto),
    ];

    private readonly ICreditorRepository _repository;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly ICurrentUserService _currentUserService;

    public CreditorsController(ICreditorRepository repository, IFaHierarchyOrderInfoRepository orderInfos, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _orderInfos = orderInfos;
        _currentUserService = currentUserService;
    }

    /// <summary>Server-Spaltenfilter: Col-Key → gerenderter Zelltext (Badges „Ja“/„Sage“/„WMS“/„Aktiv“/„Inaktiv“ wie in der View).</summary>
    public static readonly Dictionary<string, Func<Creditor, string?>> ColumnMap = new()
    {
        ["short-name"] = c => c.ShortName,
        ["name"] = c => c.Name,
        ["matchcode"] = c => c.Matchcode,
        ["city"] = c => string.Join(" ", new[] { c.PostalCode, c.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
        ["country"] = c => c.Country,
        ["contact"] = c => c.ContactPerson,
        ["phone"] = c => c.Phone,
        ["email"] = c => c.Email,
        ["sage-kto"] = c => c.SageKto,
        ["coater"] = c => c.IsCoater ? "Ja" : "",
        ["source"] = c => SourceText(c.Source),
        ["status"] = c => c.IsActive ? "Aktiv" : "Inaktiv",
    };

    public static string SourceText(string source) => source == StorageLocationSource.Sage ? "Sage" : "WMS";

    public static bool IstSageGefuehrt(Creditor c) => c.Source == StorageLocationSource.Sage;

    public async Task<IActionResult> Index(string? status = null, bool coater = false, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        status = status is StatusInaktiv or StatusAlle ? status : StatusAktiv;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var all = await _repository.GetAllAsync();
        IEnumerable<Creditor> query = status switch
        {
            StatusInaktiv => all.Where(c => !c.IsActive),
            StatusAlle => all,
            _ => all.Where(c => c.IsActive),
        };
        if (coater)
            query = query.Where(c => c.IsCoater);

        // Spaltenfilter nach der Filterkarte, vor der Pagination (ADR 0005).
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(query.AsQueryable(), columnFilters, ColumnMap, ColumnFilterTypes.For("Creditors/Index")).ToList();

        // Dienstleister-Meldung ueber ALLE Kreditoren (unabhaengig von Filterkarte/Spaltenfilter); bei AKE ist die Quelle leer.
        ViewBag.Unassigned = CreditorAssignment.FindUnassigned(await _orderInfos.GetAllAsync(), all);
        ViewBag.Status = status;
        ViewBag.Coater = coater;
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = filtered.Count
        };
        return View(filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var creditor = await _repository.GetByIdAsync(id);
        return creditor == null ? NotFound() : View(creditor);
    }

    [RequireMasterDataAccess]
    public IActionResult Create(string? shortName = null) =>
        View(new Creditor { ShortName = Normalize(shortName), IsCoater = !string.IsNullOrWhiteSpace(shortName) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(Creditor posted)
    {
        var creditor = new Creditor
        {
            Source = StorageLocationSource.Manual,
            SageKto = Normalize(posted.SageKto),
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName(),
        };
        ApplyStammFelder(creditor, posted);
        ApplyWmsFelder(creditor, posted);
        creditor.IsActive = true;

        await ValidateUniqueAsync(creditor, null);
        if (!ModelState.IsValid)
            return View(creditor);

        await _repository.AddAsync(creditor);
        TempData["SuccessMessage"] = $"Kreditor '{creditor.ShortName}' wurde angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var creditor = await _repository.GetByIdAsync(id);
        return creditor == null ? NotFound() : View(creditor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, Creditor posted)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (IstSageGefuehrt(existing))
        {
            // Sage fuehrt diese Felder (Lese-Abgleich E8c) — Formularwerte zaehlen nicht, also auch nicht ihre Validierung.
            foreach (var feld in SageGefuehrteFelder)
                ModelState.Remove(feld);
        }
        else
        {
            ApplyStammFelder(existing, posted);
            existing.SageKto = Normalize(posted.SageKto);
        }
        ApplyWmsFelder(existing, posted);

        await ValidateUniqueAsync(existing, id);
        if (!ModelState.IsValid)
            return View(existing);

        SetModified(existing);
        await _repository.UpdateAsync(existing);
        TempData["SuccessMessage"] = $"Kreditor '{existing.ShortName ?? existing.Name}' wurde gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.IsActive = active;
        SetModified(existing);
        await _repository.UpdateAsync(existing);
        TempData["SuccessMessage"] = $"Kreditor '{existing.ShortName ?? existing.Name}' wurde {(active ? "reaktiviert" : "deaktiviert")}.";
        return RedirectToAction(nameof(Index), new { status = active ? StatusAktiv : StatusInaktiv });
    }

    /// <summary>Kurzname Pflicht bei Herkunft WMS; Kurzname (Gross/Klein egal) und Sage-Konto eindeutig — App-Layer-Guard,
    /// der gefilterte UNIQUE-Index ist die zweite Linie (InMemory erzwingt ihn nicht).</summary>
    private async Task ValidateUniqueAsync(Creditor c, int? exceptId)
    {
        if (c.ShortName == null && !IstSageGefuehrt(c))
            ModelState.AddModelError(nameof(Creditor.ShortName), "Kurzname ist ein Pflichtfeld.");
        if (c.ShortName != null && await _repository.ExistsShortNameAsync(c.ShortName, exceptId))
            ModelState.AddModelError(nameof(Creditor.ShortName), $"Kurzname '{c.ShortName}' ist bereits vergeben (Groß-/Kleinschreibung zählt nicht).");
        if (c.SageKto != null && await _repository.ExistsSageKtoAsync(c.SageKto, exceptId))
            ModelState.AddModelError(nameof(Creditor.SageKto), $"Sage-Konto '{c.SageKto}' ist bereits einem anderen Kreditor zugeordnet.");
    }

    private static void ApplyStammFelder(Creditor target, Creditor src)
    {
        target.Name = src.Name?.Trim() ?? string.Empty;
        target.Street = Normalize(src.Street);
        target.PostalCode = Normalize(src.PostalCode);
        target.City = Normalize(src.City);
        target.Country = Normalize(src.Country);
        target.Email = Normalize(src.Email);
        target.Phone = Normalize(src.Phone);
        target.ContactPerson = Normalize(src.ContactPerson);
    }

    private static void ApplyWmsFelder(Creditor target, Creditor src)
    {
        target.ShortName = Normalize(src.ShortName);
        target.Matchcode = Normalize(src.Matchcode);
        target.Remark = Normalize(src.Remark);
        target.IsCoater = src.IsCoater;
    }

    private void SetModified(Creditor c)
    {
        c.ModifiedAt = DateTime.Now;
        c.ModifiedBy = _currentUserService.GetDisplayName();
        c.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    }

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
