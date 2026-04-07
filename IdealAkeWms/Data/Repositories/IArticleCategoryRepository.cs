using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleCategoryRepository
{
    Task<List<ArticleCategory>> GetAllOrderedAsync();
    Task<ArticleCategory?> GetByIdAsync(int id);
    Task<ArticleCategory?> GetByNameAsync(string name);
    Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync();
    Task<Dictionary<int, int>> GetArticleCountByCategoryAsync();
    Task AddAsync(ArticleCategory category);
    Task UpdateAsync(ArticleCategory category);
    Task DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
}
