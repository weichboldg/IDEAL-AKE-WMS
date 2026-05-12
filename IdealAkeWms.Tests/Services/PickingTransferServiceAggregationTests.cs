using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Aggregations-Audit-Tests fuer PickingTransferService: Stellen sicher, dass
/// die interne Bestandsberechnung in DoTransferAsync die Sage-Bewegungen
/// korrekt verrechnet (SageEinbuchung +, SageAusbuchung -).
/// </summary>
public class PickingTransferServiceAggregationTests
{
    private const string User = "TestUser";
    private const string WinUser = "TEST\\u";

    [Fact]
    public async Task TransferPickedItemsAsync_TreatsSageEinbuchungAsAvailableStock()
    {
        // Arrange: Source-Lagerplatz hat 10 (Einbuchung) + 3 (SageEinbuchung) = 13.
        // PickingItem benoetigt 11. Wenn SageEinbuchung faelschlich als
        // "Ausbuchung" interpretiert wuerde, waere der Bestand 10 - 3 = 7
        // und der Transfer wuerde mit "Nicht genuegend Bestand" fehlschlagen.
        using var ctx = TestDbContextFactory.Create();
        var (article, source, target, order) = SeedScenario(ctx);

        ctx.StockMovements.AddRange(
            NewMovement(article.Id, source.Id, 10m, MovementType.Einbuchung),
            NewMovement(article.Id, source.Id, 3m, MovementType.SageEinbuchung)
        );
        var item = new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = article.ArticleNumber,
            Quantity = 11m,
            SourceStorageLocationId = source.Id,
            IsPicked = true,
            IsTransferred = false,
            RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks),
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.PickingItems.Add(item);
        ctx.SaveChanges();

        var service = CreateService(ctx);

        // Act
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, null, User, WinUser);

        // Assert: Transfer succeeds because SageEinbuchung counts as +
        count.Should().Be(1);
    }

    [Fact]
    public async Task TransferPickedItemsAsync_TreatsSageAusbuchungAsRemovedStock()
    {
        // Arrange: Source-Lagerplatz hat 10 (Einbuchung) - 4 (SageAusbuchung) = 6.
        // PickingItem benoetigt 7. Wenn SageAusbuchung faelschlich als
        // "Plus" interpretiert wuerde, waere der Bestand 14 und der Transfer
        // wuerde succeeden statt zu fehlen.
        using var ctx = TestDbContextFactory.Create();
        var (article, source, target, order) = SeedScenario(ctx);

        ctx.StockMovements.AddRange(
            NewMovement(article.Id, source.Id, 10m, MovementType.Einbuchung),
            NewMovement(article.Id, source.Id, 4m, MovementType.SageAusbuchung)
        );
        var item = new PickingItem
        {
            ProductionOrderId = order.Id,
            BomArticleNumber = article.ArticleNumber,
            Quantity = 7m,
            SourceStorageLocationId = source.Id,
            IsPicked = true,
            IsTransferred = false,
            RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks),
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.PickingItems.Add(item);
        ctx.SaveChanges();

        var service = CreateService(ctx, negativeBuchungErlaubt: "false");

        // Act + Assert: Transfer should fail because effective stock (6) < quantity (7)
        var act = async () => await service.TransferPickedItemsAsync(order.Id, target.Id, null, User, WinUser);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nicht genügend Bestand*");
    }

    private static (Article article, StorageLocation source, StorageLocation target, ProductionOrder order)
        SeedScenario(ApplicationDbContext ctx)
    {
        var article = new Article
        {
            ArticleNumber = "A-1",
            Description = "Test",
            Unit = "Stk",
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.Articles.Add(article);

        var source = new StorageLocation
        {
            Code = "L-SRC",
            BarcodeValue = "L-SRC",
            IsActive = true,
            IsPickingTransport = false,
            Source = StorageLocationSource.Sage,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        var target = new StorageLocation
        {
            Code = "L-TGT",
            BarcodeValue = "L-TGT",
            IsActive = true,
            IsPickingTransport = true,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.StorageLocations.AddRange(source, target);

        var order = new ProductionOrder
        {
            OrderNumber = "FA-100",
            Quantity = 1,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();

        return (article, source, target, order);
    }

    private static StockMovement NewMovement(int articleId, int locationId, decimal qty, MovementType type) => new()
    {
        ArticleId = articleId,
        StorageLocationId = locationId,
        Quantity = qty,
        MovementType = type,
        Timestamp = DateTime.Now,
        WindowsUser = WinUser,
        CreatedAt = DateTime.Now,
        CreatedBy = User,
        CreatedByWindows = WinUser
    };

    private static PickingTransferService CreateService(
        ApplicationDbContext ctx,
        string? negativeBuchungErlaubt = "false",
        string? negativeBuchungLagerplatz = "NAN")
    {
        var settingRepo = new Mock<IAppSettingRepository>();
        settingRepo.Setup(r => r.GetValueAsync("NegativeBuchungErlaubt"))
            .ReturnsAsync(negativeBuchungErlaubt);
        settingRepo.Setup(r => r.GetValueAsync("NegativeBuchungLagerplatz"))
            .ReturnsAsync(negativeBuchungLagerplatz);

        var stockRepo = new Mock<IStockMovementRepository>();
        // Picking transport target -> service calls GetProductionOrdersAtLocationAsync
        stockRepo.Setup(r => r.GetProductionOrdersAtLocationAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<string>());

        return new PickingTransferService(ctx, NullLogger<PickingTransferService>.Instance, settingRepo.Object, stockRepo.Object);
    }
}
