using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;

namespace AKEBDELight.Controllers;

[Route("api/articles")]
[ApiController]
public class ArticlesApiController : ControllerBase
{
    private readonly IArticleRepository _articleRepository;

    public ArticlesApiController(IArticleRepository articleRepository)
    {
        _articleRepository = articleRepository;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int limit = 50)
    {
        var results = await _articleRepository.SearchAsync(q, limit);
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
