using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly IProductionWorkplaceRepository _productionWorkplaceRepository;
    private readonly IAppSettingRepository _appSettingRepository;

    public AccountController(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ICurrentUserService currentUserService,
        IWorkStepRepository workStepRepository,
        IProductionWorkplaceRepository productionWorkplaceRepository,
        IAppSettingRepository appSettingRepository)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _currentUserService = currentUserService;
        _workStepRepository = workStepRepository;
        _productionWorkplaceRepository = productionWorkplaceRepository;
        _appSettingRepository = appSettingRepository;
    }

    private async Task SetWindowsAuthAktivAsync()
    {
        var flag = await _appSettingRepository.GetValueAsync(AppSettingKeys.WindowsAuthAktiv);
        ViewBag.WindowsAuthAktiv = string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Wenn bereits eingeloggt, zum Dashboard
        if (HttpContext.Session.GetInt32(CurrentUserService.SessionKeyUserId).HasValue)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        await SetWindowsAuthAktivAsync();
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            await SetWindowsAuthAktivAsync();
            return View(vm);
        }

        var user = await _userRepository.GetByNameAsync(vm.UserName);
        if (user == null || !user.IsActive)
        {
            vm.ErrorMessage = "Benutzer nicht gefunden oder inaktiv.";
            await SetWindowsAuthAktivAsync();
            return View(vm);
        }

        // Passwort prüfen (wenn gesetzt)
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!_passwordService.VerifyPassword(user.PasswordHash, vm.Password ?? string.Empty))
            {
                vm.ErrorMessage = "Falsches Passwort.";
                await SetWindowsAuthAktivAsync();
                return View(vm);
            }
        }
        else
        {
            // Kein Passwort gesetzt - nur prüfen ob Passwort leer ist
            if (!string.IsNullOrEmpty(vm.Password))
            {
                vm.ErrorMessage = "Für diesen Benutzer ist kein Passwort hinterlegt.";
                await SetWindowsAuthAktivAsync();
                return View(vm);
            }
        }

        // Session setzen
        HttpContext.Session.SetInt32(CurrentUserService.SessionKeyUserId, user.Id);
        HttpContext.Session.SetString(CurrentUserService.SessionKeyUserName, user.Name);
        // Windows-Name (falls die Anmeldung ueber ein Domaenen-Geraet lief) fuers Audit in die
        // Session — die Middleware normalisiert HttpContext.User bei bestehender Session auf anonym.
        HttpContext.Session.SetString(CurrentUserService.SessionKeyWindowsUserName, HttpContext.User?.Identity?.Name ?? "");
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie);
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.AutoLoginTriedCookie);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // KEIN [ValidateAntiForgeryToken]: Nach einem Windows-SSO-Login wird die Seite unter der
    // Windows-Identitaet gerendert -> der Antiforgery-Token ist an diese Identitaet gebunden.
    // Mobile-Browser (Android/iOS) senden bei nachfolgenden POSTs KEIN NTLM erneut -> der
    // Logout-POST kommt anonym an -> Token-Identitaet != Request-Identitaet -> 400. Der Token
    // laesst sich nicht fuer Desktop (NTLM-Resend) UND Mobile (anonym) gleichzeitig passend
    // binden. Logout ist Low-Risk fuer CSRF (schlimmstenfalls Abmeldung, kein Datenzugriff),
    // daher hier bewusst ohne Antiforgery-Pruefung.
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        Response.Cookies.Append(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie, "1",
            new CookieOptions { HttpOnly = true, IsEssential = true });
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult WindowsLogin()
    {
        // Force-SSO: einmalige Negotiate-Challenge auch fuer per UA nicht erkannte
        // Windows-Clients erzwingen. /Account/* ist von der Middleware ausgeschlossen,
        // deshalb auf / (Home) redirecten, wo die Middleware greift.
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.NoAutoLoginCookie);
        Response.Cookies.Delete(Middleware.WindowsAutoLoginMiddleware.AutoLoginTriedCookie);
        Response.Cookies.Append(Middleware.WindowsAutoLoginMiddleware.ForceSsoCookie, "1",
            new CookieOptions { HttpOnly = true, IsEssential = true });
        return RedirectToAction("Index", "Home");
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
            NotifyOnReorderLevel = user.NotifyOnReorderLevel,
            DefaultPageSize = user.DefaultPageSize,
            DefaultWorkStepId = user.DefaultWorkStepId,
            AvailableWorkSteps = await _workStepRepository.GetActiveAsync(),
            DefaultWorkbenches = user.DefaultWorkbenches,
            AvailableWorkplaces = await _productionWorkplaceRepository.GetAllOrderedAsync(),
            DefaultFilterFaWorklistDescription1 = user.DefaultFilterFaWorklistDescription1
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
        {
            vm.AvailableWorkSteps = await _workStepRepository.GetActiveAsync();
            vm.AvailableWorkplaces = await _productionWorkplaceRepository.GetAllOrderedAsync();
            return View(vm);
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return RedirectToAction(nameof(Login));

        user.DefaultFilterBeschaffung = vm.DefaultFilterBeschaffung;
        user.DefaultFilterArtikelgruppe = vm.DefaultFilterArtikelgruppe;
        user.RecursiveFilterSearch = vm.RecursiveFilterSearch;
        user.Email = vm.Email;
        user.NotifyOnReorderLevel = vm.NotifyOnReorderLevel;
        // PageSize-Validation: nur erlaubte Werte oder NULL durchlassen
        user.DefaultPageSize = (vm.DefaultPageSize.HasValue
            && IdealAkeWms.Services.PageSize.AllowedOptions.Contains(vm.DefaultPageSize.Value))
            ? vm.DefaultPageSize
            : null;
        user.DefaultWorkStepId = vm.DefaultWorkStepId;
        user.DefaultWorkbenches = string.IsNullOrWhiteSpace(vm.DefaultWorkbenches) ? null : vm.DefaultWorkbenches.Trim();
        user.DefaultFilterFaWorklistDescription1 = string.IsNullOrWhiteSpace(vm.DefaultFilterFaWorklistDescription1) ? null : vm.DefaultFilterFaWorklistDescription1.Trim();

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
