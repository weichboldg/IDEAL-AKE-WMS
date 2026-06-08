using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordService _passwordService;
    private readonly IUserViewPreferenceRepository _viewPreferenceRepository;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService,
        IPasswordService passwordService,
        IUserViewPreferenceRepository viewPreferenceRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
        _viewPreferenceRepository = viewPreferenceRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var users = await _userRepository.GetAllWithRolesAsync();
        var ordered = users.OrderBy(u => u.Name).ToList();

        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = ordered.Count
        };
        return View(ordered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Create()
    {
        var vm = new UserEditViewModel { IsActive = true };
        await PopulateRolesAsync(vm, new List<int>());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(UserEditViewModel vm, string? newPassword)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(vm, vm.SelectedRoleIds);
            return View(vm);
        }

        var user = new User
        {
            Name = vm.Name,
            PersonalNumber = vm.PersonalNumber,
            IsActive = vm.IsActive,
            Email = vm.Email,
            NotifyOnReorderLevel = vm.NotifyOnReorderLevel,
            DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe,
            RecursiveFilterSearch = vm.RecursiveFilterSearch,
            IsPicker = vm.IsPicker,
            DefaultPageSize = ValidatedPageSize(vm.DefaultPageSize),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        if (!string.IsNullOrEmpty(newPassword))
            user.PasswordHash = _passwordService.HashPassword(newPassword);

        await _userRepository.AddAsync(user);

        await _roleRepository.SetUserRolesAsync(
            user.Id,
            vm.SelectedRoleIds,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Benutzer '{user.Name}' wurde erfolgreich angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoleKeys = await _roleRepository.GetRoleKeysByUserIdAsync(id);
        var allRoles = await _roleRepository.GetAllOrderedAsync();
        var selectedRoleIds = allRoles
            .Where(r => userRoleKeys.Contains(r.Key))
            .Select(r => r.Id)
            .ToList();

        var vm = new UserEditViewModel
        {
            Id = user.Id,
            Name = user.Name,
            PersonalNumber = user.PersonalNumber,
            IsActive = user.IsActive,
            Email = user.Email,
            NotifyOnReorderLevel = user.NotifyOnReorderLevel,
            DefaultFilterBeschaffung = user.DefaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = user.DefaultFilterArtikelgruppe,
            RecursiveFilterSearch = user.RecursiveFilterSearch,
            IsPicker = user.IsPicker,
            DefaultPageSize = user.DefaultPageSize,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            CreatedByWindows = user.CreatedByWindows,
            SelectedRoleIds = selectedRoleIds
        };

        await PopulateRolesAsync(vm, selectedRoleIds);
        ViewBag.SavedViewPreferences = await _viewPreferenceRepository.GetAllByUserAsync(id);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, UserEditViewModel vm, string? newPassword)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(vm, vm.SelectedRoleIds);
            return View(vm);
        }

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = vm.Name;
        existing.PersonalNumber = vm.PersonalNumber;
        existing.IsActive = vm.IsActive;
        existing.Email = vm.Email;
        existing.NotifyOnReorderLevel = vm.NotifyOnReorderLevel;
        existing.DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung;
        existing.DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe;
        existing.RecursiveFilterSearch = vm.RecursiveFilterSearch;
        existing.IsPicker = vm.IsPicker;
        existing.DefaultPageSize = ValidatedPageSize(vm.DefaultPageSize);

        if (!string.IsNullOrEmpty(newPassword))
            existing.PasswordHash = _passwordService.HashPassword(newPassword);

        existing.ModifiedAt = DateTime.UtcNow;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _userRepository.UpdateAsync(existing);

        await _roleRepository.SetUserRolesAsync(
            id,
            vm.SelectedRoleIds,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Benutzer '{existing.Name}' wurde erfolgreich gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> ResetViewPreferences(int id, string? viewKey)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(viewKey))
        {
            await _viewPreferenceRepository.DeleteByUserAndViewAsync(id, viewKey);
            var displayName = ColumnDefinitions.GetByViewKey(viewKey)?.DisplayName ?? viewKey;
            TempData["SuccessMessage"] = $"Ansichts-Einstellungen '{displayName}' fuer '{user.Name}' wurden zurueckgesetzt.";
        }
        else
        {
            await _viewPreferenceRepository.DeleteAllByUserAsync(id);
            TempData["SuccessMessage"] = $"Alle Ansichts-Einstellungen fuer '{user.Name}' wurden zurueckgesetzt.";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>
    /// Hand-gepflegte Uebersicht: welche Rolle hat Zugriff auf welche Seiten.
    /// Wird von Users/Index|Create|Edit verlinkt. View kommt in Task 9.
    /// </summary>
    [HttpGet]
    public IActionResult RoleOverview()
    {
        return View();
    }

    private async Task PopulateRolesAsync(UserEditViewModel vm, List<int> selectedIds)
    {
        var roles = await _roleRepository.GetAllOrderedAsync();
        vm.AvailableRoles = roles.Select(r => new RoleCheckboxItem
        {
            Id = r.Id,
            Name = r.Name,
            Key = r.Key,
            IsSelected = selectedIds.Contains(r.Id)
        }).ToList();
    }

    /// <summary>Akzeptiert nur erlaubte PageSize-Werte; sonst NULL (= System-Default).</summary>
    private static int? ValidatedPageSize(int? value)
        => value.HasValue && Services.PageSize.AllowedOptions.Contains(value.Value) ? value : null;
}
