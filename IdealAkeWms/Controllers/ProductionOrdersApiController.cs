using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[Route("api/productionorders")]
[ApiController]
[RequirePickingAccess]
public class ProductionOrdersApiController : ControllerBase
{
    private static readonly HashSet<string> AllowedToggleFields = [
        "HasGlass", "HasExternalPurchase", "IsCoatingDone",
        "HasCooling", "HasFan", "HasElectric", "HasDoors", "HasSuperstructure"
    ];

    private readonly IProductionOrderRepository _repository;

    public ProductionOrdersApiController(IProductionOrderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 20)
    {
        var orders = await _repository.SearchAsync(q, limit);

        var result = orders.Select(o => new
        {
            id = o.Id,
            text = string.IsNullOrEmpty(o.ArticleNumber)
                ? $"{o.OrderNumber} - {o.Customer}"
                : $"{o.OrderNumber} - {o.ArticleNumber} - {o.Customer}"
        });

        return Ok(result);
    }

    [HttpPost("toggle-field")]
    public async Task<IActionResult> ToggleField([FromBody] ToggleFieldRequest request)
    {
        if (!AllowedToggleFields.Contains(request.Field))
            return BadRequest("Ungültiges Feld.");

        var order = await _repository.GetByIdAsync(request.Id);
        if (order == null)
            return NotFound();

        if (request.Field == "HasGlass")
            order.HasGlass = request.Value;
        else if (request.Field == "HasExternalPurchase")
            order.HasExternalPurchase = request.Value;
        else if (request.Field == "IsCoatingDone")
            order.IsCoatingDone = request.Value;
        else if (request.Field == "HasCooling")
            order.HasCooling = request.Value;
        else if (request.Field == "HasFan")
            order.HasFan = request.Value;
        else if (request.Field == "HasElectric")
            order.HasElectric = request.Value;
        else if (request.Field == "HasDoors")
            order.HasDoors = request.Value;
        else if (request.Field == "HasSuperstructure")
            order.HasSuperstructure = request.Value;

        await _repository.UpdateAsync(order);
        return Ok();
    }
}

public class ToggleFieldRequest
{
    public int Id { get; set; }
    public string Field { get; set; } = string.Empty;
    public bool Value { get; set; }
}
