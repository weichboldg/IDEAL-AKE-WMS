using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/picking-status")]
[ApiController]
[RequirePickingAccess]
public class PickingStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone", "IsDonePicking"
    ];

    private readonly IProductionOrderPickingStatusRepository _pickingStatus;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public PickingStatusApiController(
        IProductionOrderPickingStatusRepository pickingStatus,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _pickingStatus = pickingStatus;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] PickingStatusToggleRequest req)
    {
        if (!AllowedFields.Contains(req.Field))
            return BadRequest($"Ungültiges Feld: {req.Field}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _pickingStatus.GetByProductionOrderIdAsync(req.ProductionOrderId);
        if (row == null) return NotFound("PickingStatus-Zeile fehlt (sollte durch AgentJob eager-created sein).");

        await _pickingStatus.SetFieldAsync(
            req.ProductionOrderId, req.Field, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class PickingStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
