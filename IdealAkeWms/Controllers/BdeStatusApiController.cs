using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[Route("api/bde-status")]
[ApiController]
[RequirePickingAccess]
public class BdeStatusApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedFields = ["IsDoneBde"];

    private readonly IProductionOrderBdeStatusRepository _bdeStatus;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;

    public BdeStatusApiController(
        IProductionOrderBdeStatusRepository bdeStatus,
        IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser)
    {
        _bdeStatus = bdeStatus;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] BdeStatusToggleRequest req)
    {
        if (!AllowedFields.Contains(req.Field))
            return BadRequest($"Ungültiges Feld: {req.Field}");

        var order = await _productionOrders.GetByIdAsync(req.ProductionOrderId);
        if (order == null) return NotFound();

        var row = await _bdeStatus.GetByProductionOrderIdAsync(req.ProductionOrderId);
        if (row == null) return NotFound("BdeStatus-Zeile fehlt.");

        await _bdeStatus.SetIsDoneBdeAsync(
            req.ProductionOrderId, req.Value,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        return Ok();
    }
}

public class BdeStatusToggleRequest
{
    public int ProductionOrderId { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
