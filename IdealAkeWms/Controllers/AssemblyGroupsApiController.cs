using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/assembly-groups")]
[ApiController]
[RequirePickingAccess]
public class AssemblyGroupsApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedGroupKeys = ["VK", "VL", "VE", "VT", "VA"];

    private readonly IProductionOrderAssemblyGroupRepository _groups;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public AssemblyGroupsApiController(
        IProductionOrderAssemblyGroupRepository groups,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _groups = groups;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle-applicable")]
    public async Task<IActionResult> ToggleApplicable([FromBody] AssemblyGroupToggleRequest req)
    {
        if (!AllowedGroupKeys.Contains(req.GroupKey))
            return BadRequest($"Ungültiger GroupKey: {req.GroupKey}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _groups.GetByPoAndKeyAsync(req.ProductionOrderId, req.GroupKey);
        if (row == null) return NotFound("AssemblyGroup-Zeile fehlt (sollte durch AgentJob eager-created sein).");

        await _groups.SetIsApplicableAsync(
            req.ProductionOrderId, req.GroupKey, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class AssemblyGroupToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public bool Value { get; set; }
}
