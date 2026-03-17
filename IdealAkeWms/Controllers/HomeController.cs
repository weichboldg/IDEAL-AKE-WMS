using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Controllers;

public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppSettingRepository _appSettings;

    public HomeController(ICurrentUserService currentUserService, IAppSettingRepository appSettings)
    {
        _currentUserService = currentUserService;
        _appSettings = appSettings;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.DisplayName = _currentUserService.GetDisplayName();
        ViewBag.CanPick = await _currentUserService.CanPickAsync();
        ViewBag.CanViewTracking = await _currentUserService.CanViewTrackingAsync();
        ViewBag.HasMasterDataAccess = await _currentUserService.HasMasterDataAccessAsync();
        var teileverfolgungAktiv = (await _appSettings.GetValueAsync("TeileverfolgungAktiv"))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        ViewBag.TeileverfolgungAktiv = teileverfolgungAktiv;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
