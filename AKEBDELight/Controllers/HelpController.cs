using Microsoft.AspNetCore.Mvc;

namespace AKEBDELight.Controllers;

public class HelpController : Controller
{
    public IActionResult Index() => View();
}
