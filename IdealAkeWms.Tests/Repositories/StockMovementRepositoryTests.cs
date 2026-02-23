using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

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
}
