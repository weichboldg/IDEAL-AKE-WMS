using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;

namespace AKEBDELight.Controllers;

[Route("api/productionorders")]
[ApiController]
public class ProductionOrdersApiController : ControllerBase
{
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
}
