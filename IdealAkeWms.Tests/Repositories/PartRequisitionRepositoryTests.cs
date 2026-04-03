using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class PartRequisitionRepositoryTests
{
    private static async Task<(ApplicationDbContext context, PartRequisitionRepository repo, ProductionOrder order)> CreateRepoWithOrder()
    {
        var context = TestDbContextFactory.Create();
        var order = new ProductionOrder
        {
            OrderNumber = "2607151",
            Quantity = 1,
            ArticleNumber = "S0310395",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();
        var repo = new PartRequisitionRepository(context);
        return (context, repo, order);
    }

    private static PartRequisition MakeRequisition(int productionOrderId, string articleNumber = "ART-001", string status = PartRequisitionStatus.Offen, DateTime? createdAt = null)
    {
        return new PartRequisition
        {
            ProductionOrderId = productionOrderId,
            ArticleNumber = articleNumber,
            Quantity = 1,
            Status = status,
            Priority = PartRequisitionPriority.Normal,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
    }

    [Fact]
    public async Task AddAsync_CreatesRequisition()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var req = MakeRequisition(order.Id);
        await repo.AddAsync(req);

        req.Id.Should().BeGreaterThan(0);

        var loaded = await repo.GetByIdAsync(req.Id);
        loaded.Should().NotBeNull();
        loaded!.ArticleNumber.Should().Be("ART-001");
        loaded.ProductionOrder.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOpenByArticleNumberAsync_ReturnsOnlyOpen()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var openReq = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen);
        var fulfilledReq = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Erfuellt);
        var cancelledReq = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Storniert);
        var differentArticle = MakeRequisition(order.Id, "ART-999", PartRequisitionStatus.Offen);

        await repo.AddRangeAsync(new[] { openReq, fulfilledReq, cancelledReq, differentArticle });

        var result = await repo.GetOpenByArticleNumberAsync("ART-001");

        result.Should().HaveCount(1);
        result[0].Status.Should().Be(PartRequisitionStatus.Offen);
        result[0].ArticleNumber.Should().Be("ART-001");
    }

    [Fact]
    public async Task GetOpenByArticleNumberAsync_OrdersByCreatedAtAsc()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var now = DateTime.UtcNow;
        var oldest = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen, now.AddMinutes(-30));
        var middle = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen, now.AddMinutes(-10));
        var newest = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen, now);

        await repo.AddRangeAsync(new[] { newest, oldest, middle });

        var result = await repo.GetOpenByArticleNumberAsync("ART-001");

        result.Should().HaveCount(3);
        result[0].CreatedAt.Should().BeBefore(result[1].CreatedAt);
        result[1].CreatedAt.Should().BeBefore(result[2].CreatedAt);
    }

    [Fact]
    public async Task FulfillAsync_SetsStatusAndReference()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        // FulfillAsync needs a real StockMovementId — create StorageLocation + Article + StockMovement
        var storageLocation = new StorageLocation
        {
            Code = "LAG-01",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        var article = new Article
        {
            ArticleNumber = "ART-001",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.StorageLocations.Add(storageLocation);
        context.Articles.Add(article);
        await context.SaveChangesAsync();

        var stockMovement = new StockMovement
        {
            ArticleId = article.Id,
            StorageLocationId = storageLocation.Id,
            Quantity = 1,
            MovementType = MovementType.Einbuchung,
            Timestamp = DateTime.UtcNow,
            WindowsUser = "test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.StockMovements.Add(stockMovement);
        await context.SaveChangesAsync();

        var req = MakeRequisition(order.Id);
        await repo.AddAsync(req);

        await repo.FulfillAsync(req.Id, stockMovement.Id, "fulfiller", "DOMAIN\\fulfiller");

        var loaded = await context.PartRequisitions.FindAsync(req.Id);
        loaded!.Status.Should().Be(PartRequisitionStatus.Erfuellt);
        loaded.FulfilledByStockMovementId.Should().Be(stockMovement.Id);
        loaded.FulfilledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelAsync_SetsStatusAndCancelledBy()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var req = MakeRequisition(order.Id);
        await repo.AddAsync(req);

        await repo.CancelAsync(req.Id, "Lagerist", "canceller", "DOMAIN\\canceller");

        var loaded = await context.PartRequisitions.FindAsync(req.Id);
        loaded!.Status.Should().Be(PartRequisitionStatus.Storniert);
        loaded.CancelledBy.Should().Be("Lagerist");
        loaded.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HasOpenRequisitionAsync_ReturnsTrueWhenExists()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var req = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen);
        await repo.AddAsync(req);

        var hasOpen = await repo.HasOpenRequisitionAsync(order.Id, "ART-001");
        hasOpen.Should().BeTrue();

        // Different article — should return false
        var hasOpenOther = await repo.HasOpenRequisitionAsync(order.Id, "ART-999");
        hasOpenOther.Should().BeFalse();

        // After cancellation — should return false
        await repo.CancelAsync(req.Id, "user", "user", "DOMAIN\\user");
        var hasOpenAfterCancel = await repo.HasOpenRequisitionAsync(order.Id, "ART-001");
        hasOpenAfterCancel.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnsentAsync_ReturnsOnlyUnsentOpen()
    {
        var (context, repo, order) = await CreateRepoWithOrder();

        var unsentOpen = MakeRequisition(order.Id, "ART-001", PartRequisitionStatus.Offen);
        unsentOpen.EmailSentAt = null;

        var sentOpen = MakeRequisition(order.Id, "ART-002", PartRequisitionStatus.Offen);
        sentOpen.EmailSentAt = DateTime.UtcNow;

        var unsentFulfilled = MakeRequisition(order.Id, "ART-003", PartRequisitionStatus.Erfuellt);
        unsentFulfilled.EmailSentAt = null;

        await repo.AddRangeAsync(new[] { unsentOpen, sentOpen, unsentFulfilled });

        var result = await repo.GetUnsentAsync();

        result.Should().HaveCount(1);
        result[0].ArticleNumber.Should().Be("ART-001");
        result[0].EmailSentAt.Should().BeNull();
        result[0].Status.Should().Be(PartRequisitionStatus.Offen);
    }
}
