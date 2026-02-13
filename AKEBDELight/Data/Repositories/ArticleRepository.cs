using AKEBDELight.Models;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

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

    public async Task<(List<Article> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.ArticleNumber.Contains(search) ||
                                     (a.Description != null && a.Description.Contains(search)));
        }
        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(a => a.ArticleNumber)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return (items, totalCount);
    }
}
