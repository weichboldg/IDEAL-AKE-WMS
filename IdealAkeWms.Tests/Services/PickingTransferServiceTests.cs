using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdealAkeWms.Tests.Services;

public class PickingTransferServiceTests
{
    private const string User = "TestUser";
    private const string WinUser = "DOMAIN\\testuser";

    private static ApplicationDbContext CreateContext([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        => TestDbContextFactory.Create(name);

    private static PickingTransferService CreateService(
        ApplicationDbContext context,
        string? negativeBuchungErlaubt = "false",
        string? negativeBuchungLagerplatz = "NAN")
    {
        var settingRepo = new Mock<IAppSettingRepository>();
        settingRepo.Setup(r => r.GetValueAsync("NegativeBuchungErlaubt"))
            .ReturnsAsync(negativeBuchungErlaubt);
        settingRepo.Setup(r => r.GetValueAsync("NegativeBuchungLagerplatz"))
            .ReturnsAsync(negativeBuchungLagerplatz);

        var stockRepo = new Mock<IStockMovementRepository>();
        var logger = new Mock<ILogger<PickingTransferService>>();

        return new PickingTransferService(context, logger.Object, settingRepo.Object, stockRepo.Object);
    }

    private static Article AddArticle(ApplicationDbContext ctx, string articleNumber, string description = "Test")
    {
        var article = new Article
        {
            ArticleNumber = articleNumber,
            Description = description,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.Articles.Add(article);
        ctx.SaveChanges();
        return article;
    }

    private static StorageLocation AddLocation(ApplicationDbContext ctx, string code, bool isPickingTransport = false)
    {
        var loc = new StorageLocation
        {
            Code = code,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser,
            IsPickingTransport = isPickingTransport
        };
        ctx.StorageLocations.Add(loc);
        ctx.SaveChanges();
        return loc;
    }

    private static ProductionOrder AddOrder(ApplicationDbContext ctx, string orderNumber = "FA-001")
    {
        var order = new ProductionOrder
        {
            OrderNumber = orderNumber,
            Quantity = 1,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();
        return order;
    }

    private static PickingItem AddPickingItem(
        ApplicationDbContext ctx,
        int productionOrderId,
        string bomArticleNumber,
        decimal quantity,
        int sourceLocationId,
        bool isPicked = true,
        bool isTransferred = false)
    {
        var item = new PickingItem
        {
            ProductionOrderId = productionOrderId,
            BomArticleNumber = bomArticleNumber,
            Quantity = quantity,
            SourceStorageLocationId = sourceLocationId,
            IsPicked = isPicked,
            IsTransferred = isTransferred,
            RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks),
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        };
        ctx.PickingItems.Add(item);
        ctx.SaveChanges();
        return item;
    }

    private static void AddStock(ApplicationDbContext ctx, int articleId, int locationId, decimal quantity)
    {
        ctx.StockMovements.Add(new StockMovement
        {
            ArticleId = articleId,
            StorageLocationId = locationId,
            Quantity = quantity,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.Now,
            WindowsUser = WinUser,
            CreatedAt = DateTime.Now,
            CreatedBy = User,
            CreatedByWindows = WinUser
        });
        ctx.SaveChanges();
    }

    // ── Test 1: Happy path — 10 items all transferred ──

    [Fact]
    public async Task TransferPickedItems_MultipleItems_AllTransferred()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");

        var articles = new List<Article>();
        var sources = new List<StorageLocation>();

        for (int i = 0; i < 10; i++)
        {
            var art = AddArticle(ctx, $"ART-{i:D3}");
            var src = AddLocation(ctx, $"SRC-{i:D2}");
            AddStock(ctx, art.Id, src.Id, 100);
            AddPickingItem(ctx, order.Id, art.ArticleNumber, 5, src.Id);
            articles.Add(art);
            sources.Add(src);
        }

        var service = CreateService(ctx);
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);

        count.Should().Be(10);

        var movements = ctx.StockMovements.Where(m => m.MovementType == MovementType.Umbuchung).ToList();
        movements.Should().HaveCount(10);

        var pickingItems = ctx.PickingItems.Where(p => p.ProductionOrderId == order.Id).ToList();
        pickingItems.Should().OnlyContain(p => p.IsTransferred);
    }

    // ── Test 2: Article not found — item is skipped ──

    [Fact]
    public async Task TransferPickedItems_ArticleNotFound_SkipsItem()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");

        // Add picking item with article number that doesn't exist in Articles table
        AddPickingItem(ctx, order.Id, "NONEXISTENT-ART", 5, src.Id);

        // Also add a valid item so the method doesn't throw "no picked items"
        var validArt = AddArticle(ctx, "VALID-ART");
        AddStock(ctx, validArt.Id, src.Id, 100);
        AddPickingItem(ctx, order.Id, validArt.ArticleNumber, 3, src.Id);

        var service = CreateService(ctx);
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);

        count.Should().Be(1); // Only the valid one transferred
    }

    // ── Test 3: Insufficient stock — throws when negative not allowed ──

    [Fact]
    public async Task TransferPickedItems_InsufficientStock_ThrowsWhenNegativeNotAllowed()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");

        AddStock(ctx, art.Id, src.Id, 5); // Only 5 in stock
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 10, src.Id); // Need 10

        var service = CreateService(ctx, negativeBuchungErlaubt: "false");

        var act = () => service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nicht genügend Bestand*");
    }

    // ── Test 4: CRITICAL — Duplicate article+location, total exceeds stock, must throw ──

    [Fact]
    public async Task TransferPickedItems_DuplicateArticleSameSource_DecrementsStockCorrectly()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");

        AddStock(ctx, art.Id, src.Id, 15); // 15 in stock
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src.Id); // First: 8
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src.Id); // Second: 8 → total 16 > 15

        var service = CreateService(ctx, negativeBuchungErlaubt: "false");

        var act = () => service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nicht genügend Bestand*");
    }

    // ── Test 5: Duplicate article+location, total fits in stock ──

    [Fact]
    public async Task TransferPickedItems_DuplicateArticleSameSource_BothFitInStock()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");

        AddStock(ctx, art.Id, src.Id, 20); // 20 in stock
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src.Id); // First: 8
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src.Id); // Second: 8 → total 16 ≤ 20

        var service = CreateService(ctx);
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);

        count.Should().Be(2);

        var movements = ctx.StockMovements.Where(m => m.MovementType == MovementType.Umbuchung).ToList();
        movements.Should().HaveCount(2);
        movements.Should().OnlyContain(m => m.Quantity == 8);
    }

    // ── Test 6: Same article, different source locations — stock calculated separately ──

    [Fact]
    public async Task TransferPickedItems_SameArticleDifferentSources_CalculatesStockSeparately()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src1 = AddLocation(ctx, "SRC-1");
        var src2 = AddLocation(ctx, "SRC-2");
        var art = AddArticle(ctx, "ART-001");

        AddStock(ctx, art.Id, src1.Id, 10);
        AddStock(ctx, art.Id, src2.Id, 10);
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src1.Id); // 8 from SRC-1 (10 avail)
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 8, src2.Id); // 8 from SRC-2 (10 avail)

        var service = CreateService(ctx);
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);

        count.Should().Be(2);
    }

    // ── Test 7: Negative booking allowed — uses NAN location ──

    [Fact]
    public async Task TransferPickedItems_NegativeAllowed_UsesNanLocation()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var nanLoc = AddLocation(ctx, "NAN");
        var art = AddArticle(ctx, "ART-001");

        AddStock(ctx, art.Id, src.Id, 5); // Only 5
        AddPickingItem(ctx, order.Id, art.ArticleNumber, 10, src.Id); // Need 10

        var service = CreateService(ctx, negativeBuchungErlaubt: "true", negativeBuchungLagerplatz: "NAN");
        var count = await service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);

        count.Should().Be(1);

        var movement = ctx.StockMovements.First(m => m.MovementType == MovementType.Umbuchung);
        movement.SourceStorageLocationId.Should().Be(nanLoc.Id); // Source changed to NAN
        movement.StorageLocationId.Should().Be(target.Id);
    }

    // ── Test 8: No picked items — throws ──

    [Fact]
    public async Task TransferPickedItems_NoPickedItems_Throws()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");

        var service = CreateService(ctx);

        var act = () => service.TransferPickedItemsAsync(order.Id, target.Id, 1, User, WinUser);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keine gepickten Artikel*");
    }
}
