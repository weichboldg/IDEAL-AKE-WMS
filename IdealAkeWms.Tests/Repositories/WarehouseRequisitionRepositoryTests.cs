using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

public class WarehouseRequisitionRepositoryTests
{
    private static async Task<(int userId, int workplaceId, int recipientGroupId)> SeedAsync(ApplicationDbContext ctx)
    {
        var u = new User { Name = "tester", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp = new ProductionWorkplace { Name = "WB-1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var grp = new OrderRecipientGroup { Name = "Lager", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.ProductionWorkplaces.Add(wp); ctx.OrderRecipientGroups.Add(grp);
        await ctx.SaveChangesAsync();
        return (u.Id, wp.Id, grp.Id);
    }

    [Fact]
    public async Task CreateDraftAsync_SetsStatusAndAudit()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);

        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "tester", "DOMAIN\\tester");

        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Draft);
        r.ProductionWorkplaceId.Should().Be(wpId);
        r.CreatedBy.Should().Be("tester");
        r.CreatedByWindows.Should().Be("DOMAIN\\tester");
    }

    [Fact]
    public async Task AddItem_AssignsPositionN_Plus1()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");

        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        await repo.AddItemAsync(id, "ART-2", "Mutter", "Stk", 10m, "t", "t");

        var items = ctx.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        items.Should().HaveCount(2);
        items[0].Position.Should().Be(1);
        items[1].Position.Should().Be(2);
    }

    [Fact]
    public async Task AddItem_DuplicateArticle_Throws()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, _) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");

        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        Func<Task> act = () => repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 3m, "t", "t");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ART-1*bereits*");
    }

    [Fact]
    public async Task SubmitAsync_SetsStatusAndAudit()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");

        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r!.RowVersion);

        var updated = await ctx.WarehouseRequisitions.FindAsync(id);
        updated!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);
        updated.OrderRecipientGroupId.Should().Be(grpId);
        updated.SubmittedAt.Should().NotBeNull();
        updated.SubmittedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task CloseAsync_WritesItemQuantitiesPicked()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        await repo.AddItemAsync(id, "ART-2", "Mutter", "Stk", 10m, "t", "t");
        var rBefore = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", rBefore!.RowVersion);

        var rSubmitted = await ctx.WarehouseRequisitions.FindAsync(id);
        var items = ctx.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var quantities = new Dictionary<int, decimal>
        {
            [items[0].Id] = 4m,
            [items[1].Id] = 10m
        };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool>
        {
            [items[0].Id] = true // ART-1 short (4<5) but marked final -> Closed
        };

        await repo.CloseAsync(id, quantities, notes, flags, userId, "t", "t", rSubmitted!.RowVersion);

        var updated = await ctx.WarehouseRequisitions
            .Include(r => r.Items)
            .FirstAsync(r => r.Id == id);
        updated.Status.Should().Be(WarehouseRequisitionStatus.Closed);
        updated.Items.First(i => i.ArticleNumber == "ART-1").QuantityPicked.Should().Be(4m);
        updated.Items.First(i => i.ArticleNumber == "ART-2").QuantityPicked.Should().Be(10m);
    }

    [Fact]
    public async Task CancelAsync_SubmittedThenCancelled_Tracks()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var r1 = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r1!.RowVersion);
        var r2 = await ctx.WarehouseRequisitions.FindAsync(id);

        await repo.CancelAsync(id, "Falsch erfasst", userId, "t", "t", r2!.RowVersion);

        var updated = await ctx.WarehouseRequisitions.FindAsync(id);
        updated!.Status.Should().Be(WarehouseRequisitionStatus.Cancelled);
        updated.CancellationReason.Should().Be("Falsch erfasst");
        updated.CancelledAt.Should().NotBeNull();
        updated.CancelledByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetForWarehouse_FiltersStatusNotDraft()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);

        var draftId = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        var submittedId = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(submittedId, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var rs = await ctx.WarehouseRequisitions.FindAsync(submittedId);
        await repo.SubmitAsync(submittedId, grpId, userId, "t", "t", rs!.RowVersion);

        var (items, total) = await repo.GetForWarehouseAsync(statusFilter: null, workplaceId: null, page: 1, pageSize: 25);

        items.Select(i => i.Id).Should().NotContain(draftId);
        items.Select(i => i.Id).Should().Contain(submittedId);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingSubmitEmails_FindsSubmittedWithoutEmail()
    {
        var ctx = TestDbContextFactory.Create();
        var (userId, wpId, grpId) = await SeedAsync(ctx);
        var repo = new WarehouseRequisitionRepository(ctx);
        var id = await repo.CreateDraftAsync(wpId, userId, "t", "t");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "t", "t");
        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grpId, userId, "t", "t", r!.RowVersion);

        var pending = await repo.GetPendingSubmitEmailsAsync();

        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be(id);
    }

    private async Task<int> SeedRequisitionAsync(ApplicationDbContext db, params (int requested, decimal? picked, bool finalShortage)[] items)
    {
        var r = new WarehouseRequisition
        {
            ProductionWorkplaceId = 1,
            Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now,
            CreatedBy = "test",
            CreatedByWindows = "test\\test",
            SubmittedAt = DateTime.Now,
        };
        db.WarehouseRequisitions.Add(r);
        await db.SaveChangesAsync();
        int pos = 1;
        foreach (var (req, picked, final) in items)
        {
            db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
            {
                WarehouseRequisitionId = r.Id,
                Position = pos++,
                ArticleNumber = $"ART-{pos}",
                ArticleDescription = $"Article {pos}",
                QuantityRequested = req,
                QuantityPicked = picked,
                IsFinalShortage = final,
                CreatedAt = DateTime.Now,
                CreatedBy = "test",
                CreatedByWindows = "test\\test",
            });
        }
        await db.SaveChangesAsync();
        return r.Id;
    }

    [Fact]
    public async Task CloseAsync_AllItemsFullyDelivered_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m, [items[1].Id] = 5m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool>();

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_AllShortagesMarkedFinal_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 7m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true, [items[1].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_OneShortageNotFinal_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m, [items[1].Id] = 3m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = false, [items[1].Id] = false };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_TreatedAsZero_StatusPartiallyDelivered_WhenNotFinal()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = false };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_AndFinalShortageTrue_StatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_ReClose_AfterRestlieferungComplete_TransitionsToClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 3m },
            new Dictionary<int, string?>(),
            new Dictionary<int, bool> { [items[0].Id] = false },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 10m },
            new Dictionary<int, string?>(),
            new Dictionary<int, bool> { [items[0].Id] = false },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_IsFinalShortageTrueButFullyDelivered_FlagIgnoredStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m };
        var notes = new Dictionary<int, string?>();
        var flags = new Dictionary<int, bool> { [items[0].Id] = true };

        await repo.CloseAsync(id, qty, notes, flags, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task SaveProgressAsync_PersistsQuantitiesNotesAndFlags_WithoutStatusChange()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 4m },
            new Dictionary<int, string?> { [items[0].Id] = "  hello  " },
            new Dictionary<int, bool> { [items[0].Id] = true },
            "u", "w");

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.QuantityPicked.Should().Be(4m);
        item.Note.Should().Be("hello");
        item.IsFinalShortage.Should().BeTrue();
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // unveraendert
    }

    [Fact]
    public async Task SaveProgressAsync_DoesNotPromoteSubmittedToPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, false));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 3m },
            new Dictionary<int, string?>(),
            new Dictionary<int, bool> { [items[0].Id] = false },
            "u", "w");

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // KEIN PartiallyDelivered
    }
}
