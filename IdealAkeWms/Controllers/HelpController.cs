using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

public class HelpController : Controller
{
    public IActionResult Index() => View();
}
