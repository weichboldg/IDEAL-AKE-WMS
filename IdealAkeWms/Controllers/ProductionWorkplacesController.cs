using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class ProductionWorkplacesController : Controller
{
    private readonly IProductionWorkplaceRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _appSettings;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ApplicationDbContext _ctx;

    public ProductionWorkplacesController(
        IProductionWorkplaceRepository repository,
        IUserRepository userRepository,
        IWorkStepRepository workStepRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository appSettings,
        IServiceSettingRepository serviceSettings,
        ApplicationDbContext ctx)
    {
        _repository = repository;
        _userRepository = userRepository;
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
        _appSettings = appSettings;
        _serviceSettings = serviceSettings;
        _ctx = ctx;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (BDE-Badge "Aktiv"/"Inaktiv", Benutzer als Anzahl + Namen-Liste bzw. Gedankenstrich,
    /// Vorkommissioniertage als "X Tage"-Badge bzw. "Standard").
    /// </summary>
    private static readonly Dictionary<string, Func<ProductionWorkplace, string?>> ColumnMap = new()
    {
        ["name"] = wp => wp.Name,
        ["sage-nr"] = wp => wp.SageArbeitsplatznummer,
        ["arbeitsschritt"] = wp => wp.ArbeitsschrittCode,
        ["hall"] = wp => wp.Hall,
        ["bde"] = wp => (wp.BdeAktiv ? "Aktiv" : "Inaktiv") + (wp.BdeNurAgFertigmeldung ? " nur Fertigmeldung" : "")
                     + (wp.BdeTeileliste ? " Teileliste" : ""),
        ["sage-missing"] = wp => wp.SageMissingSince?.ToString("dd.MM.yyyy"),
        ["users"] = wp => wp.ProductionWorkplaceUsers.Any()
            ? $"{wp.ProductionWorkplaceUsers.Count} {string.Join(", ", wp.ProductionWorkplaceUsers.Select(wu => wu.User.Name))}"
            : "—",
        ["pre-picking-days"] = wp => wp.OverridePrePickingDays.HasValue
            ? $"{wp.OverridePrePickingDays} Tage"
            : "Standard",
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var workplaces = await _repository.GetAllWithUsersOrderedAsync();

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var list = ColumnFilterHelper.Apply(workplaces, columnFilters, ColumnMap, ColumnFilterTypes.For("ProductionWorkplaces/Index")).ToList();
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = list.Count
        };
        return View(list.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Create()
    {
        var vm = new ProductionWorkplaceEditViewModel
        {
            AvailableUsers = await _userRepository.GetActiveUsersAsync(),
            AllWorkSteps = await _workStepRepository.GetActiveAsync()
        };
        await SetFormViewBagAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(ProductionWorkplaceEditViewModel vm, int[]? workStepIds = null)
    {
        await ValidateTeilelisteAsync(vm, vm.BdeAktiv, NormalizeCode(vm.ArbeitsschrittCode));
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            vm.AllWorkSteps = await _workStepRepository.GetActiveAsync();
            vm.SelectedWorkStepIds = workStepIds?.ToList() ?? new List<int>();
            await SetFormViewBagAsync();
            return View(vm);
        }

        var workplace = new ProductionWorkplace
        {
            Name = vm.Name,
            Hall = vm.Hall,
            OverridePrePickingDays = vm.OverridePrePickingDays,
            BdeAktiv = vm.BdeAktiv,
            BdeDefaultArbeitsgang = string.IsNullOrWhiteSpace(vm.BdeDefaultArbeitsgang)
                ? null
                : vm.BdeDefaultArbeitsgang.Trim(),
            // Create setzt nie eine Sage-Nummer -> Kuerzel immer pflegbar (FA-a8)
            ArbeitsschrittCode = NormalizeCode(vm.ArbeitsschrittCode),
            BdeNurAgFertigmeldung = vm.BdeNurAgFertigmeldung,
            BdeTeileliste = vm.BdeTeileliste,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _repository.AddAsync(workplace);

        if (vm.SelectedUserIds is { Count: > 0 })
        {
            await _repository.SetProductionWorkplaceUsersAsync(
                workplace.Id,
                vm.SelectedUserIds,
                _currentUserService.GetDisplayName(),
                _currentUserService.GetWindowsUserName());
        }

        if (workStepIds is { Length: > 0 })
        {
            await _repository.SetWorkStepsAsync(
                workplace.Id,
                workStepIds.ToList(),
                _currentUserService.GetDisplayName(),
                _currentUserService.GetWindowsUserName());
        }

        TempData["SuccessMessage"] = $"Werkbank '{workplace.Name}' wurde angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var workplace = await _repository.GetByIdWithUsersAsync(id);
        if (workplace == null)
            return NotFound();

        var vm = new ProductionWorkplaceEditViewModel
        {
            Id = workplace.Id,
            Name = workplace.Name,
            Hall = workplace.Hall,
            OverridePrePickingDays = workplace.OverridePrePickingDays,
            BdeAktiv = workplace.BdeAktiv,
            BdeDefaultArbeitsgang = workplace.BdeDefaultArbeitsgang,
            BdeUseCustomShiftPlan = workplace.BdeUseCustomShiftPlan,
            SageArbeitsplatznummer = workplace.SageArbeitsplatznummer,
            ArbeitsschrittCode = workplace.ArbeitsschrittCode,
            BdeNurAgFertigmeldung = workplace.BdeNurAgFertigmeldung,
            BdeTeileliste = workplace.BdeTeileliste,
            SageMissingSince = workplace.SageMissingSince,
            CustomShifts = await _ctx.BdeShifts
                .Where(s => s.ProductionWorkplaceId == workplace.Id)
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .ToListAsync(),
            SelectedUserIds = workplace.ProductionWorkplaceUsers.Select(wu => wu.UserId).ToList(),
            AvailableUsers = await _userRepository.GetActiveUsersAsync(),
            AllWorkSteps = await _workStepRepository.GetActiveAsync(),
            SelectedWorkStepIds = await _repository.GetWorkStepIdsAsync(workplace.Id)
        };

        await SetFormViewBagAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, ProductionWorkplaceEditViewModel vm, int[]? workStepIds = null)
    {
        if (id != vm.Id)
            return NotFound();

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // Kuerzel wie beim Speichern: Sage-Werkbank behaelt den gespeicherten Wert (FA-a8).
        await ValidateTeilelisteAsync(vm, vm.BdeAktiv,
            existing.SageArbeitsplatznummer == null ? NormalizeCode(vm.ArbeitsschrittCode) : existing.ArbeitsschrittCode);
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            vm.AllWorkSteps = await _workStepRepository.GetActiveAsync();
            vm.SelectedWorkStepIds = workStepIds?.ToList() ?? new List<int>();
            await SetFormViewBagAsync();
            return View(vm);
        }

        existing.Name = vm.Name;
        existing.Hall = vm.Hall;
        existing.OverridePrePickingDays = vm.OverridePrePickingDays;
        existing.BdeAktiv = vm.BdeAktiv;
        existing.BdeDefaultArbeitsgang = string.IsNullOrWhiteSpace(vm.BdeDefaultArbeitsgang)
            ? null
            : vm.BdeDefaultArbeitsgang.Trim();
        existing.BdeUseCustomShiftPlan = vm.BdeUseCustomShiftPlan;
        existing.BdeNurAgFertigmeldung = vm.BdeNurAgFertigmeldung;
        existing.BdeTeileliste = vm.BdeTeileliste;
        // FA-a8: Kuerzel nur an Werkbaenken OHNE Sage-Nummer — entschieden am gespeicherten Wert, nie am Formular
        // (die Sage-Nummer kommt als readonly-Input zurueck und ist manipulierbar). Repository schuetzt zusaetzlich.
        if (existing.SageArbeitsplatznummer == null)
            existing.ArbeitsschrittCode = NormalizeCode(vm.ArbeitsschrittCode);
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _repository.UpdateAsync(existing);
        await _repository.SetProductionWorkplaceUsersAsync(
            id,
            vm.SelectedUserIds ?? new List<int>(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());
        await _repository.SetWorkStepsAsync(
            id,
            workStepIds?.ToList() ?? new List<int>(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Werkbank '{existing.Name}' wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizeCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private async Task<bool> IsHierarchicalMasterAsync()
    {
        var v = await _serviceSettings.GetValueAsync(IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master);
        return v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>v1.99.32 (Spec 2026-11-13 BDE-Teileliste, AK 2): Option an nur bei Master an, BDE aktiv und gesetztem Kuerzel —
    /// sonst ModelState-Fehler, nichts gespeichert (auch ein manipulierter POST an AKE ohne Feld).</summary>
    private async Task ValidateTeilelisteAsync(ProductionWorkplaceEditViewModel vm, bool bdeAktiv, string? code)
    {
        if (!vm.BdeTeileliste) return;
        if (!await IsHierarchicalMasterAsync())
            ModelState.AddModelError(string.Empty, "Die BDE-Teileliste gibt es nur bei hierarchischen Produktionsaufträgen (IDEAL).");
        else if (!bdeAktiv)
            ModelState.AddModelError(nameof(vm.BdeTeileliste), "Die BDE-Teileliste braucht „BDE aktiv“.");
        else if (code == null)
            ModelState.AddModelError(nameof(vm.BdeTeileliste), "Die BDE-Teileliste braucht ein Arbeitsschritt-Kürzel an der Werkbank.");
    }

    private async Task SetFormViewBagAsync()
    {
        ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync(AppSettingKeys.BdeDefaultArbeitsgang) ?? "(nicht gesetzt)";
        // FA-c8: Hinweis, dass die Werkbank-Option bei globalem NurFA ohne Wirkung ist
        ViewBag.NurFaGlobal = (await _appSettings.GetValueAsync(AppSettingKeys.BdeNurFaMeldung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        // v1.99.32: Option BDE-Teileliste nur bei Master an sichtbar (IDEAL)
        ViewBag.Hierarchisch = await IsHierarchicalMasterAsync();
    }
}
