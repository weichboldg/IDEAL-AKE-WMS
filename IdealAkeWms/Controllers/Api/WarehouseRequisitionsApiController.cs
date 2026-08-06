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
    private readonly IProductionWorkplaceRepository _workplaces;

    public WarehouseRequisitionsApiController(
        IWarehouseRequisitionRepository repo, IArticleRepository articles,
        IStockMovementRepository stock, ICurrentUserService user,
        IAppSettingRepository settings, IProductionWorkplaceRepository workplaces)
    {
        _repo = repo;
        _articles = articles;
        _stock = stock;
        _user = user;
        _settings = settings;
        _workplaces = workplaces;
    }

    public record AddItemRequest(string ArticleNumber, decimal Quantity);
    public record UpdateItemRequest(decimal Quantity);
    public record UpdateCommentRequest(string? Comment);
    public record AddDummyItemRequest(string? Description, decimal Quantity);

    public record QuickAddItem(string ArticleNumber, decimal Quantity);
    public record QuickAddRequest(List<QuickAddItem> Items);
    public record QuickAddSkipped(string ArticleNumber, string Reason);
    public record QuickAddResponse(int? LagerRequisitionId, int? GlasRequisitionId,
        int AddedLager, int AddedGlas, List<QuickAddSkipped> Skipped);

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

    [HttpPut("{id:int}/comment")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentRequest body)
    {
        var requisition = await _repo.GetByIdAsync(id, includeItems: false);
        if (requisition == null) return NotFound();

        var guard = CheckOwnershipAndDraft(requisition);
        if (guard != null) return guard;

        await _repo.SaveCommentAsync(id, body.Comment, _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }

    /// <summary>
    /// DUMMY-Position (Teil-7): legt bei unbekannter EK-Nummer eine Position auf dem einen
    /// geseedeten DUMMY-Artikel an. Die Bezeichnung ist Pflicht und muss vom Default-Seed
    /// abweichen; sie landet als Positions-Snapshot auf ArticleDescription (Article.Description
    /// bleibt unveraendert). Umgeht den Glas/Lager-Gruppen-Guard (DUMMY ist typ-neutral) und
    /// den Duplikat-Guard (mehrere DUMMY-Positionen je Bestellung erlaubt).
    /// </summary>
    [HttpPost("{id:int}/items/dummy")]
    public async Task<IActionResult> AddDummyItem(int id, [FromBody] AddDummyItemRequest body)
    {
        var requisition = await _repo.GetByIdAsync(id, includeItems: false);
        if (requisition == null) return NotFound();

        var guard = CheckOwnershipAndDraft(requisition);
        if (guard != null) return guard;

        var dummy = await _articles.GetByArticleNumberAsync(Article.DummyArticleNumber);
        if (dummy == null)
            return BadRequest(new { error = "DUMMY-Artikel fehlt, Seed nicht eingespielt." });

        var description = body.Description?.Trim() ?? string.Empty;
        if (description.Length == 0
            || string.Equals(description, Article.DummyDefaultDescription.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Bitte eine eigene Bezeichnung eingeben." });
        }

        try
        {
            await _repo.AddItemAsync(id, Article.DummyArticleNumber, description, null,
                body.Quantity, _user.GetDisplayName(), _user.GetWindowsUserName());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok();
    }

    /// <summary>
    /// BOM-Quick-Add (v1.25.0): EIN Endpunkt fuer Einzel (1 Item) UND Bulk.
    /// Typ automatisch aus der Artikelgruppe (Glas-Gruppe -> Glas, sonst -> Lager;
    /// gemeinsame/EUZ -> Lager). Rechte je Typ. Pro Typ genau EIN Draft je Lauf
    /// (offener Draft wiederverwendet, sonst neu mit User-Default-Werkbank).
    /// Ungueltige Items landen in skipped (kein Abbruch); alles skipped -> BadRequest.
    /// </summary>
    [HttpPost("quick-add")]
    public async Task<IActionResult> QuickAdd([FromBody] QuickAddRequest body)
    {
        var items = body?.Items ?? new List<QuickAddItem>();
        if (items.Count == 0)
            return BadRequest(new { error = "Keine Positionen uebergeben." });

        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        var winName = _user.GetWindowsUserName();

        var glasGroups = GlasArticleGroupFilter.ParseGroups(
            await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));

        var canOrderLager = await _user.CanOrderLagerAsync();
        var canOrderGlas = await _user.CanOrderGlasAsync();

        int? lagerReqId = null, glasReqId = null;
        int addedLager = 0, addedGlas = 0;
        var skipped = new List<QuickAddSkipped>();

        // Werkbank (erste zugeordnete) nur bei Bedarf und nur einmal aufloesen.
        int? resolvedWorkplaceId = null;
        bool workplaceResolved = false;
        async Task<int?> ResolveWorkplaceAsync()
        {
            if (workplaceResolved) return resolvedWorkplaceId;
            workplaceResolved = true;
            var wps = await _workplaces.GetByUserIdAsync(userId);
            resolvedWorkplaceId = wps.Count > 0 ? wps[0].Id : (int?)null;
            return resolvedWorkplaceId;
        }

        foreach (var item in items)
        {
            var articleNumber = item.ArticleNumber?.Trim() ?? string.Empty;

            if (item.Quantity <= 0)
            {
                skipped.Add(new QuickAddSkipped(articleNumber, "Menge muss groesser 0 sein."));
                continue;
            }

            var article = await _articles.GetByArticleNumberAsync(articleNumber);
            if (article == null)
            {
                skipped.Add(new QuickAddSkipped(articleNumber, "Artikel nicht gefunden."));
                continue;
            }

            // Typ automatisch: reine Glas-Gruppe -> Glas, sonst Lager (gemeinsame/EUZ -> Lager).
            var norm = GlasArticleGroupFilter.NormalizeGroup(article.ArticleGroup);
            var type = glasGroups.Contains(norm)
                ? WarehouseRequisitionType.Glas
                : WarehouseRequisitionType.Lager;

            if (type == WarehouseRequisitionType.Glas && !canOrderGlas)
            {
                skipped.Add(new QuickAddSkipped(articleNumber, "Keine Glasbestell-Berechtigung."));
                continue;
            }
            if (type == WarehouseRequisitionType.Lager && !canOrderLager)
            {
                skipped.Add(new QuickAddSkipped(articleNumber, "Keine Lagerbestell-Berechtigung."));
                continue;
            }

            // Draft je Typ lazy.
            int reqId;
            if (type == WarehouseRequisitionType.Glas)
            {
                if (glasReqId == null)
                {
                    var existing = await _repo.GetOpenDraftForUserAndTypeAsync(userId, type);
                    if (existing != null)
                    {
                        glasReqId = existing.Id;
                    }
                    else
                    {
                        var wpId = await ResolveWorkplaceAsync();
                        if (wpId == null)
                            return BadRequest(new { error = "Bitte Standard-Werkbank im Profil hinterlegen." });
                        glasReqId = await _repo.CreateDraftAsync(wpId.Value, type, userId, displayName, winName);
                    }
                }
                reqId = glasReqId.Value;
            }
            else
            {
                if (lagerReqId == null)
                {
                    var existing = await _repo.GetOpenDraftForUserAndTypeAsync(userId, type);
                    if (existing != null)
                    {
                        lagerReqId = existing.Id;
                    }
                    else
                    {
                        var wpId = await ResolveWorkplaceAsync();
                        if (wpId == null)
                            return BadRequest(new { error = "Bitte Standard-Werkbank im Profil hinterlegen." });
                        lagerReqId = await _repo.CreateDraftAsync(wpId.Value, type, userId, displayName, winName);
                    }
                }
                reqId = lagerReqId.Value;
            }

            try
            {
                await _repo.AddItemAsync(reqId, articleNumber, article.Description ?? string.Empty,
                    article.Unit, item.Quantity, displayName, winName);
                if (type == WarehouseRequisitionType.Glas) addedGlas++; else addedLager++;
            }
            catch (InvalidOperationException ex)
            {
                // z. B. Artikel bereits in dieser Bestellung.
                skipped.Add(new QuickAddSkipped(articleNumber, ex.Message));
            }
        }

        var response = new QuickAddResponse(lagerReqId, glasReqId, addedLager, addedGlas, skipped);
        if (addedLager == 0 && addedGlas == 0)
            return BadRequest(response);
        return Ok(response);
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
