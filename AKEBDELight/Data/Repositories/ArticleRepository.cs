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
}
