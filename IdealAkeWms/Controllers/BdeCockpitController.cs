using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireBdeShiftleadAccess]
public class BdeCockpitController : Controller
{
    public IActionResult Index() => View();
}
