using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class RolesController : Controller
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;

    public RolesController(
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService)
    {
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Name inkl. "System"-Badge-Text bei Systemrollen, Benutzer-Anzahl als Zahl).
    /// </summary>
    private static readonly Dictionary<string, Func<RoleEditViewModel, string?>> ColumnMap = new()
    {
        ["name"] = r => r.IsSystem ? $"{r.Name} System" : r.Name,
        ["key"] = r => r.Key,
        ["ad-group"] = r => r.AdGroup,
        ["user-count"] = r => r.UserCount.ToString(),
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var roles = await _roleRepository.GetAllOrderedAsync();
        var allViewModels = roles.Select(r => new RoleEditViewModel
        {
            Id = r.Id,
            Key = r.Key,
            Name = r.Name,
            Description = r.Description,
            AdGroup = r.AdGroup,
            SortOrder = r.SortOrder,
            IsSystem = r.IsSystem,
            UserCount = r.UserRoles.Count
        });

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var viewModels = ColumnFilterHelper.Apply(allViewModels, columnFilters, ColumnMap).ToList();

        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = viewModels.Count
        };
        return View(viewModels.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    public IActionResult Create()
    {
        var vm = new RoleEditViewModel { SortOrder = 100 };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var existingByKey = await _roleRepository.GetByKeyAsync(vm.Key);
        if (existingByKey != null)
        {
            ModelState.AddModelError(nameof(vm.Key), "Dieser Schlüssel wird bereits verwendet.");
            return View(vm);
        }

        var role = new Role
        {
            Key = vm.Key,
            Name = vm.Name,
            Description = vm.Description,
            AdGroup = vm.AdGroup,
            SortOrder = vm.SortOrder,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _roleRepository.AddAsync(role);

        TempData["SuccessMessage"] = $"Rolle \"{role.Name}\" wurde erfolgreich angelegt.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        var vm = new RoleEditViewModel
        {
            Id = role.Id,
            Key = role.Key,
            Name = role.Name,
            Description = role.Description,
            AdGroup = role.AdGroup,
            SortOrder = role.SortOrder,
            IsSystem = role.IsSystem,
            UserCount = role.UserRoles.Count
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleEditViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(vm);

        var existing = await _roleRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        // System roles: only Name, Description, and AdGroup can be changed
        if (existing.IsSystem)
        {
            existing.Name = vm.Name;
            existing.Description = vm.Description;
            existing.AdGroup = vm.AdGroup;
        }
        else
        {
            // Check unique key only if changed
            if (existing.Key != vm.Key)
            {
                var existingByKey = await _roleRepository.GetByKeyAsync(vm.Key);
                if (existingByKey != null)
                {
                    vm.IsSystem = existing.IsSystem;
                    vm.UserCount = existing.UserRoles.Count;
                    ModelState.AddModelError(nameof(vm.Key), "Dieser Schlüssel wird bereits verwendet.");
                    return View(vm);
                }
            }

            existing.Key = vm.Key;
            existing.Name = vm.Name;
            existing.Description = vm.Description;
            existing.AdGroup = vm.AdGroup;
            existing.SortOrder = vm.SortOrder;
        }

        existing.ModifiedAt = DateTime.UtcNow;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _roleRepository.UpdateAsync(existing);

        TempData["SuccessMessage"] = $"Rolle \"{existing.Name}\" wurde erfolgreich gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return NotFound();

        if (role.IsSystem)
        {
            TempData["WarningMessage"] = "Systemrollen können nicht gelöscht werden.";
            return RedirectToAction(nameof(Index));
        }

        if (role.UserRoles.Count > 0)
        {
            TempData["WarningMessage"] = $"Die Rolle \"{role.Name}\" ist noch {role.UserRoles.Count} Benutzer(n) zugewiesen und kann nicht gelöscht werden.";
            return RedirectToAction(nameof(Index));
        }

        var roleName = role.Name;
        await _roleRepository.DeleteAsync(role);

        TempData["SuccessMessage"] = $"Rolle \"{roleName}\" wurde erfolgreich gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
