using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;

    public AccountController(IUserRepository userRepository, IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Wenn bereits eingeloggt, zum Dashboard
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Index", "Home");

        var vm = new LoginViewModel
        {
            AvailableUsers = await _userRepository.GetActiveUsersAsync()
        };

        ViewBag.ReturnUrl = returnUrl;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        vm.AvailableUsers = await _userRepository.GetActiveUsersAsync();

        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userRepository.GetByIdAsync(vm.UserId);
        if (user == null || !user.IsActive)
        {
            vm.ErrorMessage = "Benutzer nicht gefunden oder inaktiv.";
            return View(vm);
        }

        // Passwort prüfen (wenn gesetzt)
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!_passwordService.VerifyPassword(user.PasswordHash, vm.Password ?? string.Empty))
            {
                vm.ErrorMessage = "Falsches Passwort.";
                return View(vm);
            }
        }
        else
        {
            // Kein Passwort gesetzt - nur prüfen ob Passwort leer ist
            if (!string.IsNullOrEmpty(vm.Password))
            {
                vm.ErrorMessage = "Für diesen Benutzer ist kein Passwort hinterlegt.";
                return View(vm);
            }
        }

        // Session setzen
        HttpContext.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
        HttpContext.Session.SetString(CurrentUserService.SessionKeyUserName, user.Name);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
