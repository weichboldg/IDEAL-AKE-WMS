using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[Route("api/articles")]
[ApiController]
public class ArticlesApiController : ControllerBase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IAppSettingRepository _settings;

    public ArticlesApiController(IArticleRepository articleRepository, IAppSettingRepository settings)
    {
        _articleRepository = articleRepository;
        _settings = settings;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 50,
        [FromQuery] string? type = null)
    {
        IEnumerable<Article> results;
        if (Enum.TryParse<WarehouseRequisitionType>(type, ignoreCase: true, out var reqType))
        {
            // Typ-gescopte Suche (Lager-/Glas-Bestellung): erst breiter suchen,
            // dann nach erlaubten Artikelgruppen filtern, dann auf limit kappen.
            var glasGroups = GlasArticleGroupFilter.ParseGroups(
                await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));
            var sharedGroups = GlasArticleGroupFilter.ParseGroups(
                await _settings.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen));
            var raw = await _articleRepository.SearchAsync(q, Math.Max(limit * 5, 100));
            results = raw
                // DUMMY nie in die regulaere Suche einmischen — er darf ausschliesslich ueber den
                // „Artikel nicht gefunden -> DUMMY-Position"-Zweig (mit Pflicht-Bezeichnung) angelegt
                // werden, sonst wuerde die Default-Bezeichnung ungeaendert uebernommen (Teil-7).
                .Where(a => a.ArticleNumber != Article.DummyArticleNumber)
                .Where(a => GlasArticleGroupFilter.IsAllowedForType(a.ArticleGroup, reqType, glasGroups, sharedGroups))
                .Take(limit);
        }
        else
        {
            var raw = await _articleRepository.SearchAsync(q, limit + 1);
            results = raw
                .Where(a => a.ArticleNumber != Article.DummyArticleNumber)
                .Take(limit);
        }
        return Ok(results.Select(a => new
        {
            id = a.Id,
            text = a.ArticleNumber + (a.Description != null ? " - " + a.Description : "")
        }));
    }

    [HttpGet("by-number/{articleNumber}")]
    public async Task<IActionResult> GetByNumber(string articleNumber)
    {
        var article = await _articleRepository.GetByArticleNumberAsync(articleNumber);
        if (article == null)
            return NotFound();
        return Ok(new
        {
            id = article.Id,
            text = article.ArticleNumber + (article.Description != null ? " - " + article.Description : "")
        });
    }
}
