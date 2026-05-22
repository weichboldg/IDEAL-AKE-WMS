using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Article>> GetAllOrderedAsync()
    {
        return await _dbSet.OrderBy(a => a.ArticleNumber).ToListAsync();
    }

    public async Task<List<Article>> SearchAsync(string? query, int limit = 50)
    {
        var q = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(a => a.ArticleNumber.Contains(query) ||
                             (a.Description != null && a.Description.Contains(query)));
        }
        return await q.OrderBy(a => a.ArticleNumber).Take(limit).ToListAsync();
    }

    public async Task<Article?> GetByArticleNumberAsync(string articleNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.ArticleNumber == articleNumber);
    }

    public async Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(
        int page, int pageSize, string? search,
        IReadOnlyDictionary<string, string>? columnFilters = null)
    {
        var query = _dbSet.Include(a => a.ArticleCategory).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.ArticleNumber.Contains(search) ||
                                     (a.Description != null && a.Description.Contains(search)));
        }

        if (columnFilters != null)
        {
            foreach (var (key, raw) in columnFilters)
            {
                var (tokens, negate) = Services.ColumnFilterHelper.Parse(raw);
                if (tokens.Count == 0) continue;
                query = ApplyArticleColumnFilter(query, key, tokens, negate);
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(a => a.ArticleNumber)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return (items, totalCount);
    }

    private static IQueryable<Article> ApplyArticleColumnFilter(
        IQueryable<Article> q, string key, List<string> tokens, bool negate)
    {
        var patterns = tokens.Select(t => $"%{t}%").ToList();
        return key switch
        {
            "article-number" => negate
                ? q.Where(a => !patterns.Any(p => EF.Functions.Like(a.ArticleNumber, p)))
                : q.Where(a => patterns.Any(p => EF.Functions.Like(a.ArticleNumber, p))),
            "description" => negate
                ? q.Where(a => a.Description == null || !patterns.Any(p => EF.Functions.Like(a.Description, p)))
                : q.Where(a => a.Description != null && patterns.Any(p => EF.Functions.Like(a.Description, p))),
            "unit" => negate
                ? q.Where(a => a.Unit == null || !patterns.Any(p => EF.Functions.Like(a.Unit, p)))
                : q.Where(a => a.Unit != null && patterns.Any(p => EF.Functions.Like(a.Unit, p))),
            "article-group" => negate
                ? q.Where(a => a.ArticleGroup == null || !patterns.Any(p => EF.Functions.Like(a.ArticleGroup, p)))
                : q.Where(a => a.ArticleGroup != null && patterns.Any(p => EF.Functions.Like(a.ArticleGroup, p))),
            "category" => negate
                ? q.Where(a => a.ArticleCategory == null || !patterns.Any(p => EF.Functions.Like(a.ArticleCategory.Name, p)))
                : q.Where(a => a.ArticleCategory != null && patterns.Any(p => EF.Functions.Like(a.ArticleCategory.Name, p))),
            _ => q
        };
    }
}
