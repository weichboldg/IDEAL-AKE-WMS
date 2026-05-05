using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUserService _currentUserService;

    public AccountController(IUserRepository userRepository, IPasswordService passwordService, ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Wenn bereits eingeloggt, zum Dashboard
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userRepository.GetByNameAsync(vm.UserName);
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

    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!userId.HasValue)
            return RedirectToAction(nameof(Login));

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return RedirectToAction(nameof(Login));

        var vm = new ProfileViewModel
        {
            Name = user.Name,
            PersonalNumber = user.PersonalNumber,
            DefaultFilterBeschaffung = user.DefaultFilterBeschaffung,
            DefaultFilterArtikelgruppe = user.DefaultFilterArtikelgruppe,
            RecursiveFilterSearch = user.RecursiveFilterSearch,
            Email = user.Email,
            NotifyOnReorderLevel = user.NotifyOnReorderLevel
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm, string? newPassword)
    {
        var userId = _currentUserService.GetCurrentAppUserId();
        if (!userId.HasValue)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return RedirectToAction(nameof(Login));

        user.DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung;
        user.DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe;
        user.RecursiveFilterSearch = vm.RecursiveFilterSearch;
        user.Email = vm.Email;
        user.NotifyOnReorderLevel = vm.NotifyOnReorderLevel;

        if (!string.IsNullOrEmpty(newPassword))
            user.PasswordHash = _passwordService.HashPassword(newPassword);

        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = _currentUserService.GetCurrentAppUserName() ?? string.Empty;
        user.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _userRepository.UpdateAsync(user);
        TempData["SuccessMessage"] = "Profil gespeichert.";
        return RedirectToAction(nameof(Profile));
    }
}
