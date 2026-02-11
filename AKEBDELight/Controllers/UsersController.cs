using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserRepository userRepository, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userRepository.GetAllAsync();
        return View(users.OrderBy(u => u.Name).ToList());
    }

    public IActionResult Create()
    {
        return View(new User { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (!ModelState.IsValid)
            return View(user);

        user.CreatedAt = DateTime.Now;
        user.CreatedBy = _currentUserService.GetDisplayName();
        user.CreatedByWindows = _currentUserService.GetWindowsUserName();

        await _userRepository.AddAsync(user);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User user)
    {
        if (id != user.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(user);

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = user.Name;
        existing.PersonalNumber = user.PersonalNumber;
        existing.IsActive = user.IsActive;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUserService.GetDisplayName();
        existing.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _userRepository.UpdateAsync(existing);
        return RedirectToAction(nameof(Index));
    }
}
