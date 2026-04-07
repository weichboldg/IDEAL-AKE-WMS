using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ArticleAttributeRepositoryTests
{
    private static ArticleAttributeDefinition CreateDefinition(
        string name, AttributeType type = AttributeType.Boolean, int sortOrder = 0, bool isActive = true)
        => new()
        {
            Name = name,
            AttributeType = type,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    private static Article CreateArticle(string number)
        => new()
        {
            ArticleNumber = number,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetAllDefinitionsAsync_ReturnsSortedWithOptions()
    {
        using var context = TestDbContextFactory.Create();
        var d1 = CreateDefinition("Material", AttributeType.Dropdown, 1);
        var d2 = CreateDefinition("Laserschnitt", AttributeType.Boolean, 0);
        context.ArticleAttributeDefinitions.AddRange(d1, d2);
        await context.SaveChangesAsync();

        context.ArticleAttributeOptions.AddRange(
            new ArticleAttributeOption { ArticleAttributeDefinitionId = d1.Id, Value = "Alu", SortOrder = 0 },
            new ArticleAttributeOption { ArticleAttributeDefinitionId = d1.Id, Value = "Stahl", SortOrder = 1 });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetAllDefinitionsAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Laserschnitt"); // SortOrder 0
        result[1].Name.Should().Be("Material");      // SortOrder 1
        result[1].Options.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveDefinitionsOrderedAsync_ExcludesInactive()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleAttributeDefinitions.AddRange(
            CreateDefinition("Active", isActive: true),
            CreateDefinition("Inactive", isActive: false));
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetActiveDefinitionsOrderedAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task DefinitionHasValuesAsync_ReturnsTrueWhenValuesExist()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.DefinitionHasValuesAsync(def.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DefinitionHasValuesAsync_ReturnsFalseWhenNoValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Empty");
        context.ArticleAttributeDefinitions.Add(def);
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.DefinitionHasValuesAsync(def.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task OptionIsInUseAsync_DetectsUsedOption()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Material", AttributeType.Dropdown);
        context.ArticleAttributeDefinitions.Add(def);
        await context.SaveChangesAsync();

        var option = new ArticleAttributeOption { ArticleAttributeDefinitionId = def.Id, Value = "Alu" };
        context.ArticleAttributeOptions.Add(option);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            SelectedOptionId = option.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        (await repo.OptionIsInUseAsync(option.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task GetNextSortOrderAsync_ReturnsMaxPlusOne()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleAttributeDefinitions.AddRange(
            CreateDefinition("A", sortOrder: 0),
            CreateDefinition("B", sortOrder: 5));
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var next = await repo.GetNextSortOrderAsync();
        next.Should().Be(6);
    }

    [Fact]
    public async Task GetNextSortOrderAsync_ReturnsZeroWhenEmpty()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleAttributeRepository(context);
        var next = await repo.GetNextSortOrderAsync();
        next.Should().Be(0);
    }

    [Fact]
    public async Task SaveValuesAsync_CreatesNewValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id, BooleanValue = true }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().HaveCount(1);
        values[0].BooleanValue.Should().BeTrue();
    }

    [Fact]
    public async Task SaveValuesAsync_UpdatesExistingValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id, BooleanValue = true }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().HaveCount(1);
        values[0].BooleanValue.Should().BeTrue();
    }

    [Fact]
    public async Task SaveValuesAsync_RemovesClearedValues()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var article = CreateArticle("ART1");
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.Add(new ArticleAttributeValue
        {
            ArticleId = article.Id,
            ArticleAttributeDefinitionId = def.Id,
            BooleanValue = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        // Send value without BooleanValue or SelectedOptionId — should remove
        await repo.SaveValuesAsync(article.Id, new List<ArticleAttributeValue>
        {
            new() { ArticleAttributeDefinitionId = def.Id }
        }, "TestUser", "DOMAIN\\test");

        var values = await repo.GetValuesByArticleIdAsync(article.Id);
        values.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValuesByArticleIdsAsync_BatchLoadsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var def = CreateDefinition("Test");
        context.ArticleAttributeDefinitions.Add(def);
        var a1 = CreateArticle("ART1");
        var a2 = CreateArticle("ART2");
        context.Articles.AddRange(a1, a2);
        await context.SaveChangesAsync();

        context.ArticleAttributeValues.AddRange(
            new ArticleAttributeValue { ArticleId = a1.Id, ArticleAttributeDefinitionId = def.Id, BooleanValue = true, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new ArticleAttributeValue { ArticleId = a2.Id, ArticleAttributeDefinitionId = def.Id, BooleanValue = false, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetValuesByArticleIdsAsync(new List<int> { a1.Id, a2.Id });

        result.Should().HaveCount(2);
        result[a1.Id].Should().HaveCount(1);
        result[a2.Id].Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCategoryNamesByArticleNumbersAsync_ReturnsCategoryNames()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new ArticleCategory { Name = "Bleche", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" };
        context.ArticleCategories.Add(cat);
        await context.SaveChangesAsync();

        context.Articles.AddRange(
            new Article { ArticleNumber = "A1", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A2", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleAttributeRepository(context);
        var result = await repo.GetCategoryNamesByArticleNumbersAsync(new List<string> { "A1", "A2" });

        result.Should().HaveCount(2);
        result["A1"].Should().Be("Bleche");
        result["A2"].Should().BeNull();
    }
}
