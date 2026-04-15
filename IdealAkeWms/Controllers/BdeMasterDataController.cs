using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireBdeShiftleadAccess]
public class BdeMasterDataController : Controller
{
    private readonly IBdeOperatorRepository _operatorRepository;
    private readonly IBdeActivityRepository _activityRepository;
    private readonly IBdeTerminalRepository _terminalRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public BdeMasterDataController(
        IBdeOperatorRepository operatorRepository,
        IBdeActivityRepository activityRepository,
        IBdeTerminalRepository terminalRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _operatorRepository = operatorRepository;
        _activityRepository = activityRepository;
        _terminalRepository = terminalRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(string tab = "operators")
    {
        var operators = await _operatorRepository.GetAllAsync();
        var activities = await _activityRepository.GetAllAsync();
        var terminals = await _terminalRepository.GetAllAsync();

        ViewBag.Tab = tab;
        ViewBag.Operators = operators
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.FirstName)
            .ToList();
        ViewBag.Activities = activities;
        ViewBag.Terminals = terminals;

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
                CreatedAt = DateTime.UtcNow,
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
            existing.ModifiedAt = DateTime.UtcNow;
            existing.ModifiedBy = _currentUserService.GetDisplayName();
            existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

            await _operatorRepository.UpdateAsync(existing);
            TempData["SuccessMessage"] = $"Operator '{existing.DisplayName}' wurde erfolgreich gespeichert.";
        }

        return RedirectToAction(nameof(Index), new { tab = "operators" });
    }

    private async Task PopulateUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        ViewBag.Users = users.OrderBy(u => u.Name).ToList();
    }
}
