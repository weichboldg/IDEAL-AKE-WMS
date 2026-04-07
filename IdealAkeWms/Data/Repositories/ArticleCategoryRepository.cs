using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleCategoryRepository : IArticleCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArticleCategory>> GetAllOrderedAsync()
    {
        return await _context.ArticleCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ArticleCategory?> GetByIdAsync(int id)
    {
        return await _context.ArticleCategories.FindAsync(id);
    }

    public async Task<ArticleCategory?> GetByNameAsync(string name)
    {
        return await _context.ArticleCategories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync()
    {
        return await _context.ArticleCategories
            .ToDictionaryAsync(c => c.Name, c => c.Id);
    }

    public async Task<Dictionary<int, int>> GetArticleCountByCategoryAsync()
    {
        var articles = await _context.Articles
            .Where(a => a.ArticleCategoryId != null)
            .Select(a => a.ArticleCategoryId!.Value)
            .ToListAsync();

        return articles
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task AddAsync(ArticleCategory category)
    {
        _context.ArticleCategories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ArticleCategory category)
    {
        _context.ArticleCategories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.ArticleCategories.FindAsync(id);
        if (category != null)
        {
            _context.ArticleCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.ArticleCategories.AnyAsync(c => c.Name == name);
    }
}
