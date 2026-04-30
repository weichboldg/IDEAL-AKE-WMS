using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/warehouserequisitions")]
[RequirePickingOrStockAccess]
public class WarehouseRequisitionsApiController : ControllerBase
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IArticleRepository _articles;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;

    public WarehouseRequisitionsApiController(
        IWarehouseRequisitionRepository repo, IArticleRepository articles,
        IStockMovementRepository stock, ICurrentUserService user)
    {
        _repo = repo;
        _articles = articles;
        _stock = stock;
        _user = user;
    }

    public record AddItemRequest(string ArticleNumber, decimal Quantity);
    public record UpdateItemRequest(decimal Quantity);

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddItemRequest body)
    {
        var article = await _articles.GetByArticleNumberAsync(body.ArticleNumber);
        if (article == null)
            return BadRequest(new { error = "Artikel nicht gefunden." });

        try
        {
            await _repo.AddItemAsync(id, body.ArticleNumber, article.Description ?? "", article.Unit,
                body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok();
    }

    [HttpPut("items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateItemRequest body)
    {
        await _repo.UpdateItemQuantityAsync(itemId, body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        await _repo.RemoveItemAsync(itemId);
        return Ok();
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] string articleNumber)
    {
        var stock = await _stock.GetCurrentStockAsync(filterArticle: articleNumber);
        var locationStr = string.Join(", ", stock.Where(s => s.CurrentQuantity > 0).Select(s => $"{s.StorageLocationCode} ({s.CurrentQuantity:N3})"));
        return Ok(new { locations = locationStr });
    }
}
