using System.Globalization;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
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
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IFaHierarchyNodeRepository _faNodes;
    private readonly IProductionWorkplaceRepository _workplaces;

    public OperationConfigsController(IOperationConfigRepository repo, ICurrentUserService currentUser, IServiceSettingRepository serviceSettings,
        IFaHierarchyNodeRepository faNodes, IProductionWorkplaceRepository workplaces)
    {
        _repo = repo;
        _currentUser = currentUser;
        _serviceSettings = serviceSettings;
        _faNodes = faNodes;
        _workplaces = workplaces;
    }

    /// <summary>Hoechstlaenge der Sage-Arbeitsgangnummer (Sage-Stamm string(31), View varchar(31) laut P-AG1).</summary>
    public const int SageArbeitsgangnummerMaxLength = 31;

    /// <summary>Pruefliste (a): sichtbare Kappung. ponytail: Kappung 50, voller Export erst bei Bedarf.</summary>
    public const int PrueflisteMax = 50;

    /// <summary>Quelle wie angezeigt (Spec 2026-10-16 FA 6); unbekannter Wert roh, nicht still „Manuell".</summary>
    public static string FormatSource(string? source) => source switch
    {
        OperationConfigSource.Sage => "Sage",
        OperationConfigSource.ExternalSystem => "Fremdsystem",
        OperationConfigSource.Manual => "Manuell",
        _ => source ?? string.Empty,
    };

    /// <summary>„Nicht in Sage" (FA 4): gesetzter Zeitstempel und nie bei Quelle Fremdsystem. Gilt nur bei eingeschaltetem Abgleich.</summary>
    public static bool IsNotInSage(OperationConfig c) => c.SageMissingSince != null && c.Source != OperationConfigSource.ExternalSystem;

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
            ["source"] = c => FormatSource(c.Source),
            ["sageag"] = c => c.WirksameSageArbeitsgangnummer,
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
        // Schalter aus -> keine Kennzeichnung, auch wenn alte Zeitstempel stehen (AKE-Regression, AK 16).
        var markerActive = string.Equals(await _serviceSettings.GetValueAsync("Sync:OperationConfigSyncEnabled"), "true", StringComparison.OrdinalIgnoreCase);
        // Pruefliste nur bei hierarchischen FAs (IDEAL, Rueckfrage 7): an der AKE gaebe es keine Strukturknoten, (b) waere nur Rauschen.
        var prueflisteAktiv = string.Equals(await _serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master), "true", StringComparison.OrdinalIgnoreCase);
        Pruefliste? pruefliste = null;
        if (prueflisteAktiv)
        {
            const string ausschlussKey = "Sync:ProductionWorkplaceSyncAusschlussliste";
            var ausschluss = (await _serviceSettings.GetValueAsync(ausschlussKey)
                    ?? ServiceSettingDefinitions.All.First(d => d.Key == ausschlussKey).DefaultValue ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) // wie der Dienst
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            pruefliste = BuildPruefliste((await _faNodes.GetAllAsync()).Select(n => n.Arbeitsschritte), all,
                await _workplaces.GetAllOrderedAsync(), ausschluss);
        }

        return View(new OperationConfigIndexViewModel
        {
            Configs = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Unconfigured = await _repo.GetUnconfiguredOperationsAsync(),
            DuplicateNames = all.GroupBy(c => c.OperationName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1).Select(g => string.Join(" / ", g.Select(c => c.OperationName))).ToList(),
            MarkerActive = markerActive,
            NotInSage = markerActive
                ? all.Where(IsNotInSage).OrderBy(c => c.OperationName, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<OperationConfig>(),
            Abweichungen = all.Where(c => c.SageArbeitsgangnummerWeichtAb).OrderBy(c => c.OperationName, StringComparer.OrdinalIgnoreCase).ToList(),
            Pruefliste = pruefliste,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = filtered.Count,
            },
        });
    }

    /// <summary>Hinweis, wenn der Offset im POST fehlt/leer/nicht numerisch ist (FA 4a Teil 2): nichts speichern — ein still
    /// gespeicherter Offset 0 verschoebe ab FA-Struktur Teil 2 den Soll-Termin und damit „überfällig“ unbemerkt.</summary>
    public const string OffsetFehlt = "Offset (Arbeitstage) fehlt – nichts gespeichert.";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string? operationName, string? displayName, int? dueDateOffsetDays, bool isExternalSystemRelevant)
    {
        var name = (operationName ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            TempData["WarningMessage"] = "Kürzel darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }
        if (name.Length > 100 || (displayName?.Trim().Length ?? 0) > 200)
        {
            TempData["WarningMessage"] = "Kürzel höchstens 100, Anzeigename höchstens 200 Zeichen.";
            return RedirectToAction(nameof(Index));
        }
        if (dueDateOffsetDays is not int offset)
        {
            TempData["WarningMessage"] = OffsetFehlt;
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
            DueDateOffsetDays = offset,
            IsExternalSystemRelevant = isExternalSystemRelevant,
            Source = OperationConfigSource.Manual,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUser.GetDisplayName(),
            CreatedByWindows = _currentUser.GetWindowsUserName(),
        });
        TempData["SuccessMessage"] = $"Arbeitsgang „{name}“ angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? displayName, int? dueDateOffsetDays, bool isExternalSystemRelevant,
        string? sageArbeitsgangnummerManuell = null)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config == null) return NotFound();
        if ((displayName?.Trim().Length ?? 0) > 200)
        {
            TempData["WarningMessage"] = "Anzeigename höchstens 200 Zeichen.";
            return RedirectToAction(nameof(Index));
        }
        // Handwert (FA-5): das Inline-Formular postet das Feld immer; leer = Handwert loeschen (dann wirkt wieder der Sage-Wert).
        var hand = string.IsNullOrWhiteSpace(sageArbeitsgangnummerManuell) ? null : sageArbeitsgangnummerManuell.Trim();
        if ((hand?.Length ?? 0) > SageArbeitsgangnummerMaxLength)
        {
            TempData["WarningMessage"] = $"Sage-AG (Hand) höchstens {SageArbeitsgangnummerMaxLength} Zeichen – nichts gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        if (dueDateOffsetDays is not int offset)
        {
            TempData["WarningMessage"] = OffsetFehlt; // auch Anzeigename/Relevanz/Audit bleiben unveraendert
            return RedirectToAction(nameof(Index));
        }

        config.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        config.DueDateOffsetDays = offset;
        config.IsExternalSystemRelevant = isExternalSystemRelevant;
        config.SageArbeitsgangnummerManuell = hand; // der Sage-Wert gehoert dem Abgleich (Repository schreibt ihn nicht)
        config.ModifiedAt = DateTime.Now;
        config.ModifiedBy = _currentUser.GetDisplayName();
        config.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _repo.UpdateAsync(config);
        TempData["SuccessMessage"] = $"Arbeitsgang „{config.OperationName}“ gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Pruefliste der Sage-Zuordnung (FA-6, reine Anzeige, ohne DB): (a) Kuerzel aus FA-Strukturknoten ohne Arbeitsgang-Eintrag bzw. ohne
    /// wirksame Sage-Arbeitsgangnummer (Trennregel <see cref="FaArbeitsschrittSymbole.Tokenize"/> wie der Dienst, ohne Ausschlussliste),
    /// (b) Werkbaenke ohne Sage-Arbeitsplatz, (b2) Werkbaenke, deren Nummer einer wirksamen Sage-Arbeitsgangnummer gleicht (P-AG3/P-AG5:
    /// die Werkbank-Anlage aus vw_IDEAL_AKE_WMS_Arbeitsgaenge schreibt Arbeitsgang-, keine Arbeitsplatznummern).
    /// </summary>
    internal static Pruefliste BuildPruefliste(IEnumerable<string?> arbeitsschritte, IReadOnlyList<OperationConfig> configs,
        IReadOnlyList<ProductionWorkplace> werkbaenke, IReadOnlySet<string> ausschluss)
    {
        var byName = SageFaArbeitsgaenge.KuerzelLookup(configs); // kleinste Id gewinnt (wie Offsets); dieselbe Regel nutzt die Sage-AG-Vorschau

        var ohneAg = arbeitsschritte
            .SelectMany(a => FaArbeitsschrittSymbole.Tokenize(a).Distinct(StringComparer.OrdinalIgnoreCase))
            .Where(t => !ausschluss.Contains(t))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Kuerzel: g.Key, Knoten: g.Count(), Config: byName.GetValueOrDefault(g.Key)))
            .Where(x => x.Config?.WirksameSageArbeitsgangnummer == null)
            .Select(x => new KuerzelOhneSageAg(x.Kuerzel, x.Config == null ? "nicht in Arbeitsgang-Stammdaten" : "ohne Sage-AG", x.Knoten))
            .OrderByDescending(x => x.Knoten).ThenBy(x => x.Kuerzel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var agNummern = configs.Where(c => c.WirksameSageArbeitsgangnummer != null)
            .GroupBy(c => c.WirksameSageArbeitsgangnummer!.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(c => c.OperationName)), StringComparer.Ordinal);

        return new Pruefliste(
            ohneAg.Take(PrueflisteMax).ToList(),
            ohneAg.Count,
            werkbaenke.Where(w => string.IsNullOrWhiteSpace(w.SageArbeitsplatznummer)).ToList(),
            werkbaenke.Where(w => !string.IsNullOrWhiteSpace(w.SageArbeitsplatznummer) && agNummern.ContainsKey(w.SageArbeitsplatznummer.Trim()))
                .Select(w => new WerkbankNummerGleichAg(w, agNummern[w.SageArbeitsplatznummer!.Trim()]))
                .ToList());
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
