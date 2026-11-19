using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Models;

namespace IdealAkeWms.Controllers;

public class HomeController : Controller
{
    // v1.99.45 (ADR 0034): Kacheln rendert die View aus NavigationSicht, keine Rechte mehr im Controller.
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
