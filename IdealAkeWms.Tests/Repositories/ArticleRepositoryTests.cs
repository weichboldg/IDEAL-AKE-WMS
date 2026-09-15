using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task SearchAsync_FindsArticle_ByMatchcodeSubstring()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.AddRange(
            CreateArticle("A1", "SCHRAUBE-M6"),
            CreateArticle("A2", "MUTTER-M6"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var result = await repo.SearchAsync("SCHRAUBE");

        result.Should().ContainSingle(a => a.ArticleNumber == "A1");
    }

    [Fact]
    public async Task SearchAsync_StillFindsArticle_ByArticleNumberAndDescription()
    {
        using var context = TestDbContextFactory.Create();
        var byNumber = CreateArticle("XYZ-1", "MC-1");
        var byDescription = CreateArticle("A2", "MC-2");
        byDescription.Description = "Sonderteil";
        context.Articles.AddRange(byNumber, byDescription);
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);

        (await repo.SearchAsync("XYZ")).Should().ContainSingle(a => a.ArticleNumber == "XYZ-1");
        (await repo.SearchAsync("Sonderteil")).Should().ContainSingle(a => a.ArticleNumber == "A2");
    }

    [Fact]
    public async Task GetPaginatedAsync_Search_FindsArticle_ByMatchcodeSubstring()
    {
        using var context = TestDbContextFactory.Create();
        context.Articles.AddRange(
            CreateArticle("A1", "SCHRAUBE-M6"),
            CreateArticle("A2", "MUTTER-M6"));
        await context.SaveChangesAsync();

        var repo = new ArticleRepository(context);
        var (items, totalCount) = await repo.GetPaginatedAsync(1, 50, "SCHRAUBE");

        totalCount.Should().Be(1);
        items.Should().ContainSingle(a => a.ArticleNumber == "A1");
    }

    // ApplyArticleColumnFilter nutzt (analog zu "description"/"unit"/"article-group"/"category",
    // Konsistenz vor F7 an dieser Stelle) EF.Functions.Like(...) innerhalb patterns.Any(...) — das
    // ist unter InMemory grundsaetzlich nicht ausfuehrbar (nicht matchcode-spezifisch, betrifft alle
    // Article-Spaltenfilter gleichermassen; EF.Functions.Like wirft bei Client-Evaluation immer eine
    // InvalidOperationException). Wie in ProductionOrderRepository.BuildLeitstandQuery geprueft:
    // SQL-Uebersetzung gegen einen nie verbundenen SqlServer-Context via ToQueryString().
    [Fact]
    public void ColumnFilter_Matchcode_SqlServer_TranslatesLikeWithNullGuard()
    {
        using var context = CreateSqlServerContext();
        var repo = new ArticleRepository(context);

        var sql = repo.BuildArticleQuery(null, new Dictionary<string, string> { ["matchcode"] = "SCHRAUBE" })
            .ToQueryString();

        sql.Should().Contain("LIKE", "der Filter muss als SQL LIKE uebersetzt werden");
        sql.Should().Contain("Matchcode");
        sql.Should().Contain("IS NOT NULL", "Null-Matchcode darf im Positiv-Fall nicht treffen");
    }

    [Fact]
    public void ColumnFilter_Matchcode_Negated_SqlServer_PreservesNullGuardUnderNot()
    {
        using var context = CreateSqlServerContext();
        var repo = new ArticleRepository(context);

        var sql = repo.BuildArticleQuery(null, new Dictionary<string, string> { ["matchcode"] = "!SCHRAUBE" })
            .ToQueryString();

        sql.Should().Contain("LIKE");
        sql.Should().Contain("NOT", "der Negativfall muss als NOT uebersetzt werden");
        sql.Should().Contain("IS NULL", "Null-Matchcode muss im Negativ-Fall (kein Treffer) enthalten bleiben");
    }

    [Fact]
    public void SearchAsync_And_GetPaginatedAsync_SqlServer_TranslateMatchcodeFreetext()
    {
        using var context = CreateSqlServerContext();
        var repo = new ArticleRepository(context);

        var sql = repo.BuildArticleQuery("SCHRAUBE", null).ToQueryString();

        sql.Should().Contain("Matchcode", "die Freitextsuche muss Matchcode als ODER-Bedingung uebersetzen");
    }

    /// <summary>
    /// Context gegen den SqlServer-Provider mit einer nicht aufloesbaren Server-Adresse — es wird NIE
    /// eine Verbindung geoeffnet, <c>ToQueryString()</c> kompiliert nur den Ausdrucksbaum zu SQL-Text.
    /// Ausschliesslich fuer SQL-Uebersetzungs-Checks (Muster aus ProductionOrderRepositoryTests).
    /// </summary>
    private static ApplicationDbContext CreateSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1")
            .Options;
        return new ApplicationDbContext(options);
    }
}
