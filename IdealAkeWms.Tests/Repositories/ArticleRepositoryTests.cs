using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class ArticleRepositoryTests
{
    private static Article CreateArticle(string articleNumber, string? matchcode = null)
        => new()
        {
            ArticleNumber = articleNumber,
            Matchcode = matchcode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            CreatedByWindows = "Test"
        };

    [Fact]
    public async Task GetMatchcodesByArticleNumbersAsync_ReturnsOnlyExistingArticles()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.AddRange(
            CreateArticle("A1", "MC-A1"),
            CreateArticle("A2", "MC-A2"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.GetMatchcodesByArticleNumbersAsync(new[] { "A1", "A2", "MISSING" });

        result.Should().HaveCount(2);
        result.Should().ContainKey("A1").WhoseValue.Should().Be("MC-A1");
        result.Should().ContainKey("A2").WhoseValue.Should().Be("MC-A2");
        result.Should().NotContainKey("MISSING");
    }

    [Fact]
    public async Task GetMatchcodesByArticleNumbersAsync_EmptyMatchcode_ReturnsNullValue()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.Add(CreateArticle("A1", matchcode: null));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.GetMatchcodesByArticleNumbersAsync(new[] { "A1" });

        result.Should().ContainKey("A1");
        result["A1"].Should().BeNull();
    }

    [Fact]
    public async Task GetMatchcodesByArticleNumbersAsync_EmptyInput_ReturnsEmptyDict()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new ArticleRepository(context);

        var result = await repo.GetMatchcodesByArticleNumbersAsync(Array.Empty<string>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByMatchcodeExactAsync_ReturnsExactMatch()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.AddRange(
            CreateArticle("A1", "SCHRAUBE"),
            CreateArticle("A2", "SCHRAUBENZIEHER"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.GetByMatchcodeExactAsync("SCHRAUBE");

        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("A1");
    }

    [Fact]
    public async Task GetByMatchcodeExactAsync_SubstringOnly_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.Add(CreateArticle("A1", "SCHRAUBENZIEHER"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.GetByMatchcodeExactAsync("SCHRAUBE");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMatchcodeExactAsync_NoMatch_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.Add(CreateArticle("A1", "SCHRAUBE"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.GetByMatchcodeExactAsync("NOPE");

        result.Should().BeNull();
    }
}
