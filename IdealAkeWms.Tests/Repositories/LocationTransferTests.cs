using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests für die Repository-Logik die der Lagerplatz-Umbuchung (LocationTransfer) zugrunde liegt.
/// LocationTransfer bucht alle Artikel eines Quell-Lagerplatzes auf einen Ziel-Lagerplatz (MovementType.Umbuchung).
/// </summary>
public class LocationTransferTests
{
    private static (Article art1, Article art2, StorageLocation source, StorageLocation target) SeedBaseData(
        Data.ApplicationDbContext ctx)
    {
        var art1 = new Article
        {
            ArticleNumber = "ART-001",
            Description = "Artikel 1",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        var art2 = new Article
        {
            ArticleNumber = "ART-002",
            Description = "Artikel 2",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.Articles.AddRange(art1, art2);

        var source = new StorageLocation
        {
            Code = "QUELLE",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        var target = new StorageLocation
        {
            Code = "ZIEL",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.StorageLocations.AddRange(source, target);
        ctx.SaveChanges();

        return (art1, art2, source, target);
    }

    private static StockMovement CreateMovement(Article article, StorageLocation dest,
        decimal qty, MovementType type, int? sourceLocId = null)
    {
        return new StockMovement
        {
            ArticleId = article.Id,
            StorageLocationId = dest.Id,
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
    public async Task GetCurrentStock_FilterBySourceLocation_ReturnsOnlyThatLocation()
    {
        // Prüft, dass GetCurrentStockAsync korrekt nach Lagerplatz filtert
        // (wird vom LocationTransfer-Controller zur Bestandsabfrage genutzt)
        using var ctx = TestDbContextFactory.Create();
        var (art1, art2, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(art1, source, 5, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(art2, target, 3, MovementType.Einbuchung));

        var sourceStock = await repo.GetCurrentStockAsync(filterStorageLocationId: source.Id);

        sourceStock.Should().ContainSingle();
        sourceStock[0].StorageLocationCode.Should().Be("QUELLE");
        sourceStock[0].CurrentQuantity.Should().Be(5);
    }

    [Fact]
    public async Task Umbuchung_SingleArticle_EmptiesSource_FillsTarget()
    {
        // Simuliert eine Lagerplatz-Umbuchung für einen einzelnen Artikel:
        // Einbuchung auf Quelle → Umbuchung auf Ziel → Quelle leer, Ziel hat Bestand
        using var ctx = TestDbContextFactory.Create();
        var (art1, _, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(art1, source, 10, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(art1, target, 10, MovementType.Umbuchung, source.Id));

        var allStock = await repo.GetCurrentStockAsync();

        var sourceStock = allStock.FirstOrDefault(s => s.StorageLocationCode == "QUELLE");
        var targetStock = allStock.FirstOrDefault(s => s.StorageLocationCode == "ZIEL");

        // Quelle muss leer sein (0-Bestand wird herausgefiltert)
        sourceStock.Should().BeNull("0-Bestände werden nicht zurückgegeben");
        targetStock.Should().NotBeNull();
        targetStock!.CurrentQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Umbuchung_MultipleArticles_MovesAllFromSourceToTarget()
    {
        // Simuliert Lagerplatz-Umbuchung mit mehreren Artikeln auf der Quelle
        using var ctx = TestDbContextFactory.Create();
        var (art1, art2, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        // Beide Artikel auf Quell-Lagerplatz einbuchen
        await repo.AddAsync(CreateMovement(art1, source, 15, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(art2, source, 8, MovementType.Einbuchung));

        // LocationTransfer: alle Artikel von Quelle nach Ziel umbuchen
        var sourceItems = await repo.GetCurrentStockAsync(filterStorageLocationId: source.Id);
        foreach (var item in sourceItems.Where(s => s.CurrentQuantity > 0))
        {
            await repo.AddAsync(CreateMovement(
                art1.Id == item.ArticleId ? art1 : art2,
                target, item.CurrentQuantity, MovementType.Umbuchung, source.Id));
        }

        var finalSourceStock = await repo.GetCurrentStockAsync(filterStorageLocationId: source.Id);
        var finalTargetStock = await repo.GetCurrentStockAsync(filterStorageLocationId: target.Id);

        finalSourceStock.Should().BeEmpty("Quell-Lagerplatz soll nach Umbuchung leer sein");
        finalTargetStock.Should().HaveCount(2);
        finalTargetStock.Sum(s => s.CurrentQuantity).Should().Be(23); // 15 + 8
    }

    [Fact]
    public async Task Umbuchung_PartialQuantity_KeepsRemainingAtSource()
    {
        // Partielle Umbuchung: nur Teil des Bestands wird umgebucht
        using var ctx = TestDbContextFactory.Create();
        var (art1, _, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(art1, source, 20, MovementType.Einbuchung));
        // Nur 7 umbuchen (nicht der volle Bestand)
        await repo.AddAsync(CreateMovement(art1, target, 7, MovementType.Umbuchung, source.Id));

        var allStock = await repo.GetCurrentStockAsync();
        var sourceStock = allStock.FirstOrDefault(s => s.StorageLocationCode == "QUELLE");
        var targetStock = allStock.FirstOrDefault(s => s.StorageLocationCode == "ZIEL");

        sourceStock!.CurrentQuantity.Should().Be(13); // 20 - 7
        targetStock!.CurrentQuantity.Should().Be(7);
    }

    [Fact]
    public async Task GetCurrentStock_AfterLocationTransfer_SourceNotInResult()
    {
        // Nach vollständiger Umbuchung darf der Quell-Lagerplatz nicht mehr in Ergebnisliste erscheinen
        using var ctx = TestDbContextFactory.Create();
        var (art1, art2, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(art1, source, 5, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(art2, source, 3, MovementType.Einbuchung));

        // Vollständige Umbuchung
        await repo.AddAsync(CreateMovement(art1, target, 5, MovementType.Umbuchung, source.Id));
        await repo.AddAsync(CreateMovement(art2, target, 3, MovementType.Umbuchung, source.Id));

        var allStock = await repo.GetCurrentStockAsync();
        var sourceCodes = allStock.Select(s => s.StorageLocationCode).Distinct().ToList();

        sourceCodes.Should().NotContain("QUELLE");
        sourceCodes.Should().Contain("ZIEL");
    }

    [Fact]
    public async Task GetCurrentStock_FilterByLocation_ExcludesOtherLocations()
    {
        // Stellt sicher, dass filterStorageLocationId nur den gewünschten Lagerplatz zurückgibt
        using var ctx = TestDbContextFactory.Create();
        var (art1, art2, source, target) = SeedBaseData(ctx);
        var repo = new StockMovementRepository(ctx);

        await repo.AddAsync(CreateMovement(art1, source, 10, MovementType.Einbuchung));
        await repo.AddAsync(CreateMovement(art2, target, 5, MovementType.Einbuchung));

        var filtered = await repo.GetCurrentStockAsync(filterStorageLocationId: target.Id);

        filtered.Should().ContainSingle();
        filtered[0].ArticleNumber.Should().Be("ART-002");
        filtered[0].CurrentQuantity.Should().Be(5);
    }
}
