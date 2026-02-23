using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUserService;

    public HomeController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public IActionResult Index()
    {
        ViewBag.DisplayName = _currentUserService.GetDisplayName();
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
