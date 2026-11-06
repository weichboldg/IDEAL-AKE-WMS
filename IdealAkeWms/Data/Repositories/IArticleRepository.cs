using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleRepository : IRepository<Article>
{
    Task<List<Article>> GetAllOrderedAsync();
    Task<List<Article>> SearchAsync(string? query, int limit = 50);
    Task<Article?> GetByArticleNumberAsync(string articleNumber);
    Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search,
        IReadOnlyDictionary<string, string>? columnFilters = null);
    Task<Dictionary<string, string?>> GetMatchcodesByArticleNumbersAsync(IReadOnlyCollection<string> articleNumbers);
    /// <summary>Exakt, getrimmt, ohne Gross/Klein; DUMMY ausgeschlossen. Bis zu <paramref name="limit"/> Treffer nach Artikelnummer -
    /// der Aufrufer unterscheidet keins/eindeutig/mehrdeutig (Matchcode ist nicht eindeutig, nie still den ersten nehmen).</summary>
    Task<List<Article>> GetByMatchcodeExactAsync(string matchcode, int limit);
    /// <summary>Sage-Zusatzfelder (Satellit ArticleSageDetails, nur ueber SData gelesen); null = nie ueber SData gelesen.</summary>
    Task<ArticleSageDetail?> GetSageDetailAsync(int articleId);
}
