using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;

namespace IdealAkeWms.Controllers;

[Route("api/stock")]
[ApiController]
[RequireStockOrPickingOrTrackingAccess]
public class StockApiController : ControllerBase
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public StockApiController(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    [HttpGet("by-order/{orderNumber}")]
    public async Task<IActionResult> GetStockByOrder(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return BadRequest("orderNumber is required.");

        var items = await _stockMovementRepository.GetStockByProductionOrderAsync(orderNumber);

        return Ok(items.Select(i => new
        {
            articleNumber = i.ArticleNumber,
            description = i.ArticleDescription ?? "",
            storageLocation = i.StorageLocationCode,
            quantity = i.CurrentQuantity,
            unit = i.Unit ?? ""
        }));
    }
}
