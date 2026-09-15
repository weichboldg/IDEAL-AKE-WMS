using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Tests fuer den gemeinsamen Matchcode-Batch-Lookup (Task 2, Artikelstamm) — genutzt von allen
/// fuenf FA-Zeilenlisten (ProductionOrders, PickingLeitstand, FaCompletion, FaWorklist, Picking).
/// Deckt (a) den Article-Join per ArticleNumber und (b) die AK-14-Zaehlung (fehlende Artikelstamm-
/// Treffer, EINE Zahl je Aufruf, nicht je Zeile) ab.
/// </summary>
public class MatchcodeLookupTests
{
    private static Article MakeArticle(string number, string? matchcode) => new()
    {
        ArticleNumber = number,
        Matchcode = matchcode,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test",
        CreatedByWindows = "test",
    };

    /// <summary>Zaehlt Information-Log-Eintraege, deren formatierte Nachricht <paramref name="fragment"/> enthaelt.</summary>
    private static int InfosContaining(Mock<ILogger> logger, string fragment) =>
        logger.Invocations.Count(i =>
            i.Method.Name == nameof(ILogger.Log)
            && (LogLevel)i.Arguments[0] == LogLevel.Information
            && i.Arguments[2]?.ToString()?.Contains(fragment, StringComparison.Ordinal) == true);

    [Fact]
    public async Task BuildAsync_KnownArticleNumber_ReturnsMatchcodeFromArticle()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(MakeArticle("X", "KESSEL-7"));
        await ctx.SaveChangesAsync();
        var articleRepo = new ArticleRepository(ctx);
        var logger = new Mock<ILogger>();

        var result = await MatchcodeLookup.BuildAsync(articleRepo, new[] { "X" }, logger.Object, "Test");

        result.GetValueOrDefault("X").Should().Be("KESSEL-7");
        InfosContaining(logger, "Matchcode-Join").Should().Be(0); // kein Fehltreffer -> kein Log.
    }

    [Fact]
    public async Task BuildAsync_MissingArticle_ReturnsNull_AndLogsExactlyOneCount()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(MakeArticle("X", "KESSEL-7"));
        await ctx.SaveChangesAsync();
        var articleRepo = new ArticleRepository(ctx);
        var logger = new Mock<ILogger>();

        // Drei FA-Zeilen-Artikelnummern, nur "X" hat einen Artikelstamm-Treffer.
        var result = await MatchcodeLookup.BuildAsync(
            articleRepo, new[] { "X", "Y", "Z" }, logger.Object, "TestListe");

        result.ContainsKey("Y").Should().BeFalse();
        result.GetValueOrDefault("Y").Should().BeNull();
        result.ContainsKey("Z").Should().BeFalse();

        // Genau EIN Log-Eintrag mit der Zahl 2 (fehlend) von 3 (gesamt) -- nicht je Zeile.
        logger.Invocations.Count(i => i.Method.Name == nameof(ILogger.Log)
            && (LogLevel)i.Arguments[0] == LogLevel.Information).Should().Be(1);
        InfosContaining(logger, "2").Should().Be(1);
        InfosContaining(logger, "TestListe").Should().Be(1);
    }

    [Fact]
    public async Task BuildAsync_ArticleWithEmptyMatchcode_IsPresentInDict_ButValueNull()
    {
        // F7 (Spec): Artikel vorhanden, Matchcode leer -> Value null, aber KEIN Fehltreffer (Key da).
        using var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(MakeArticle("X", null));
        await ctx.SaveChangesAsync();
        var articleRepo = new ArticleRepository(ctx);
        var logger = new Mock<ILogger>();

        var result = await MatchcodeLookup.BuildAsync(articleRepo, new[] { "X" }, logger.Object, "Test");

        result.ContainsKey("X").Should().BeTrue();
        result["X"].Should().BeNull();
        InfosContaining(logger, "Matchcode-Join").Should().Be(0);
    }

    [Fact]
    public async Task BuildAsync_NullOrEmptyArticleNumbers_AreIgnored_NoRepositoryCall()
    {
        var articleRepo = new Mock<IArticleRepository>();
        var logger = new Mock<ILogger>();

        var result = await MatchcodeLookup.BuildAsync(
            articleRepo.Object, new[] { null, "" }.Select(s => s), logger.Object, "Test");

        result.Should().BeEmpty();
        articleRepo.Verify(
            a => a.GetMatchcodesByArticleNumbersAsync(It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task BuildAsync_RepositoryReturnsNull_IsTreatedAsEmpty_NoThrow()
    {
        // Absicherung gegen einen unkonfigurierten Mock (Moq liefert fuer Task<T> ohne Setup
        // einen Task mit default(T) = null) -- MatchcodeLookup darf hier nicht werfen.
        var articleRepo = new Mock<IArticleRepository>();
        articleRepo.Setup(a => a.GetMatchcodesByArticleNumbersAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync((Dictionary<string, string?>)null!);
        var logger = new Mock<ILogger>();

        var result = await MatchcodeLookup.BuildAsync(articleRepo.Object, new[] { "X" }, logger.Object, "Test");

        result.Should().BeEmpty();
        InfosContaining(logger, "Matchcode-Join").Should().Be(1); // "X" zaehlt als Fehltreffer.
    }
}
