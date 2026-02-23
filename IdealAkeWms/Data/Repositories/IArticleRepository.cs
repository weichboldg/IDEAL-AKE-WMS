using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleRepository : IRepository<Article>
{
    Task<List<Article>> GetAllOrderedAsync();
    Task<List<Article>> SearchAsync(string? query, int limit = 50);
    Task<Article?> GetByArticleNumberAsync(string articleNumber);
    Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search);
}
