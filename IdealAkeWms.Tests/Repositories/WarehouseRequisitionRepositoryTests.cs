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
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.NoRestock // ART-1 short (4<5) but marked NoRestock -> Closed
        };

        await repo.CloseAsync(id, quantities, notes, new Dictionary<int, string?>(), statuses, userId, "t", "t", rSubmitted!.RowVersion);

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

        var statuses = new[]
        {
            WarehouseRequisitionStatus.Submitted,
            WarehouseRequisitionStatus.PartiallyDelivered,
            WarehouseRequisitionStatus.Closed,
            WarehouseRequisitionStatus.Cancelled
        };
        var (items, total) = await repo.GetForWarehouseAsync(statuses, workplaceId: null, page: 1, pageSize: 25);

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

    private async Task<int> SeedRequisitionAsync(ApplicationDbContext db, params (int requested, decimal? picked, ShortageStatus status)[] items)
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
        foreach (var (req, picked, status) in items)
        {
            db.WarehouseRequisitionItems.Add(new WarehouseRequisitionItem
            {
                WarehouseRequisitionId = r.Id,
                Position = pos++,
                ArticleNumber = $"ART-{pos}",
                ArticleDescription = $"Article {pos}",
                QuantityRequested = req,
                QuantityPicked = picked,
                ShortageStatus = status,
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
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m, [items[1].Id] = 5m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>();

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_AllShortagesNoRestock_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 7m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock };

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_OneShortageNotFinal_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m, [items[1].Id] = 3m };
        var notes = new Dictionary<int, string?>();
        // ART-2 short (3<5) and explicitly marked WillBeRestocked -> PartiallyDelivered
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None, [items[1].Id] = ShortageStatus.WillBeRestocked };

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_TreatedAsZero_StatusPartiallyDelivered_WhenWillBeRestocked()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked };

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_QuantityPickedNull_AndNoRestock_StatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock };

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_ReClose_AfterRestlieferungComplete_TransitionsToClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        // 1st close: short, marked WillBeRestocked -> PartiallyDelivered
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 3m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

        // 2nd close: now fully delivered -> Closed (status doesn't matter once delivered)
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 10m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None },
            1, "test", "test\\test", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_NoRestockButFullyDelivered_StatusIgnoredClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 10m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock };

        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "test", "test\\test", new byte[0]);

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task SaveProgressAsync_PersistsQuantitiesNotesAndStatuses_WithoutStatusChange()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 4m },
            new Dictionary<int, string?> { [items[0].Id] = "  hello  " },
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock },
            "u", "w");

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.QuantityPicked.Should().Be(4m);
        item.Note.Should().Be("hello");
        item.ShortageStatus.Should().Be(ShortageStatus.NoRestock);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // unveraendert
    }

    [Fact]
    public async Task SaveProgressAsync_DoesNotPromoteSubmittedToPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?> { [items[0].Id] = 3m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None },
            "u", "w");

        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);  // KEIN PartiallyDelivered
    }

    [Fact]
    public async Task GetMissingPartsAsync_IncludesClosedAndPartiallyDelivered_WithFinalShortages()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        // Closed-Bestellung mit NoRestock
        var closedId = await SeedRequisitionAsync(db, (10, 5m, ShortageStatus.NoRestock));
        var closedItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == closedId).ToList();
        await repo.CloseAsync(closedId,
            new Dictionary<int, decimal> { [closedItems[0].Id] = 5m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [closedItems[0].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        // PartiallyDelivered-Bestellung mit NoRestock auf einem Item, anderes WillBeRestocked
        var pdId = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.WillBeRestocked));
        var pdItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == pdId).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(pdId,
            new Dictionary<int, decimal> { [pdItems[0].Id] = 0m, [pdItems[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [pdItems[0].Id] = ShortageStatus.NoRestock, [pdItems[1].Id] = ShortageStatus.WillBeRestocked },
            1, "u", "w", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(pdId))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

        var (items, total) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
        items.Should().HaveCount(2);
        items.Select(i => i.RequisitionId).Should().BeEquivalentTo(new[] { closedId, pdId });
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetMissingPartsAsync_AppliesWorkplaceFilter()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "WB1" },
            new ProductionWorkplace { Id = 2, Name = "WB2" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var r1 = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var i1 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r1).ToList();
        await repo.CloseAsync(r1,
            new Dictionary<int, decimal> { [i1[0].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [i1[0].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var r2req = new WarehouseRequisition
        {
            ProductionWorkplaceId = 2, Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w", SubmittedAt = DateTime.Now
        };
        db.WarehouseRequisitions.Add(r2req);
        await db.SaveChangesAsync();
        var r2item = new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = r2req.Id, Position = 1, ArticleNumber = "A2",
            ArticleDescription = "Desc2", QuantityRequested = 5m,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w"
        };
        db.WarehouseRequisitionItems.Add(r2item);
        await db.SaveChangesAsync();
        await repo.CloseAsync(r2req.Id,
            new Dictionary<int, decimal> { [r2item.Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [r2item.Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (only1, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, 1, null, null, null, 1, 100);
        only1.Should().HaveCount(1);
        only1[0].WorkplaceName.Should().Be("WB1");
    }

    [Fact]
    public async Task GetMissingPartsAsync_AppliesColumnFilter_OnArticleNumber_WithOrSyntax()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        items[0].ArticleNumber = "AAA-1";
        items[1].ArticleNumber = "BBB-2";
        await db.SaveChangesAsync();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["ArticleNumber"] = "AAA" };
        var (filtered, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filters, null, null, 1, 100);
        filtered.Should().HaveCount(1);
        filtered[0].ArticleNumber.Should().Be("AAA-1");

        var filtersOr = new Dictionary<string, string> { ["ArticleNumber"] = "AAA,BBB" };
        var (both, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filtersOr, null, null, 1, 100);
        both.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMissingPartsAsync_PaginationLimitsResults()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        for (int i = 0; i < 5; i++)
        {
            var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock));
            var item = db.WarehouseRequisitionItems.Where(x => x.WarehouseRequisitionId == id).Single();
            await repo.CloseAsync(id,
                new Dictionary<int, decimal> { [item.Id] = 0m },
                new Dictionary<int, string?>(),
                new Dictionary<int, string?>(),
                new Dictionary<int, ShortageStatus> { [item.Id] = ShortageStatus.NoRestock },
                1, "u", "w", new byte[0]);
        }

        var (page1, total) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 2);
        page1.Should().HaveCount(2);
        total.Should().Be(5);
    }

    [Fact]
    public async Task GetMissingPartsAsync_OnlyIncludesItemsWithNoRestock_InPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        // items[0]: short (8<10), marked NoRestock => MissingParts row
        // items[1]: short (3<5), marked WillBeRestocked => NOT in MissingParts (still expected to be restocked)
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 8m, [items[1].Id] = 3m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.WillBeRestocked },
            1, "u", "w", new byte[0]);
        (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
    }

    [Fact]
    public async Task GetMissingPartsAsync_ExcludesCancelledRequisitions()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock));
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status = WarehouseRequisitionStatus.Cancelled;
        await db.SaveChangesAsync();

        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
        result.Should().HaveCount(0);
    }

    // ===== v1.19.0 — 4 neue Repository-Tests fuer 3-State-Logik =====

    [Fact]
    public async Task CloseAsync_AllItemsWillBeRestocked_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 3m, [items[1].Id] = 2m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.WillBeRestocked,
            [items[1].Id] = ShortageStatus.WillBeRestocked
        };
        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_AllShortagesNoRestock_SetsStatusClosed_V19()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.NoRestock,
            [items[1].Id] = ShortageStatus.NoRestock
        };
        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task CloseAsync_MixedShortageStatuses_SetsStatusPartiallyDelivered()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None), (5, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 5m, [items[1].Id] = 0m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus>
        {
            [items[0].Id] = ShortageStatus.WillBeRestocked,
            [items[1].Id] = ShortageStatus.NoRestock
        };
        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);
    }

    [Fact]
    public async Task CloseAsync_ShortageStatusNoneWithShortage_SetsStatusClosed()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();
        var qty = new Dictionary<int, decimal> { [items[0].Id] = 3m };
        var notes = new Dictionary<int, string?>();
        var statuses = new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None };
        await repo.CloseAsync(id, qty, notes, new Dictionary<int, string?>(), statuses, 1, "u", "w", new byte[0]);
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Closed);
    }

    [Fact]
    public async Task GetMissingPartsAsync_TabWillBeRestocked_ReturnsOnlyMatchingItems()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (result, total) = await repo.GetMissingPartsAsync(ShortageStatus.WillBeRestocked, null, null, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
        result[0].Status.Should().Be(ShortageStatus.WillBeRestocked);
    }

    [Fact]
    public async Task GetMissingPartsAsync_TabNoRestock_ReturnsOnlyMatchingItems()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.WillBeRestocked, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (result, total) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, null, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[1].Id);
        result[0].Status.Should().Be(ShortageStatus.NoRestock);
    }

    [Fact]
    public async Task GetShortageCountsForUserAsync_ReturnsBothCounts()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = 42, ProductionWorkplaceId = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test\\test"
        });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var r1 = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.WillBeRestocked), (5, 0m, ShortageStatus.WillBeRestocked));
        var i1 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r1).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(r1,
            new Dictionary<int, decimal> { [i1[0].Id] = 0m, [i1[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [i1[0].Id] = ShortageStatus.WillBeRestocked, [i1[1].Id] = ShortageStatus.WillBeRestocked },
            1, "u", "w", new byte[0]);

        var r2 = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock));
        var i2 = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == r2).Single();
        await repo.CloseAsync(r2,
            new Dictionary<int, decimal> { [i2.Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [i2.Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (waitingItems, waitingReqs, noRestockItems, noRestockReqs) = await repo.GetShortageCountsForUserAsync(42);
        waitingItems.Should().Be(2);
        waitingReqs.Should().Be(1);
        noRestockItems.Should().Be(1);
        noRestockReqs.Should().Be(1);
    }

    [Fact]
    public async Task GetShortageCountsForUserAsync_OnlyForUserWorkplaces()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "WB1" },
            new ProductionWorkplace { Id = 2, Name = "WB2" });
        db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
        {
            UserId = 42, ProductionWorkplaceId = 1,
            CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test\\test"
        });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);

        var r2req = new WarehouseRequisition
        {
            ProductionWorkplaceId = 2, Status = WarehouseRequisitionStatus.Submitted,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w", SubmittedAt = DateTime.Now
        };
        db.WarehouseRequisitions.Add(r2req); await db.SaveChangesAsync();
        var r2item = new WarehouseRequisitionItem
        {
            WarehouseRequisitionId = r2req.Id, Position = 1, ArticleNumber = "X",
            ArticleDescription = "Y", QuantityRequested = 5m,
            CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w"
        };
        db.WarehouseRequisitionItems.Add(r2item); await db.SaveChangesAsync();
        await repo.CloseAsync(r2req.Id,
            new Dictionary<int, decimal> { [r2item.Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [r2item.Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var (waitingItems, _, noRestockItems, _) = await repo.GetShortageCountsForUserAsync(42);
        waitingItems.Should().Be(0);
        noRestockItems.Should().Be(0);
    }

    [Fact]
    public async Task CloseAsync_PersistsNoteEinkauf()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 10m },
            new Dictionary<int, string?> { [items[0].Id] = "lager note" },
            new Dictionary<int, string?> { [items[0].Id] = "ek note" },
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.None },
            1, "u", "w", new byte[0]);

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.Note.Should().Be("lager note");
        item.NoteEinkauf.Should().Be("ek note");
    }

    [Fact]
    public async Task SaveProgressAsync_PersistsNoteEinkauf_WithoutStatusChange()
    {
        using var db = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, null, ShortageStatus.None));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).ToList();

        await repo.SaveProgressAsync(id,
            new Dictionary<int, decimal?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, string?> { [items[0].Id] = "  ek wert  " },
            new Dictionary<int, ShortageStatus>(),
            "u", "w");

        var item = await db.WarehouseRequisitionItems.FindAsync(items[0].Id);
        item!.NoteEinkauf.Should().Be("ek wert");
        var r = await db.WarehouseRequisitions.FindAsync(id);
        r!.Status.Should().Be(WarehouseRequisitionStatus.Submitted);
    }

    [Fact]
    public async Task GetMissingPartsAsync_FiltersByNoteLager_Column()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        items[0].Note = "lager-hinweis";
        items[1].Note = "andere notiz";
        await db.SaveChangesAsync();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?>(),
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["NoteLager"] = "lager" };
        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filters, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
    }

    [Fact]
    public async Task GetMissingPartsAsync_FiltersByNoteEinkauf_Column()
    {
        using var db = TestDbContextFactory.Create();
        db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
        await db.SaveChangesAsync();
        var repo = new WarehouseRequisitionRepository(db);
        var id = await SeedRequisitionAsync(db, (10, 0m, ShortageStatus.NoRestock), (5, 0m, ShortageStatus.NoRestock));
        var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
        await repo.CloseAsync(id,
            new Dictionary<int, decimal> { [items[0].Id] = 0m, [items[1].Id] = 0m },
            new Dictionary<int, string?>(),
            new Dictionary<int, string?> { [items[0].Id] = "ek-hinweis", [items[1].Id] = "egal" },
            new Dictionary<int, ShortageStatus> { [items[0].Id] = ShortageStatus.NoRestock, [items[1].Id] = ShortageStatus.NoRestock },
            1, "u", "w", new byte[0]);

        var filters = new Dictionary<string, string> { ["NoteEinkauf"] = "ek-hinweis" };
        var (result, _) = await repo.GetMissingPartsAsync(ShortageStatus.NoRestock, null, filters, null, null, 1, 100);
        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(items[0].Id);
        result[0].NoteEinkauf.Should().Be("ek-hinweis");
    }
}
