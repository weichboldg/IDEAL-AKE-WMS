using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/warehouserequisitions")]
[RequirePickingOrStockOrLagerbestellungAccess]
[RequireLagerbestellungAktiv]
public class WarehouseRequisitionsApiController : ControllerBase
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IArticleRepository _articles;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;
    private readonly IAppSettingRepository _settings;

    public WarehouseRequisitionsApiController(
        IWarehouseRequisitionRepository repo, IArticleRepository articles,
        IStockMovementRepository stock, ICurrentUserService user,
        IAppSettingRepository settings)
    {
        _repo = repo;
        _articles = articles;
        _stock = stock;
        _user = user;
        _settings = settings;
    }

    public record AddItemRequest(string ArticleNumber, decimal Quantity);
    public record UpdateItemRequest(decimal Quantity);

    /// <summary>
    /// Einheitlicher Guard fuer die Item-Endpoints: nur der Ersteller (Ownership,
    /// wie Submit/Cancel) darf Positionen einer Bestellung im Draft-Status aendern.
    /// Liefert bei Verletzung das kurzschliessende Result, sonst null.
    /// </summary>
    private IActionResult? CheckOwnershipAndDraft(WarehouseRequisition requisition)
    {
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        var owns = requisition.CreatedByUserId != null
            ? requisition.CreatedByUserId == userId
            : requisition.CreatedBy == displayName;
        if (!owns) return Forbid();
        if (requisition.Status != WarehouseRequisitionStatus.Draft)
            return BadRequest(new { error = "Bestellung ist nicht mehr im Entwurf." });
        return null;
    }

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddItemRequest body)
    {
        var article = await _articles.GetByArticleNumberAsync(body.ArticleNumber);
        if (article == null)
            return BadRequest(new { error = "Artikel nicht gefunden." });

        var requisition = await _repo.GetByIdAsync(id, includeItems: false);
        if (requisition == null)
            return NotFound();

        var guard = CheckOwnershipAndDraft(requisition);
        if (guard != null) return guard;

        var glasGroups = GlasArticleGroupFilter.ParseGroups(
            await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));
        var sharedGroups = GlasArticleGroupFilter.ParseGroups(
            await _settings.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen));
        if (!GlasArticleGroupFilter.IsAllowedForType(article.ArticleGroup, requisition.Type, glasGroups, sharedGroups))
        {
            var msg = requisition.Type == WarehouseRequisitionType.Glas
                ? $"Artikelgruppe '{article.ArticleGroup}' ist keine Glas-Artikelgruppe — Artikel gehoert in die Lager-Bestellung."
                : $"Artikelgruppe '{article.ArticleGroup}' gehoert zur Glas-Bestellung.";
            return BadRequest(new { error = msg });
        }

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
        var requisition = await _repo.GetByItemIdAsync(itemId);
        if (requisition == null) return NotFound();
        var guard = CheckOwnershipAndDraft(requisition);
        if (guard != null) return guard;

        await _repo.UpdateItemQuantityAsync(itemId, body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var requisition = await _repo.GetByItemIdAsync(itemId);
        if (requisition == null) return NotFound();
        var guard = CheckOwnershipAndDraft(requisition);
        if (guard != null) return guard;

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
