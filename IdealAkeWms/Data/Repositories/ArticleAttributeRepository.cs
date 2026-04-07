using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class ArticleAttributeRepository : IArticleAttributeRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleAttributeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========== Definitions ==========

    public async Task<List<ArticleAttributeDefinition>> GetAllDefinitionsAsync()
    {
        return await _context.ArticleAttributeDefinitions
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<ArticleAttributeDefinition>> GetActiveDefinitionsOrderedAsync()
    {
        return await _context.ArticleAttributeDefinitions
            .Where(d => d.IsActive)
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<ArticleAttributeDefinition?> GetDefinitionByIdAsync(int id)
    {
        return await _context.ArticleAttributeDefinitions
            .Include(d => d.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddDefinitionAsync(ArticleAttributeDefinition definition)
    {
        _context.ArticleAttributeDefinitions.Add(definition);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDefinitionAsync(ArticleAttributeDefinition definition)
    {
        _context.ArticleAttributeDefinitions.Update(definition);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDefinitionAsync(int id)
    {
        var definition = await _context.ArticleAttributeDefinitions.FindAsync(id);
        if (definition != null)
        {
            _context.ArticleAttributeDefinitions.Remove(definition);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DefinitionHasValuesAsync(int definitionId)
    {
        return await _context.ArticleAttributeValues
            .AnyAsync(v => v.ArticleAttributeDefinitionId == definitionId);
    }

    public async Task<bool> DefinitionExistsByNameAsync(string name)
    {
        return await _context.ArticleAttributeDefinitions
            .AnyAsync(d => d.Name == name);
    }

    public async Task<int> GetNextSortOrderAsync()
    {
        var max = await _context.ArticleAttributeDefinitions
            .MaxAsync(d => (int?)d.SortOrder);
        return (max ?? -1) + 1;
    }

    // ========== Options ==========

    public async Task<List<ArticleAttributeOption>> GetOptionsByDefinitionIdAsync(int definitionId)
    {
        return await _context.ArticleAttributeOptions
            .Where(o => o.ArticleAttributeDefinitionId == definitionId)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();
    }

    public async Task AddOptionAsync(ArticleAttributeOption option)
    {
        _context.ArticleAttributeOptions.Add(option);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOptionAsync(int id)
    {
        var option = await _context.ArticleAttributeOptions.FindAsync(id);
        if (option != null)
        {
            _context.ArticleAttributeOptions.Remove(option);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> OptionIsInUseAsync(int optionId)
    {
        return await _context.ArticleAttributeValues
            .AnyAsync(v => v.SelectedOptionId == optionId);
    }

    // ========== Values ==========

    public async Task<List<ArticleAttributeValue>> GetValuesByArticleIdAsync(int articleId)
    {
        return await _context.ArticleAttributeValues
            .Include(v => v.ArticleAttributeDefinition)
            .Include(v => v.SelectedOption)
            .Where(v => v.ArticleId == articleId)
            .ToListAsync();
    }

    public async Task<Dictionary<int, List<ArticleAttributeValue>>> GetValuesByArticleIdsAsync(List<int> articleIds)
    {
        if (!articleIds.Any())
            return new Dictionary<int, List<ArticleAttributeValue>>();

        var values = await _context.ArticleAttributeValues
            .Include(v => v.ArticleAttributeDefinition)
            .Include(v => v.SelectedOption)
            .Where(v => articleIds.Contains(v.ArticleId))
            .ToListAsync();

        return values.GroupBy(v => v.ArticleId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task SaveValuesAsync(int articleId, List<ArticleAttributeValue> newValues, string userName, string windowsUser)
    {
        var existingValues = await _context.ArticleAttributeValues
            .Where(v => v.ArticleId == articleId)
            .ToListAsync();

        foreach (var newVal in newValues)
        {
            var existing = existingValues.FirstOrDefault(e => e.ArticleAttributeDefinitionId == newVal.ArticleAttributeDefinitionId);

            // Skip if value is empty (no boolean set, no option selected)
            var hasValue = newVal.BooleanValue.HasValue || newVal.SelectedOptionId.HasValue;

            if (existing != null)
            {
                if (!hasValue)
                {
                    // Remove value if cleared
                    _context.ArticleAttributeValues.Remove(existing);
                }
                else
                {
                    existing.BooleanValue = newVal.BooleanValue;
                    existing.SelectedOptionId = newVal.SelectedOptionId;
                    existing.ModifiedAt = DateTime.Now;
                    existing.ModifiedBy = userName;
                    existing.ModifiedByWindows = windowsUser;
                }
            }
            else if (hasValue)
            {
                // Create new value
                newVal.ArticleId = articleId;
                newVal.CreatedAt = DateTime.Now;
                newVal.CreatedBy = userName;
                newVal.CreatedByWindows = windowsUser;
                _context.ArticleAttributeValues.Add(newVal);
            }
        }

        await _context.SaveChangesAsync();
    }

    // ========== Batch for BOM ==========

    public async Task<Dictionary<string, string?>> GetCategoryNamesByArticleNumbersAsync(List<string> articleNumbers)
    {
        if (!articleNumbers.Any())
            return new Dictionary<string, string?>();

        return await _context.Articles
            .Where(a => articleNumbers.Contains(a.ArticleNumber))
            .Select(a => new { a.ArticleNumber, CategoryName = a.ArticleCategory != null ? a.ArticleCategory.Name : null })
            .ToDictionaryAsync(a => a.ArticleNumber, a => a.CategoryName);
    }
}
