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

    // v1.88.0: Zeile eines Kommissionierauftrags statt PickingItem (gepickt, Ist = Menge)
    private static PickingOrderLine AddPickingItem(ApplicationDbContext ctx, int productionOrderId, string bomArticleNumber, decimal quantity,
        int sourceLocationId) => KommissionierTestHelper.AddZeile(ctx, productionOrderId, bomArticleNumber, quantity, sourceLocationId);

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
        var count = await KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);

        count.Should().Be(10);

        var movements = ctx.StockMovements.Where(m => m.MovementType == MovementType.Umbuchung).ToList();
        movements.Should().HaveCount(10);

        var zeilen = ctx.PickingOrderLines.Where(p => p.ProductionOrderId == order.Id).ToList();
        zeilen.Should().OnlyContain(p => p.Status == PickingOrderLineStatus.Umgebucht && p.StockMovementId != null);
        movements.Should().OnlyContain(m => m.ProductionOrderId == order.Id && m.ProductionOrder == "FA-001"); // Teil A: FA-Feld + Text
    }

    // ── Test 2: Article not found — v1.88.0: melden statt still ueberspringen, nichts gebucht ──

    [Fact]
    public async Task TransferPickedItems_ArticleNotFound_MeldetUndBuchtNichts()
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
        var act = () => KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*NONEXISTENT-ART*unbekannt*");
        ctx.StockMovements.Count(m => m.MovementType == MovementType.Umbuchung).Should().Be(0);
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

        var act = () => KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);
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

        var act = () => KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);
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
        var count = await KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);

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
        var count = await KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);

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
        var count = await KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);

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

        var act = () => KommissionierTestHelper.UmbuchenAsync(ctx, service, order.Id, target.Id, User, WinUser);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keine Artikel zum Umbuchen*");
    }

    // ── v1.88.0 (AK 11, 12): Ist-Menge serverseitig, Teilmenge, kein Sage-Queue-Eintrag, Status InArbeit ──

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("11")]
    [InlineData("abc")]
    public async Task IstMenge_UngueltigOderUeberSoll_Abgelehnt_NichtsGebucht(string ist)
    {
        using var ctx = CreateContext(nameof(IstMenge_UngueltigOderUeberSoll_Abgelehnt_NichtsGebucht) + ist);
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");
        AddStock(ctx, art.Id, src.Id, 100);
        var zeile = AddPickingItem(ctx, order.Id, art.ArticleNumber, 10, src.Id);
        var service = CreateService(ctx);

        var act = () => service.TransferOrderLinesAsync(zeile.PickingOrderId, target.Id, false,
            new() { new PickingSelectionItem { PickingItemId = zeile.Id, SourceStorageLocationId = src.Id, IstMenge = ist } }, null, User, WinUser);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Ist-Menge*");
        ctx.StockMovements.Count(m => m.MovementType == MovementType.Umbuchung).Should().Be(0);
    }

    [Fact]
    public async Task Teilmenge_MitKomma_BuchtIst_KeinSageQueue_AuftragInArbeit()
    {
        using var ctx = CreateContext();
        var order = AddOrder(ctx);
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");
        AddStock(ctx, art.Id, src.Id, 100);
        var zeile = AddPickingItem(ctx, order.Id, art.ArticleNumber, 10, src.Id);
        var service = CreateService(ctx);

        await service.TransferOrderLinesAsync(zeile.PickingOrderId, target.Id, false,
            new() { new PickingSelectionItem { PickingItemId = zeile.Id, SourceStorageLocationId = src.Id, IstMenge = "4,5" } }, null, User, WinUser);

        var m = ctx.StockMovements.Single(x => x.MovementType == MovementType.Umbuchung);
        (m.Quantity, m.ProductionOrderId, m.ProductionOrder).Should().Be((4.5m, (int?)order.Id, "FA-001"));
        ctx.PickingOrderLines.Single().Should().Match<PickingOrderLine>(z => z.IstMenge == 4.5m && z.StockMovementId == m.Id && z.ModifiedBy == User);
        ctx.PickingOrders.Single().Status.Should().Be(PickingOrderStatus.InArbeit);
        ctx.SageBookingQueueItems.Should().BeEmpty("Kommissionier-Umbuchungen gehen nie an Sage (direkt ueber den Context)");
    }

    [Fact]
    public async Task ZeileEinesAnderenAuftrags_Abgelehnt()
    {
        using var ctx = CreateContext();
        var o1 = AddOrder(ctx, "FA-1");
        var o2 = AddOrder(ctx, "FA-2");
        var target = AddLocation(ctx, "TARGET");
        var src = AddLocation(ctx, "SRC");
        var art = AddArticle(ctx, "ART-001");
        var z1 = AddPickingItem(ctx, o1.Id, art.ArticleNumber, 1, src.Id);
        var z2 = AddPickingItem(ctx, o2.Id, art.ArticleNumber, 1, src.Id);

        var act = () => CreateService(ctx).TransferOrderLinesAsync(z1.PickingOrderId, target.Id, false,
            new() { new PickingSelectionItem { PickingItemId = z2.Id, SourceStorageLocationId = src.Id } }, null, User, WinUser);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*gehört nicht zu Auftrag*");
    }
}
