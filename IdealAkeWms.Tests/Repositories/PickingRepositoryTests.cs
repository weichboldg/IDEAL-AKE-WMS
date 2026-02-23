using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class PickingRepositoryTests
{
    private static ProductionOrder SeedProductionOrder(Data.ApplicationDbContext ctx)
    {
        var order = new ProductionOrder
        {
            OrderNumber = "WA-001",
            Quantity = 1,
            ArticleNumber = "ART-001",
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
        ctx.ProductionOrders.Add(order);
        ctx.SaveChanges();
        return order;
    }

    private static List<BomItem> CreateBomItems()
    {
        return new List<BomItem>
        {
            new() { Artikelnummer = "ART-001", Position = "1", Ressourcenummer = "RES-001", Menge = 2 },
            new() { Artikelnummer = "ART-001", Position = "2", Ressourcenummer = "RES-002", Menge = 5 },
            new() { Artikelnummer = "ART-001", Position = "3", Ressourcenummer = "RES-003", Menge = 1 }
        };
    }

    [Fact]
    public async Task InitializePickingAsync_CreatesItemsFromBom()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");

        var items = await repo.GetByProductionOrderAsync(order.Id);
        items.Should().HaveCount(3);
        items.Should().Contain(i => i.BomArticleNumber == "RES-001" && i.Quantity == 2);
        items.Should().Contain(i => i.BomArticleNumber == "RES-002" && i.Quantity == 5);
    }

    [Fact]
    public async Task InitializePickingAsync_DoesNotDuplicate_WhenCalledTwice()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");
        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");

        var items = await repo.GetByProductionOrderAsync(order.Id);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task TogglePickedAsync_SetsPickedFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");
        var items = await repo.GetByProductionOrderAsync(order.Id);
        var itemId = items[0].Id;

        await repo.TogglePickedAsync(itemId, 1, "Picker", "TEST\\picker");

        var updated = await repo.GetByIdAsync(itemId);
        updated!.IsPicked.Should().BeTrue();
        updated.PickedBy.Should().Be("Picker");
        updated.PickedByWindows.Should().Be("TEST\\picker");
        updated.SourceStorageLocationId.Should().Be(1);
        updated.PickedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TogglePickedAsync_UnpicksWhenCalledAgain()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");
        var items = await repo.GetByProductionOrderAsync(order.Id);
        var itemId = items[0].Id;

        // Pick
        await repo.TogglePickedAsync(itemId, 1, "Picker", "TEST\\picker");
        // Unpick
        await repo.TogglePickedAsync(itemId, null, "Picker", "TEST\\picker");

        var updated = await repo.GetByIdAsync(itemId);
        updated!.IsPicked.Should().BeFalse();
        updated.PickedBy.Should().BeNull();
        updated.SourceStorageLocationId.Should().BeNull();
    }

    [Fact]
    public async Task GetPickedNotTransferredAsync_OnlyReturnsPicked()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");
        var items = await repo.GetByProductionOrderAsync(order.Id);

        // Pick only first item
        await repo.TogglePickedAsync(items[0].Id, 1, "Picker", "TEST\\picker");

        var picked = await repo.GetPickedNotTransferredAsync(order.Id);
        picked.Should().ContainSingle()
            .Which.BomArticleNumber.Should().Be("RES-001");
    }

    [Fact]
    public async Task MarkAsTransferredAsync_SetsFlags()
    {
        using var ctx = TestDbContextFactory.Create();
        var order = SeedProductionOrder(ctx);
        var repo = new PickingRepository(ctx);

        await repo.InitializePickingAsync(order.Id, CreateBomItems(), "Test", "TEST\\user");
        var items = await repo.GetByProductionOrderAsync(order.Id);
        await repo.TogglePickedAsync(items[0].Id, 1, "Picker", "TEST\\picker");

        var transferTime = DateTime.Now;
        await repo.MarkAsTransferredAsync(new List<int> { items[0].Id }, transferTime);

        var updated = await repo.GetByIdAsync(items[0].Id);
        updated!.IsTransferred.Should().BeTrue();
        updated.TransferredAt.Should().Be(transferTime);

        // Should no longer appear in picked-not-transferred
        var picked = await repo.GetPickedNotTransferredAsync(order.Id);
        picked.Should().BeEmpty();
    }
}
