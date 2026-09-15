using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Tests.Repositories;

public class StockMovementRepositoryTests
{
    private static (Article article, StorageLocation loc1, StorageLocation loc2) SeedBaseData(Data.ApplicationDbContext ctx)
    {
        var article = new Article
        {
            ArticleNumber = "ART-001",
            Description = "Test Article",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.Articles.Add(article);

        var loc1 = new StorageLocation
        {
            Code = "L01",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        var loc2 = new StorageLocation
        {
            Code = "L02",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.StorageLocations.AddRange(loc1, loc2);
        ctx.SaveChanges();

        return (article, loc1, loc2);
    }

    private static void SetPrimaryLocation(Data.ApplicationDbContext ctx, Article article, StorageLocation loc)
    {
        article.PrimaryStorageLocationId = loc.Id;
        ctx.SaveChanges();
    }

    private static StockMovement CreateMovement(Article article, StorageLocation location,
        decimal qty, MovementType type, int? sourceLocId = null)
    {
        return new StockMovement
        {
            ArticleId = article.Id,
            StorageLocationId = location.Id,
            SourceStorageLocationId = sourceLocId,
            Quantity = qty,
            MovementType = type,
            Timestamp = DateTime.Now,
            WindowsUser = "TEST\\user",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task GetCurrentStock_Einbuchung_ReturnsPositiveStock()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));

        var stock = await repo.GetCurrentStockAsync();

        stock.Should().ContainSingle()
            .Which.CurrentQuantity.Should().Be(10);
    }

    [Fact]
    public async Task GetCurrentStock_EinbuchungAndAusbuchung_CalculatesCorrectly()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(article, loc1, 3, MovementType.Ausbuchung));

        var stock = await repo.GetCurrentStockAsync();

        stock.Should().ContainSingle()
            .Which.CurrentQuantity.Should().Be(7);
    }

    [Fact]
    public async Task GetCurrentStock_Umbuchung_MovesStockBetweenLocations()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, loc2) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        // Einbuchung 10 auf L01
        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));

        // Umbuchung 4 von L01 nach L02
        var transfer = CreateMovement(article, loc2, 4, MovementType.Umbuchung, loc1.Id);
        await repo.AddAsync(transfer);

        var stock = await repo.GetCurrentStockAsync();

        var stockL01 = stock.FirstOrDefault(s => s.StorageLocationCode == "L01");
        var stockL02 = stock.FirstOrDefault(s => s.StorageLocationCode == "L02");

        stockL01.Should().NotBeNull();
        stockL01!.CurrentQuantity.Should().Be(6); // 10 - 4
        stockL02.Should().NotBeNull();
        stockL02!.CurrentQuantity.Should().Be(4);
    }

    [Fact]
    public async Task GetCurrentStock_EmptyDb_ReturnsEmptyList()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new StockMovementRepository(ctx);

        var stock = await repo.GetCurrentStockAsync();

        stock.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockByArticleNumbers_ReturnsCorrectGrouping()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));

        var result = await repo.GetStockByArticleNumbersAsync(new List<string> { "ART-001" });

        result.Should().ContainKey("ART-001");
        result["ART-001"].Should().ContainSingle()
            .Which.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task GetStockByArticleNumbers_EmptyList_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new StockMovementRepository(ctx);

        var result = await repo.GetStockByArticleNumbersAsync(new List<string>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovementHistory_FiltersDateRange()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        var m1 = CreateMovement(article, loc1, 5, MovementType.Einbuchung);
        m1.Timestamp = new DateTime(2026, 1, 1);
        await repo.AddAsync(m1);

        var m2 = CreateMovement(article, loc1, 3, MovementType.Einbuchung);
        m2.Timestamp = new DateTime(2026, 2, 15);
        await repo.AddAsync(m2);

        var (history, totalCount) = await repo.GetMovementHistoryAsync(
            dateFrom: new DateTime(2026, 2, 1),
            dateTo: new DateTime(2026, 2, 28));

        totalCount.Should().Be(1);
        history.Should().ContainSingle();
        history[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task GetCurrentStock_PrimaryLocation_SortedFirstPerArticle()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, loc2) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        // L01 kommt alphabetisch vor L02; wir machen L02 zum Hauptlagerplatz.
        await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
        SetPrimaryLocation(ctx, article, loc2);

        var stock = await repo.GetCurrentStockAsync();

        stock.Should().HaveCount(2);
        stock[0].StorageLocationCode.Should().Be("L02");
        stock[0].IsPrimaryStorageLocation.Should().BeTrue();
        stock[1].StorageLocationCode.Should().Be("L01");
        stock[1].IsPrimaryStorageLocation.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentStock_NoPrimaryLocation_SortedByCode()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, loc2) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));

        var stock = await repo.GetCurrentStockAsync();

        stock.Should().HaveCount(2);
        stock[0].StorageLocationCode.Should().Be("L01");
        stock[1].StorageLocationCode.Should().Be("L02");
        stock.All(s => !s.IsPrimaryStorageLocation).Should().BeTrue();
    }

    [Fact]
    public async Task GetStockByArticleNumbers_PrimaryLocation_SortedFirstWithFlag()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, loc2) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
        SetPrimaryLocation(ctx, article, loc2);

        var result = await repo.GetStockByArticleNumbersAsync(new List<string> { "ART-001" });

        result.Should().ContainKey("ART-001");
        var locs = result["ART-001"];
        locs.Should().HaveCount(2);
        locs[0].Code.Should().Be("L02");
        locs[0].IsPrimaryStorageLocation.Should().BeTrue();
        locs[1].Code.Should().Be("L01");
        locs[1].IsPrimaryStorageLocation.Should().BeFalse();
    }

    // ===== Matchcode (Artikelstamm, Task 6): sm.Article ist per Include schon geladen, daher
    // triviales zusaetzliches OR-Glied — InMemory-lauffaehig wie die bestehenden Freitextfilter. =====

    [Fact]
    public async Task GetCurrentStock_FilterArticle_MatchesByMatchcode()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        article.Matchcode = "MC-SONDERTEIL";
        ctx.SaveChanges();
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));

        var stock = await repo.GetCurrentStockAsync(filterArticle: "SONDERTEIL");

        stock.Should().ContainSingle().Which.ArticleNumber.Should().Be("ART-001");
    }

    [Fact]
    public async Task GetCurrentStock_FilterArticle_NullMatchcode_NoMatch_NoException()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        // article.Matchcode bleibt null (Default)
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));

        var act = async () => await repo.GetCurrentStockAsync(filterArticle: "SONDERTEIL");

        var stock = await act.Should().NotThrowAsync();
        stock.Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentStock_FilterArticle_NonMatchingTerm_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        article.Matchcode = "MC-SONDERTEIL";
        ctx.SaveChanges();
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 10, MovementType.Einbuchung));

        var stock = await repo.GetCurrentStockAsync(filterArticle: "KEIN-TREFFER");

        stock.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovementHistory_FilterArticle_MatchesByMatchcode()
    {
        using var ctx = TestDbContextFactory.Create();
        var (article, loc1, _) = SeedBaseData(ctx);
        article.Matchcode = "MC-SONDERTEIL";
        ctx.SaveChanges();
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));

        var (history, totalCount) = await repo.GetMovementHistoryAsync(filterArticle: "SONDERTEIL");

        totalCount.Should().Be(1);
        history.Should().ContainSingle().Which.ArticleNumber.Should().Be("ART-001");
    }

    // Spaltenfilter "article" nutzt EF.Functions.Like innerhalb patterns.Any(...) -- unter InMemory
    // nicht ausfuehrbar (Fallstrick Task 5). Beleg daher ueber ToQueryString() gegen einen nie
    // verbundenen SqlServer-Context, gleiches Muster wie ProductionOrderRepositoryTests.
    [Fact]
    public void ArticleColumnFilter_SqlServer_TranslatesMatchcodeOrClause()
    {
        using var ctx = CreateSqlServerContext();
        var tokens = new List<string> { "sonderteil" };

        var sql = StockMovementRepository
            .ApplyMovementColumnFilter(ctx.StockMovements, "article", tokens, negate: false)
            .ToQueryString();

        sql.Should().Contain("Matchcode", "der Spaltenfilter 'article' muss auch gegen Article.Matchcode uebersetzt werden");
    }

    [Fact]
    public void ArticleColumnFilter_Negated_SqlServer_PreservesMatchcodeNullGuardUnderNot()
    {
        using var ctx = CreateSqlServerContext();
        var tokens = new List<string> { "sonderteil" };

        var sql = StockMovementRepository
            .ApplyMovementColumnFilter(ctx.StockMovements, "article", tokens, negate: true)
            .ToQueryString();

        sql.Should().Contain("Matchcode");
        sql.Should().Contain("NOT", "der Negativfall muss als NOT ueber die OR-Verknuepfung uebersetzt werden");
    }

    /// <summary>
    /// Context gegen den SqlServer-Provider mit einer nicht aufloesbaren Server-Adresse — es wird
    /// NIE eine Verbindung geoeffnet, ToQueryString() kompiliert nur den Ausdrucksbaum zu SQL-Text.
    /// Gleiches Muster wie ProductionOrderRepositoryTests.CreateSqlServerContext.
    /// </summary>
    private static Data.ApplicationDbContext CreateSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<Data.ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1")
            .Options;
        return new Data.ApplicationDbContext(options);
    }
}
