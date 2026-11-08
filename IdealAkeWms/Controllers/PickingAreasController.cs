using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E1): Stammdaten „Kommissionierbereiche“ und Zuordnungsregeln (ADR 0020:
/// Regeltyp = feste Auswahl, Wert = Auswahl aus Stammdaten, der Server prueft). Lesen <c>masterdata_read</c>, Pflege <c>masterdata</c>/<c>admin</c>.
/// </summary>
[RequireMasterDataReadAccess]
public class PickingAreasController : Controller
{
    private readonly IPickingAreaRepository _repo;
    private readonly ICurrentUserService _user;

    public PickingAreasController(IPickingAreaRepository repo, ICurrentUserService user)
    {
        _repo = repo;
        _user = user;
    }

    // ---------------- Bereiche ----------------

    public async Task<IActionResult> Index()
    {
        // Kleine, unpaginierte Stammdatenliste → Client-Spaltenfilter (ADR 0005).
        ViewBag.CanEdit = await _user.HasMasterDataAccessAsync();
        return View(await _repo.GetAllAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, RequireMasterDataAccess]
    public async Task<IActionResult> Create(PickingAreaEingabe e)
    {
        var alle = await _repo.GetAllAsync();
        var area = new PickingArea { IsActive = true, CreatedAt = DateTime.Now, CreatedBy = _user.GetDisplayName(), CreatedByWindows = _user.GetWindowsUserName() };
        var fehler = Uebernehmen(area, e with { IsActive = true, IsDefault = false }, alle);
        if (fehler.Count > 0)
        {
            TempData["WarningMessage"] = "Bereich nicht angelegt: " + string.Join(" ", fehler);
            return RedirectToAction(nameof(Index));
        }
        _repo.Add(area);
        await _repo.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Bereich „{area.Name}“ angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var area = await _repo.GetByIdAsync(id);
        if (area == null) return NotFound();
        return View(new PickingAreaEingabe(area.Name, area.Kuerzel, area.Reihenfolge, area.IsActive, area.IsDefault, area.IstVorkommissionierung) { Id = id });
    }

    [HttpPost, ValidateAntiForgeryToken, RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, PickingAreaEingabe e)
    {
        var alle = await _repo.GetAllAsync();
        var area = alle.FirstOrDefault(a => a.Id == id);
        if (area == null) return NotFound();
        var fehler = Uebernehmen(area, e, alle);
        if (fehler.Count > 0)
        {
            foreach (var f in fehler) ModelState.AddModelError("", f);
            return View(e with { Id = id });
        }
        var now = DateTime.Now;
        Audit(area, now);
        // v1.89.0 (Entscheidung Q10): ein zweiter Standard wird abgelehnt (Uebernehmen), es gibt also nie zwei – ein Speichern = eine
        // Transaktion (SaveChanges), die Zwei-Schritt-Logik aus Q2/Q13 entfaellt. Gewechselt wird ueber „abwaehlen“ + „neu setzen“.
        await _repo.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Bereich „{area.Name}“ gespeichert.";
        if (!alle.Any(a => a.IsDefault && a.IsActive))
            TempData["WarningMessage"] = "Es gibt jetzt keinen aktiven Standardbereich. Bis einer gesetzt ist, legt die Kommissionierung keine Aufträge an " +
                                         "(Meldung „Kein aktiver Standardbereich“).";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Prueft und uebernimmt die Eingabe (ein Ort fuer Anlegen und Bearbeiten); Rueckgabe = Fehlertexte, leer = ok.</summary>
    internal static List<string> Uebernehmen(PickingArea area, PickingAreaEingabe e, IReadOnlyCollection<PickingArea> alle)
    {
        var fehler = new List<string>();
        var name = e.Name?.Trim() ?? "";
        var kuerzel = e.Kuerzel?.Trim() ?? "";
        if (name.Length == 0) fehler.Add("Name ist Pflicht.");
        else if (name.Length > 100) fehler.Add("Name ist länger als 100 Zeichen.");
        else if (alle.Any(a => a.Id != area.Id && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
            fehler.Add($"Name „{name}“ ist schon vergeben.");
        if (kuerzel.Length == 0) fehler.Add("Kürzel ist Pflicht.");
        else if (kuerzel.Length > 10) fehler.Add("Kürzel ist länger als 10 Zeichen.");
        else if (alle.Any(a => a.Id != area.Id && string.Equals(a.Kuerzel, kuerzel, StringComparison.OrdinalIgnoreCase)))
            fehler.Add($"Kürzel „{kuerzel}“ ist schon vergeben.");
        if (e.Reihenfolge < 0) fehler.Add("Reihenfolge muss 0 oder größer sein.");
        if (e.IsDefault && !e.IsActive) fehler.Add("Der Standardbereich muss aktiv sein.");
        // v1.89.0 (Entscheidung Q10, AK 2): ablehnen statt abloesen
        if (e.IsDefault && alle.FirstOrDefault(a => a.Id != area.Id && a.IsDefault) is { } standard)
            fehler.Add($"Es gibt bereits einen Standardbereich („{standard.Name}“). Zuerst dort abwählen.");
        if (fehler.Count > 0) return fehler;

        area.Name = name;
        area.Kuerzel = kuerzel;
        area.Reihenfolge = e.Reihenfolge;
        area.IsActive = e.IsActive;
        area.IsDefault = e.IsDefault;
        area.IstVorkommissionierung = e.IstVorkommissionierung;
        return fehler;
    }

    // ---------------- Regeln ----------------

    public async Task<IActionResult> Regeln(int? bereichId, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var regeln = await _repo.GetRulesAsync();
        if (bereichId.HasValue) regeln = regeln.Where(r => r.PickingAreaId == bereichId).ToList();
        var filter = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var gefiltert = ColumnFilterHelper.Apply(regeln, filter, RegelColumnMap, ColumnFilterTypes.For("PickingAreas/Regeln")).ToList();
        var userDefault = await _user.GetDefaultPageSizeAsync();
        var size = Services.PageSize.Resolve(pageSize, userDefault);
        return View(new PickingAreaRegelnViewModel
        {
            Regeln = gefiltert.Skip((page - 1) * size).Take(size).ToList(),
            BereichId = bereichId,
            Bereiche = await _repo.GetAllAsync(),
            Kommissionierziele = await _repo.GetKommissionierzieleAsync(),
            Lagerorte = await _repo.GetLagerorteAsync(),
            Artikelgruppen = await _repo.GetArtikelgruppenAsync(),
            CanEdit = await _user.HasMasterDataAccessAsync(),
            Pagination = new PaginationState
            {
                CurrentPage = page, PageSize = size, PageSizeRaw = Services.PageSize.ResolveRaw(pageSize, userDefault), TotalCount = gefiltert.Count,
            },
        });
    }

    internal static readonly Dictionary<string, Func<PickingAreaRule, string?>> RegelColumnMap = new()
    {
        ["typ"] = r => KommissionierbereichRegel.Anzeige(r.RuleType),
        ["wert"] = r => r.Value,
        ["bereich"] = r => r.PickingArea?.Name,
    };

    [HttpPost, ValidateAntiForgeryToken, RequireMasterDataAccess]
    public async Task<IActionResult> RegelAnlegen(int pickingAreaId, PickingAreaRuleType ruleType, int? articleId, string? wert, int? bereichId)
    {
        var fehler = await RegelPruefenAsync(pickingAreaId, ruleType, articleId, wert);
        if (fehler.Fehler != null)
        {
            TempData["WarningMessage"] = "Regel nicht angelegt: " + fehler.Fehler;
            return RedirectToAction(nameof(Regeln), new { bereichId });
        }
        _repo.AddRule(new PickingAreaRule
        {
            PickingAreaId = pickingAreaId, RuleType = ruleType, Value = fehler.Wert!,
            CreatedAt = DateTime.Now, CreatedBy = _user.GetDisplayName(), CreatedByWindows = _user.GetWindowsUserName(),
        });
        await _repo.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Regel {KommissionierbereichRegel.Anzeige(ruleType)} „{fehler.Wert}“ angelegt.";
        return RedirectToAction(nameof(Regeln), new { bereichId });
    }

    /// <summary>Server-Pruefung einer Regel: Bereich existiert, Typ aus der festen Liste, Wert existiert im jeweiligen Stamm, (Typ, Wert) eindeutig.</summary>
    internal async Task<(string? Wert, string? Fehler)> RegelPruefenAsync(int pickingAreaId, PickingAreaRuleType ruleType, int? articleId, string? wert)
    {
        if (await _repo.GetByIdAsync(pickingAreaId) == null) return (null, "Bereich unbekannt.");
        if (!Enum.IsDefined(ruleType)) return (null, "Regeltyp unbekannt.");
        string? v = ruleType switch
        {
            PickingAreaRuleType.Artikel => articleId is { } id ? await _repo.GetArticleNumberAsync(id) : null,
            _ => wert?.Trim(),
        };
        if (string.IsNullOrEmpty(v)) return (null, ruleType == PickingAreaRuleType.Artikel ? "Artikel wählen." : "Wert wählen.");
        var imStamm = ruleType switch
        {
            PickingAreaRuleType.Artikel => true, // aus der Artikel-Id gelesen
            PickingAreaRuleType.Kommissionierziel => (await _repo.GetKommissionierzieleAsync()).Contains(v, StringComparer.OrdinalIgnoreCase),
            PickingAreaRuleType.Lagerort => (await _repo.GetLagerorteAsync()).Contains(v, StringComparer.OrdinalIgnoreCase),
            _ => (await _repo.GetArtikelgruppenAsync()).Contains(v, StringComparer.OrdinalIgnoreCase),
        };
        if (!imStamm) return (null, $"„{v}“ kommt als {KommissionierbereichRegel.Anzeige(ruleType)} im Stamm nicht vor.");
        var doppelt = (await _repo.GetRulesAsync()).FirstOrDefault(r => r.RuleType == ruleType
            && KommissionierbereichRegel.Norm(r.Value) == KommissionierbereichRegel.Norm(v));
        if (doppelt != null) return (null, $"{KommissionierbereichRegel.Anzeige(ruleType)} „{v}“ ist schon dem Bereich {doppelt.PickingArea.Name} zugeordnet.");
        return (v, null);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireMasterDataAccess]
    public async Task<IActionResult> RegelLoeschen(int id, int? bereichId)
    {
        var regel = await _repo.GetRuleAsync(id);
        if (regel == null) return NotFound();
        _repo.RemoveRule(regel);
        await _repo.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Regel „{regel.Value}“ gelöscht.";
        return RedirectToAction(nameof(Regeln), new { bereichId });
    }

    private void Audit(PickingArea a, DateTime now)
    {
        a.ModifiedAt = now;
        a.ModifiedBy = _user.GetDisplayName();
        a.ModifiedByWindows = _user.GetWindowsUserName();
    }
}
