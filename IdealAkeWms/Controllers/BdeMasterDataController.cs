using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireBdeActive]
[RequireBdeShiftleadAccess]
public class BdeMasterDataController : Controller
{
    private readonly IBdeOperatorRepository _operatorRepository;
    private readonly IBdeActivityRepository _activityRepository;
    private readonly IBdeTerminalRepository _terminalRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductionWorkplaceRepository _workplaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public BdeMasterDataController(
        IBdeOperatorRepository operatorRepository,
        IBdeActivityRepository activityRepository,
        IBdeTerminalRepository terminalRepository,
        IUserRepository userRepository,
        IProductionWorkplaceRepository workplaceRepository,
        ICurrentUserService currentUserService)
    {
        _operatorRepository = operatorRepository;
        _activityRepository = activityRepository;
        _terminalRepository = terminalRepository;
        _userRepository = userRepository;
        _workplaceRepository = workplaceRepository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter (Tab "Operatoren"): Col-Key -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View rendert
    /// (Name als "Nachname Vorname", Aktiv-Badge "Aktiv"/"Inaktiv", WMS-Benutzer bzw. "-").
    /// </summary>
    private static readonly Dictionary<string, Func<BdeOperator, string?>> OperatorColumnMap = new()
    {
        ["personnel-number"] = o => o.PersonnelNumber,
        ["name"] = o => $"{o.LastName} {o.FirstName}",
        ["active"] = o => o.IsActive ? "Aktiv" : "Inaktiv",
        ["user"] = o => o.User?.Name ?? "-",
    };

    /// <summary>Server-Side-Spaltenfilter (Tab "Aktivitäten"): Col-Key -> gerenderter Zell-Text.</summary>
    private static readonly Dictionary<string, Func<BdeActivity, string?>> ActivityColumnMap = new()
    {
        ["code"] = a => a.Code,
        ["name"] = a => a.Name,
        ["active"] = a => a.IsActive ? "Aktiv" : "Inaktiv",
    };

    /// <summary>Server-Side-Spaltenfilter (Tab "Terminals"): Col-Key -> gerenderter Zell-Text.</summary>
    private static readonly Dictionary<string, Func<BdeTerminal, string?>> TerminalColumnMap = new()
    {
        ["user"] = t => t.User?.Name ?? "-",
        ["workplace"] = t => t.DefaultProductionWorkplace?.Name ?? "-",
        ["description"] = t => t.Description,
    };

    public async Task<IActionResult> Index(string tab = "operators", int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var operators = (await _operatorRepository.GetAllAsync())
            .OrderBy(o => o.LastName).ThenBy(o => o.FirstName).ToList();
        var activities = (await _activityRepository.GetAllAsync()).ToList();
        var terminals = (await _terminalRepository.GetAllAsync()).ToList();

        // Server-Side-Spaltenfilter: greift nur auf die Tabelle des aktiven Tabs
        // (die View rendert pro Request nur EINE Tabelle), vor der Pagination.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        switch (tab)
        {
            case "activities":
                activities = ColumnFilterHelper.Apply(activities, columnFilters, ActivityColumnMap).ToList();
                break;
            case "terminals":
                terminals = ColumnFilterHelper.Apply(terminals, columnFilters, TerminalColumnMap).ToList();
                break;
            default:
                operators = ColumnFilterHelper.Apply(operators, columnFilters, OperatorColumnMap).ToList();
                break;
        }

        // Pagination greift nur auf den aktiven Tab.
        int totalForTab = tab switch
        {
            "activities" => activities.Count,
            "terminals" => terminals.Count,
            _ => operators.Count
        };

        ViewBag.Tab = tab;
        ViewBag.Operators = tab == "operators"
            ? operators.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList()
            : operators;
        ViewBag.Activities = tab == "activities"
            ? activities.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList()
            : activities;
        ViewBag.Terminals = tab == "terminals"
            ? terminals.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList()
            : terminals;

        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = totalForTab
        };

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EditOperator(int? id)
    {
        BdeOperatorEditViewModel vm;
        if (id.HasValue)
        {
            var op = await _operatorRepository.GetByIdAsync(id.Value);
            if (op == null)
                return NotFound();

            vm = new BdeOperatorEditViewModel
            {
                Id = op.Id,
                PersonnelNumber = op.PersonnelNumber,
                FirstName = op.FirstName,
                LastName = op.LastName,
                IsActive = op.IsActive,
                UserId = op.UserId
            };
        }
        else
        {
            vm = new BdeOperatorEditViewModel { IsActive = true };
        }

        await PopulateUsersAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOperator(BdeOperatorEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersAsync();
            return View(vm);
        }

        if (vm.Id == 0)
        {
            var op = new BdeOperator
            {
                PersonnelNumber = vm.PersonnelNumber,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                IsActive = vm.IsActive,
                UserId = vm.UserId,
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };
            await _operatorRepository.AddAsync(op);
            TempData["SuccessMessage"] = $"Operator '{op.DisplayName}' wurde erfolgreich angelegt.";
        }
        else
        {
            var existing = await _operatorRepository.GetByIdAsync(vm.Id);
            if (existing == null)
                return NotFound();

            existing.PersonnelNumber = vm.PersonnelNumber;
            existing.FirstName = vm.FirstName;
            existing.LastName = vm.LastName;
            existing.IsActive = vm.IsActive;
            existing.UserId = vm.UserId;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _currentUserService.GetDisplayName();
            existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

            await _operatorRepository.UpdateAsync(existing);
            TempData["SuccessMessage"] = $"Operator '{existing.DisplayName}' wurde erfolgreich gespeichert.";
        }

        return RedirectToAction(nameof(Index), new { tab = "operators" });
    }

    [HttpGet]
    public async Task<IActionResult> EditActivity(int? id)
    {
        if (id == null)
            return View(new BdeActivityEditViewModel());

        var a = await _activityRepository.GetByIdAsync(id.Value);
        if (a == null)
            return NotFound();

        return View(new BdeActivityEditViewModel
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            IsActive = a.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditActivity(BdeActivityEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        if (vm.Id == 0)
        {
            var a = new BdeActivity
            {
                Code = vm.Code,
                Name = vm.Name,
                IsActive = vm.IsActive,
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };
            await _activityRepository.AddAsync(a);
            TempData["SuccessMessage"] = $"Aktivität '{a.Name}' wurde erfolgreich angelegt.";
        }
        else
        {
            var existing = await _activityRepository.GetByIdAsync(vm.Id);
            if (existing == null)
                return NotFound();

            existing.Code = vm.Code;
            existing.Name = vm.Name;
            existing.IsActive = vm.IsActive;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _currentUserService.GetDisplayName();
            existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

            await _activityRepository.UpdateAsync(existing);
            TempData["SuccessMessage"] = $"Aktivität '{existing.Name}' wurde erfolgreich gespeichert.";
        }

        return RedirectToAction(nameof(Index), new { tab = "activities" });
    }

    [HttpGet]
    [RequireBdeAdminAccess]
    public async Task<IActionResult> EditTerminal(int? id)
    {
        await LoadTerminalSelectListsAsync();

        if (id == null)
            return View(new BdeTerminalEditViewModel());

        var t = await _terminalRepository.GetByIdAsync(id.Value);
        if (t == null)
            return NotFound();

        return View(new BdeTerminalEditViewModel
        {
            Id = t.Id,
            UserId = t.UserId,
            DefaultProductionWorkplaceId = t.DefaultProductionWorkplaceId,
            Description = t.Description
        });
    }

    [HttpPost]
    [RequireBdeAdminAccess]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTerminal(BdeTerminalEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await LoadTerminalSelectListsAsync();
            return View(vm);
        }

        if (vm.Id == 0)
        {
            var duplicate = await _terminalRepository.GetByUserIdAsync(vm.UserId);
            if (duplicate != null)
            {
                ModelState.AddModelError(nameof(vm.UserId), "Dieser Benutzer ist bereits einem Terminal zugeordnet.");
                await LoadTerminalSelectListsAsync();
                return View(vm);
            }

            var t = new BdeTerminal
            {
                UserId = vm.UserId,
                DefaultProductionWorkplaceId = vm.DefaultProductionWorkplaceId,
                Description = vm.Description,
                CreatedAt = DateTime.Now,
                CreatedBy = _currentUserService.GetDisplayName(),
                CreatedByWindows = _currentUserService.GetWindowsUserName()
            };
            await _terminalRepository.AddAsync(t);
            TempData["SuccessMessage"] = "Terminal wurde erfolgreich angelegt.";
        }
        else
        {
            var existing = await _terminalRepository.GetByIdAsync(vm.Id);
            if (existing == null)
                return NotFound();

            if (existing.UserId != vm.UserId)
            {
                var duplicate = await _terminalRepository.GetByUserIdAsync(vm.UserId);
                if (duplicate != null && duplicate.Id != vm.Id)
                {
                    ModelState.AddModelError(nameof(vm.UserId), "Dieser Benutzer ist bereits einem Terminal zugeordnet.");
                    await LoadTerminalSelectListsAsync();
                    return View(vm);
                }
            }

            existing.UserId = vm.UserId;
            existing.DefaultProductionWorkplaceId = vm.DefaultProductionWorkplaceId;
            existing.Description = vm.Description;
            existing.ModifiedAt = DateTime.Now;
            existing.ModifiedBy = _currentUserService.GetDisplayName();
            existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

            await _terminalRepository.UpdateAsync(existing);
            TempData["SuccessMessage"] = "Terminal wurde erfolgreich gespeichert.";
        }

        return RedirectToAction(nameof(Index), new { tab = "terminals" });
    }

    private async Task LoadTerminalSelectListsAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        ViewBag.AllUsers = users.OrderBy(u => u.Name).ToList();
        ViewBag.AllWorkplaces = await _workplaceRepository.GetBdeActiveAsync();
    }

    private async Task PopulateUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        ViewBag.Users = users.OrderBy(u => u.Name).ToList();
    }
}
