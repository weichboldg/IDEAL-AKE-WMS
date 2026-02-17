using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class WorkstationsController : Controller
{
    private readonly IWorkstationRepository _workstationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public WorkstationsController(
        IWorkstationRepository workstationRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _workstationRepository = workstationRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var workstations = await _workstationRepository.GetAllWithUsersAsync();
        return View(workstations);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new WorkstationEditViewModel
        {
            AvailableUsers = await _userRepository.GetActiveUsersAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkstationEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            return View(vm);
        }

        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        var workstation = new Workstation
        {
            Name = vm.Name,
            Location = vm.Location,
            DefaultPrinter = vm.DefaultPrinter,
            DefaultUserId = vm.DefaultUserId,
            CreatedAt = DateTime.Now,
            CreatedBy = displayName,
            CreatedByWindows = windowsUser
        };

        await _workstationRepository.AddAsync(workstation);

        if (vm.SelectedUserIds.Count > 0)
        {
            await _workstationRepository.SetWorkstationUsersAsync(
                workstation.Id, vm.SelectedUserIds, displayName, windowsUser);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var workstation = await _workstationRepository.GetByIdWithUsersAsync(id);
        if (workstation == null)
            return NotFound();

        var vm = new WorkstationEditViewModel
        {
            Id = workstation.Id,
            Name = workstation.Name,
            Location = workstation.Location,
            DefaultPrinter = workstation.DefaultPrinter,
            DefaultUserId = workstation.DefaultUserId,
            SelectedUserIds = workstation.WorkstationUsers.Select(wu => wu.UserId).ToList(),
            AvailableUsers = await _userRepository.GetActiveUsersAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkstationEditViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();
            return View(vm);
        }

        var existing = await _workstationRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        existing.Name = vm.Name;
        existing.Location = vm.Location;
        existing.DefaultPrinter = vm.DefaultPrinter;
        existing.DefaultUserId = vm.DefaultUserId;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = displayName;
        existing.ModifiedByWindows = windowsUser;

        await _workstationRepository.UpdateAsync(existing);
        await _workstationRepository.SetWorkstationUsersAsync(
            id, vm.SelectedUserIds, displayName, windowsUser);

        return RedirectToAction(nameof(Index));
    }
}
