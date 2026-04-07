using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IArticleAttributeRepository
{
    // Definitions
    Task<List<ArticleAttributeDefinition>> GetAllDefinitionsAsync();
    Task<List<ArticleAttributeDefinition>> GetActiveDefinitionsOrderedAsync();
    Task<ArticleAttributeDefinition?> GetDefinitionByIdAsync(int id);
    Task AddDefinitionAsync(ArticleAttributeDefinition definition);
    Task UpdateDefinitionAsync(ArticleAttributeDefinition definition);
    Task DeleteDefinitionAsync(int id);
    Task<bool> DefinitionHasValuesAsync(int definitionId);
    Task<bool> DefinitionExistsByNameAsync(string name);
    Task<int> GetNextSortOrderAsync();

    // Options
    Task<List<ArticleAttributeOption>> GetOptionsByDefinitionIdAsync(int definitionId);
    Task AddOptionAsync(ArticleAttributeOption option);
    Task DeleteOptionAsync(int id);
    Task<bool> OptionIsInUseAsync(int optionId);

    // Values
    Task<List<ArticleAttributeValue>> GetValuesByArticleIdAsync(int articleId);
    Task<Dictionary<int, List<ArticleAttributeValue>>> GetValuesByArticleIdsAsync(List<int> articleIds);
    Task SaveValuesAsync(int articleId, List<ArticleAttributeValue> values, string userName, string windowsUser);

    // Batch for BOM
    Task<Dictionary<string, string?>> GetCategoryNamesByArticleNumbersAsync(List<string> articleNumbers);
}
