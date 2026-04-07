using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ArticleCategoryRepositoryTests
{
    private static ArticleCategory CreateCategory(string name, string? source = null, string? description = null)
        => new()
        {
            Name = name,
            Description = description,
            Source = source,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetAllOrderedAsync_ReturnsAlphabetically()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.AddRange(
            CreateCategory("Zuschnitt"),
            CreateCategory("Blechtafel_AKE"),
            CreateCategory("Lackierteile"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var result = await repo.GetAllOrderedAsync();

        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Blechtafel_AKE");
        result[1].Name.Should().Be("Lackierteile");
        result[2].Name.Should().Be("Zuschnitt");
    }

    [Fact]
    public async Task GetByNameAsync_FindsCorrectCategory()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("Blechtafel_AKE", "OSEON", "Blechtafeln"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var result = await repo.GetByNameAsync("Blechtafel_AKE");

        result.Should().NotBeNull();
        result!.Source.Should().Be("OSEON");
        result.Description.Should().Be("Blechtafeln");
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleCategoryRepository(context);

        var result = await repo.GetByNameAsync("NOPE");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryNameToIdMapAsync_ReturnsDictionary()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.AddRange(
            CreateCategory("Cat1"),
            CreateCategory("Cat2"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var map = await repo.GetCategoryNameToIdMapAsync();

        map.Should().HaveCount(2);
        map.Should().ContainKey("Cat1");
        map.Should().ContainKey("Cat2");
    }

    [Fact]
    public async Task GetArticleCountByCategoryAsync_CountsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var cat = CreateCategory("TestCat");
        context.ArticleCategories.Add(cat);
        await context.SaveChangesAsync();

        context.Articles.AddRange(
            new Article { ArticleNumber = "A1", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A2", ArticleCategoryId = cat.Id, CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" },
            new Article { ArticleNumber = "A3", CreatedAt = DateTime.UtcNow, CreatedBy = "Test", CreatedByWindows = "Test" });
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var counts = await repo.GetArticleCountByCategoryAsync();

        counts.Should().ContainKey(cat.Id);
        counts[cat.Id].Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_PersistsCategory()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleCategoryRepository(context);

        await repo.AddAsync(CreateCategory("Neukat", "OSEON"));

        context.ArticleCategories.Should().HaveCount(1);
        var saved = context.ArticleCategories.First();
        saved.Name.Should().Be("Neukat");
        saved.Source.Should().Be("OSEON");
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("ToDelete"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);
        var cat = await repo.GetByNameAsync("ToDelete");
        await repo.DeleteAsync(cat!.Id);

        context.ArticleCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByNameAsync_ReturnsTrueForExisting()
    {
        using var context = TestDbContextFactory.Create();
        context.ArticleCategories.Add(CreateCategory("Exists"));
        await context.SaveChangesAsync();

        var repo = new ArticleCategoryRepository(context);

        (await repo.ExistsByNameAsync("Exists")).Should().BeTrue();
        (await repo.ExistsByNameAsync("Nope")).Should().BeFalse();
    }
}
