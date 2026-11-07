using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Debitoren-Stamm (v1.73.0, Spec 2026-11-06-sage-sdata-stammdaten-sync F10/E8b). Muster <see cref="CreditorsController"/> ohne
/// Beschichter-Filter und ohne Dienstleister-Meldung. Read/Edit-Split: Klasse <c>masterdata_read</c>, schreibende Actions <c>masterdata</c>.
/// Kein Loeschen, nur Deaktivieren. Bei Herkunft Sage sind Name, Adresse, Kontakt und Sage-Konto schreibgeschuetzt — entschieden am
/// GESPEICHERTEN Wert, nie am Formular.
/// </summary>
[RequireMasterDataReadAccess]
public class DebitorsController : Controller
{
    private static readonly string[] SageGefuehrteFelder =
    [
        nameof(Debitor.Name), nameof(Debitor.Street), nameof(Debitor.PostalCode), nameof(Debitor.City), nameof(Debitor.Country),
        nameof(Debitor.Email), nameof(Debitor.Phone), nameof(Debitor.ContactPerson), nameof(Debitor.SageKto), nameof(Debitor.Matchcode),
    ];

    private readonly IDebitorRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DebitorsController(IDebitorRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    /// <summary>Server-Spaltenfilter: Col-Key → gerenderter Zelltext.</summary>
    public static readonly Dictionary<string, Func<Debitor, string?>> ColumnMap = new()
    {
        ["short-name"] = d => d.ShortName,
        ["name"] = d => d.Name,
        ["matchcode"] = d => d.Matchcode,
        ["city"] = d => string.Join(" ", new[] { d.PostalCode, d.City }.Where(s => !string.IsNullOrWhiteSpace(s))),
        ["country"] = d => d.Country,
        ["contact"] = d => d.ContactPerson,
        ["phone"] = d => d.Phone,
        ["email"] = d => d.Email,
        ["sage-kto"] = d => d.SageKto,
        ["source"] = d => CreditorsController.SourceText(d.Source),
        ["status"] = d => d.IsActive ? "Aktiv" : "Inaktiv",
    };

    public static bool IstSageGefuehrt(Debitor d) => d.Source == StorageLocationSource.Sage;

    public async Task<IActionResult> Index(string? status = null, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        status = status is CreditorsController.StatusInaktiv or CreditorsController.StatusAlle ? status : CreditorsController.StatusAktiv;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var all = await _repository.GetAllAsync();
        IEnumerable<Debitor> query = status switch
        {
            CreditorsController.StatusInaktiv => all.Where(d => !d.IsActive),
            CreditorsController.StatusAlle => all,
            _ => all.Where(d => d.IsActive),
        };

        // Spaltenfilter nach der Filterkarte, vor der Pagination (ADR 0005).
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(query.AsQueryable(), columnFilters, ColumnMap, ColumnFilterTypes.For("Debitors/Index")).ToList();

        ViewBag.WmsEigene = all.Count(x => !IstSageGefuehrt(x)); // G6: melden, nicht loeschen
        ViewBag.Status = status;
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
        var debitor = await _repository.GetByIdAsync(id);
        return debitor == null ? NotFound() : View(debitor);
    }

    // G6 (Gerald 2026-11-07): Debitoren kommen aus Sage. Anlegen nur noch als Admin-Notfall — Konto in Sage noch nicht angelegt, der
    // Vorgang braucht den Eintrag aber jetzt; der Lese-Abgleich verknuepft ihn spaeter ueber das Sage-Konto (Herkunft wird dann Sage).
    [RequireMasterDataAccess]
    [RequireAdminAccess]
    public IActionResult Create() => View(new Debitor());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    [RequireAdminAccess]
    public async Task<IActionResult> Create(Debitor posted)
    {
        var debitor = new Debitor
        {
            Source = StorageLocationSource.Manual,
            SageKto = Normalize(posted.SageKto),
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName(),
        };
        ApplyStammFelder(debitor, posted);
        ApplyWmsFelder(debitor, posted);

        await ValidateUniqueAsync(debitor, null);
        if (!ModelState.IsValid)
            return View(debitor);

        await _repository.AddAsync(debitor);
        TempData["SuccessMessage"] = $"Debitor '{debitor.ShortName}' wurde angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var debitor = await _repository.GetByIdAsync(id);
        return debitor == null ? NotFound() : View(debitor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, Debitor posted)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        if (IstSageGefuehrt(existing))
        {
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
        TempData["SuccessMessage"] = $"Debitor '{existing.ShortName ?? existing.Name}' wurde gespeichert.";
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

        if (IstSageGefuehrt(existing))
        {
            // G6: Aktiv kommt bei Herkunft Sage aus Sage (der Abgleich wuerde es beim naechsten Lauf zuruecksetzen).
            TempData["WarningMessage"] = $"Debitor '{existing.ShortName ?? existing.Name}' stammt aus Sage – aktiv/inaktiv wird in Sage gepflegt.";
            return RedirectToAction(nameof(Details), new { id });
        }

        existing.IsActive = active;
        SetModified(existing);
        await _repository.UpdateAsync(existing);
        TempData["SuccessMessage"] = $"Debitor '{existing.ShortName ?? existing.Name}' wurde {(active ? "reaktiviert" : "deaktiviert")}.";
        return RedirectToAction(nameof(Index), new { status = active ? CreditorsController.StatusAktiv : CreditorsController.StatusInaktiv });
    }

    private async Task ValidateUniqueAsync(Debitor d, int? exceptId)
    {
        if (d.ShortName == null && !IstSageGefuehrt(d))
            ModelState.AddModelError(nameof(Debitor.ShortName), "Kurzname ist ein Pflichtfeld.");
        if (d.ShortName != null && await _repository.ExistsShortNameAsync(d.ShortName, exceptId))
            ModelState.AddModelError(nameof(Debitor.ShortName), $"Kurzname '{d.ShortName}' ist bereits vergeben (Groß-/Kleinschreibung zählt nicht).");
        if (d.SageKto != null && await _repository.ExistsSageKtoAsync(d.SageKto, exceptId))
            ModelState.AddModelError(nameof(Debitor.SageKto), $"Sage-Konto '{d.SageKto}' ist bereits einem anderen Debitor zugeordnet.");
    }

    private static void ApplyStammFelder(Debitor target, Debitor src)
    {
        target.Name = src.Name?.Trim() ?? string.Empty;
        target.Street = Normalize(src.Street);
        target.PostalCode = Normalize(src.PostalCode);
        target.City = Normalize(src.City);
        target.Country = Normalize(src.Country);
        target.Email = Normalize(src.Email);
        target.Phone = Normalize(src.Phone);
        target.ContactPerson = Normalize(src.ContactPerson);
        target.Matchcode = Normalize(src.Matchcode);
    }

    private static void ApplyWmsFelder(Debitor target, Debitor src)
    {
        target.ShortName = Normalize(src.ShortName);
        target.Remark = Normalize(src.Remark);
    }

    private void SetModified(Debitor d)
    {
        d.ModifiedAt = DateTime.Now;
        d.ModifiedBy = _currentUserService.GetDisplayName();
        d.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    }

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
