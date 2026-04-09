using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataAccess]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordService _passwordService;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService,
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userRepository.GetAllWithRolesAsync();
        return View(users.OrderBy(u => u.Name).ToList());
    }

    public async Task<IActionResult> Create()
    {
        var vm = new UserEditViewModel { IsActive = true };
        await PopulateRolesAsync(vm, new List<int>());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            CreatedByWindows = user.CreatedByWindows,
            SelectedRoleIds = selectedRoleIds
        };

        await PopulateRolesAsync(vm, selectedRoleIds);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
}
