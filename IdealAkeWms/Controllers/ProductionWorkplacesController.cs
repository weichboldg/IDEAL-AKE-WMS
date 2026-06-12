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
    private readonly ApplicationDbContext _ctx;

    public ProductionWorkplacesController(
        IProductionWorkplaceRepository repository,
        IUserRepository userRepository,
        IWorkStepRepository workStepRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository appSettings,
        ApplicationDbContext ctx)
    {
        _repository = repository;
        _userRepository = userRepository;
        _workStepRepository = workStepRepository;
        _currentUserService = currentUserService;
        _appSettings = appSettings;
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
        ["hall"] = wp => wp.Hall,
        ["bde"] = wp => wp.BdeAktiv ? "Aktiv" : "Inaktiv",
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
        var list = ColumnFilterHelper.Apply(workplaces, columnFilters, ColumnMap).ToList();
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
        ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync(AppSettingKeys.BdeDefaultArbeitsgang) ?? "(nicht gesetzt)";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(ProductionWorkplaceEditViewModel vm, int[]? workStepIds = null)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            vm.AllWorkSteps = await _workStepRepository.GetActiveAsync();
            vm.SelectedWorkStepIds = workStepIds?.ToList() ?? new List<int>();
            ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync(AppSettingKeys.BdeDefaultArbeitsgang) ?? "(nicht gesetzt)";
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
            CustomShifts = await _ctx.BdeShifts
                .Where(s => s.ProductionWorkplaceId == workplace.Id)
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .ToListAsync(),
            SelectedUserIds = workplace.ProductionWorkplaceUsers.Select(wu => wu.UserId).ToList(),
            AvailableUsers = await _userRepository.GetActiveUsersAsync(),
            AllWorkSteps = await _workStepRepository.GetActiveAsync(),
            SelectedWorkStepIds = await _repository.GetWorkStepIdsAsync(workplace.Id)
        };

        ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync(AppSettingKeys.BdeDefaultArbeitsgang) ?? "(nicht gesetzt)";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, ProductionWorkplaceEditViewModel vm, int[]? workStepIds = null)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            vm.AllWorkSteps = await _workStepRepository.GetActiveAsync();
            vm.SelectedWorkStepIds = workStepIds?.ToList() ?? new List<int>();
            ViewBag.GlobalDefaultArbeitsgang = await _appSettings.GetValueAsync(AppSettingKeys.BdeDefaultArbeitsgang) ?? "(nicht gesetzt)";
            return View(vm);
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = vm.Name;
        existing.Hall = vm.Hall;
        existing.OverridePrePickingDays = vm.OverridePrePickingDays;
        existing.BdeAktiv = vm.BdeAktiv;
        existing.BdeDefaultArbeitsgang = string.IsNullOrWhiteSpace(vm.BdeDefaultArbeitsgang)
            ? null
            : vm.BdeDefaultArbeitsgang.Trim();
        existing.BdeUseCustomShiftPlan = vm.BdeUseCustomShiftPlan;
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
}
