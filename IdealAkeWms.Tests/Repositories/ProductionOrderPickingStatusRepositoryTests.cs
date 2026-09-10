using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das neue <see cref="ProductionOrderPickingStatusRepository"/> (Phase 1 / v1.11.0).
/// Spec 11.1.
/// </summary>
public class ProductionOrderPickingStatusRepositoryTests
{
    [Fact]
    public async Task SetFieldAsync_HasGlass_PersistsValue_AndAuditFields()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-GLASS");

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetFieldAsync(created.Order.Id, "HasGlass", true, "alice", "DOMAIN\\alice");

        var reloaded = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        reloaded.Should().NotBeNull();
        reloaded!.HasGlass.Should().BeTrue();
        reloaded.ModifiedAt.Should().NotBeNull();
        reloaded.ModifiedBy.Should().Be("alice");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task SetFieldAsync_MissingStatusRow_CreatesRowAndPersists()
    {
        // Root-Cause-Regression: FAs ohne ProductionOrderPickingStatus-Zeile (Alt-Daten /
        // AgentJob hat die Zeile nicht eager angelegt) liessen "Abschliessen/Erledigt"
        // fehlschlagen (SetFieldAsync warf InvalidOperationException). Statt zu werfen,
        // muss die Zeile angelegt werden.
        using var context = TestDbContextFactory.Create();
        var order = new ProductionOrder
        {
            OrderNumber = "WA-NOSTATUS",
            Quantity = 1m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetIsDonePickingAsync(order.Id, true, "alice", "DOMAIN\\alice");

        var reloaded = await repo.GetByProductionOrderIdAsync(order.Id);
        reloaded.Should().NotBeNull();
        reloaded!.IsDonePicking.Should().BeTrue();
        reloaded.CreatedBy.Should().Be("alice");
        reloaded.ModifiedBy.Should().Be("alice");
    }

    [Fact]
    public async Task SetFieldAsync_UnknownField_ThrowsArgumentException()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-UNK");

        var repo = new ProductionOrderPickingStatusRepository(context);
        var act = async () => await repo.SetFieldAsync(created.Order.Id, "NotAField", true, "alice", "DOMAIN\\alice");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*NotAField*");
    }

    [Fact]
    public async Task SetCoatingPartsAsync_FlipsToFalse_ResetsIsCoatingDone()
    {
        // Fallstrick #11: Wenn HasCoatingParts auf false flippt, muss IsCoatingDone zurueckgesetzt werden.
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(
            context, "WA-COAT-RESET",
            hasCoatingParts: true,
            isCoatingDone: true);

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetCoatingPartsAsync(new Dictionary<int, bool> { { created.Order.Id, false } });

        var reloaded = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        reloaded.Should().NotBeNull();
        reloaded!.HasCoatingParts.Should().BeFalse();
        reloaded.IsCoatingDone.Should().BeFalse(); // Cascade-Reset
        reloaded.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetCoatingPartsAsync_FlipsToTrue_DoesNotTouchIsCoatingDone()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(
            context, "WA-COAT-SET",
            hasCoatingParts: false,
            isCoatingDone: false);

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetCoatingPartsAsync(new Dictionary<int, bool> { { created.Order.Id, true } });

        var reloaded = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        reloaded!.HasCoatingParts.Should().BeTrue();
        reloaded.IsCoatingDone.Should().BeFalse(); // unangetastet
    }

    [Fact]
    public async Task GetReleasedForPickingAsync_OrdersByPriorityThenDate()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-NoPrio",
            releaseForPicking: true, pickingPriority: null,
            productionDate: new DateTime(2026, 5, 1));
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Prio3",
            releaseForPicking: true, pickingPriority: 3);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Prio1",
            releaseForPicking: true, pickingPriority: 1);
        // negative cases (filtered)
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Closed",
            releaseForPicking: true, pickingPriority: 2, isDone: true);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-Off",
            releaseForPicking: false);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingAsync();

        result.Should().HaveCount(3);
        result[0].OrderNumber.Should().Be("WA-Prio1");
        result[1].OrderNumber.Should().Be("WA-Prio3");
        result[2].OrderNumber.Should().Be("WA-NoPrio"); // null priority last
    }

    [Fact]
    public async Task GetReleasedForPickingCountAsync_CountsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "WA-A", releaseForPicking: true, isDone: false);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-B", releaseForPicking: true, isDone: true);
        TestDataHelper.CreateOrderWithStatuses(context, "WA-C", releaseForPicking: false, isDone: false);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var count = await repo.GetReleasedForPickingCountAsync();

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetReleasedForPicking_ExcludesKommDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "FA-1",
            releaseForPicking: true, isDonePicking: false);
        TestDataHelper.CreateOrderWithStatuses(context, "FA-2",
            releaseForPicking: true, isDonePicking: true);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingAsync();
        var count = await repo.GetReleasedForPickingCountAsync();

        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("FA-1");
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetReleasedForPicking_ExcludesCancelledOrders()
    {
        // FA-Reconciliation (v1.25.0): eine vor der Loeschung freigegebene, dann stornierte FA
        // (IsReleasedForPicking=true, IsCancelled=1) darf weder in der Worklist erscheinen
        // noch im Nav-Zaehler mitgezaehlt werden.
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "FA-OPEN",
            releaseForPicking: true);
        var cancelled = TestDataHelper.CreateOrderWithStatuses(context, "FA-CANCELLED",
            releaseForPicking: true);
        cancelled.Order.IsCancelled = true;
        await context.SaveChangesAsync();

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingAsync();
        var count = await repo.GetReleasedForPickingCountAsync();

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetReleasedForPickingByPicker_ExcludesCancelledOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "FA-OPEN",
            releaseForPicking: true, assignedPickerId: 7);
        var cancelled = TestDataHelper.CreateOrderWithStatuses(context, "FA-CANCELLED",
            releaseForPicking: true, assignedPickerId: 7);
        cancelled.Order.IsCancelled = true;
        await context.SaveChangesAsync();

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(7);

        result.Should().ContainSingle().Which.OrderNumber.Should().Be("FA-OPEN");
    }

    [Fact]
    public async Task GetReleasedForPickingByPicker_ExcludesKommDoneOrders()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "FA-1",
            releaseForPicking: true, isDonePicking: false, assignedPickerId: 7);
        TestDataHelper.CreateOrderWithStatuses(context, "FA-2",
            releaseForPicking: true, isDonePicking: true, assignedPickerId: 7);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetReleasedForPickingByPickerAsync(7);

        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("FA-1");
    }

    [Fact]
    public async Task SetReleaseAsync_OnRelease_SetsAuditAndReleasedFields()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-REL");

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetReleaseAsync(created.Order.Id, released: true, priority: 7,
            releasedBy: "alice", modifiedBy: "alice", modifiedByWindows: "DOMAIN\\alice");

        var ps = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        ps!.IsReleasedForPicking.Should().BeTrue();
        ps.ReleasedAt.Should().NotBeNull();
        ps.ReleasedBy.Should().Be("alice");
        ps.PickingPriority.Should().Be(7);
        ps.ModifiedBy.Should().Be("alice");
    }

    [Fact]
    public async Task SetReleaseAsync_OnRevoke_ClearsAssignedPickerFields()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(
            context, "WA-REV",
            releaseForPicking: true,
            assignedPickerId: 5,
            assignedPickerName: "Max");

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetReleaseAsync(created.Order.Id, released: false, priority: null,
            releasedBy: null, modifiedBy: "alice", modifiedByWindows: "DOMAIN\\alice");

        var ps = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        ps!.IsReleasedForPicking.Should().BeFalse();
        ps.AssignedPickerId.Should().BeNull();
        ps.AssignedPickerName.Should().BeNull();
    }

    [Fact]
    public async Task GetByProductionOrderIdsAsync_ReturnsBulkLookupDictionary()
    {
        using var context = TestDbContextFactory.Create();
        var a = TestDataHelper.CreateOrderWithStatuses(context, "WA-1");
        var b = TestDataHelper.CreateOrderWithStatuses(context, "WA-2");
        var c = TestDataHelper.CreateOrderWithStatuses(context, "WA-3");

        var repo = new ProductionOrderPickingStatusRepository(context);
        var result = await repo.GetByProductionOrderIdsAsync(new[] { a.Order.Id, b.Order.Id, c.Order.Id });

        result.Should().HaveCount(3);
        result.Should().ContainKey(a.Order.Id);
        result.Should().ContainKey(b.Order.Id);
        result.Should().ContainKey(c.Order.Id);
    }

    // --- Kaskade „Alle Sub-FAs freigeben" (IDEAL FA-Liste-Ausbau, PickingLeitstand) ---

    [Fact]
    public async Task SetReleaseForOrderNumber_ReleasesOnlyUnreleasedRows_OfThatHauptFa()
    {
        using var context = TestDbContextFactory.Create();
        // HauptFA "100": zwei Sub-FAs offen, eine bereits freigegeben; HauptFA "200" als Fremdgruppe.
        var open1 = TestDataHelper.CreateOrderWithStatuses(context, "100");
        var open2 = TestDataHelper.CreateOrderWithStatuses(context, "100");
        var alreadyReleased = TestDataHelper.CreateOrderWithStatuses(context, "100", releaseForPicking: true);
        var foreign = TestDataHelper.CreateOrderWithStatuses(context, "200");

        var repo = new ProductionOrderPickingStatusRepository(context);
        var affected = await repo.SetReleaseForOrderNumberAsync("100", null, null, "Tester",
            "TestUser", "DOMAIN\\testuser");

        affected.Should().Be(2);
        (await repo.GetByProductionOrderIdAsync(open1.Order.Id))!.IsReleasedForPicking.Should().BeTrue();
        (await repo.GetByProductionOrderIdAsync(open2.Order.Id))!.IsReleasedForPicking.Should().BeTrue();
        // Fremdgruppe unberuehrt.
        (await repo.GetByProductionOrderIdAsync(foreign.Order.Id))!.IsReleasedForPicking.Should().BeFalse();
        alreadyReleased.PickingStatus.IsReleasedForPicking.Should().BeTrue();
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_LeavesAlreadyReleasedRowsUntouched_IncludingAudit()
    {
        using var context = TestDbContextFactory.Create();
        var existing = TestDataHelper.CreateOrderWithStatuses(context, "100", releaseForPicking: true);
        existing.PickingStatus.AssignedPickerName = "Bestandspicker";
        existing.PickingStatus.ModifiedAt = new DateTime(2020, 1, 1);
        await context.SaveChangesAsync();
        var before = existing.PickingStatus.ModifiedAt;

        TestDataHelper.CreateOrderWithStatuses(context, "100"); // offen, wird freigegeben

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetReleaseForOrderNumberAsync("100", 7, "Picker A", "Tester", "TestUser", "DOMAIN\\testuser");

        var reloaded = await repo.GetByProductionOrderIdAsync(existing.Order.Id);
        reloaded!.ModifiedAt.Should().Be(before);
        reloaded.AssignedPickerName.Should().Be("Bestandspicker");
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_AssignsPickerToAllNewlyReleasedRows()
    {
        using var context = TestDbContextFactory.Create();
        var a = TestDataHelper.CreateOrderWithStatuses(context, "100");
        var b = TestDataHelper.CreateOrderWithStatuses(context, "100");

        var repo = new ProductionOrderPickingStatusRepository(context);
        var affected = await repo.SetReleaseForOrderNumberAsync("100", 7, "Picker A", "Tester",
            "TestUser", "DOMAIN\\testuser");

        affected.Should().Be(2);
        var rows = await context.ProductionOrderPickingStatuses
            .Where(s => s.ProductionOrderId == a.Order.Id || s.ProductionOrderId == b.Order.Id)
            .ToListAsync();
        rows.Should().OnlyContain(s => s.AssignedPickerId == 7 && s.AssignedPickerName == "Picker A");
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_AssignsPriority_InSubOrderNumberOrder()
    {
        using var context = TestDbContextFactory.Create();
        // Sub-FAs absichtlich in umgekehrter Reihenfolge anlegen: "103" vor "102".
        var sub103 = TestDataHelper.CreateOrderWithStatuses(context, "100");
        sub103.Order.SubOrderNumber = "103";
        var sub102 = TestDataHelper.CreateOrderWithStatuses(context, "100");
        sub102.Order.SubOrderNumber = "102";
        await context.SaveChangesAsync();

        var repo = new ProductionOrderPickingStatusRepository(context);
        await repo.SetReleaseForOrderNumberAsync("100", null, null, "Tester", "TestUser", "DOMAIN\\testuser");

        var byKey = await context.ProductionOrderPickingStatuses
            .Include(s => s.ProductionOrder).ToListAsync();
        var prio102 = byKey.Single(s => s.ProductionOrder!.SubOrderNumber == "102").PickingPriority;
        var prio103 = byKey.Single(s => s.ProductionOrder!.SubOrderNumber == "103").PickingPriority;
        prio102.Should().BeLessThan(prio103!.Value); // SubOrderNumber aufsteigend
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_UnknownOrderNumber_ReturnsZero()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "100");

        var repo = new ProductionOrderPickingStatusRepository(context);
        var affected = await repo.SetReleaseForOrderNumberAsync("999", null, null, "T", "T", "T");

        affected.Should().Be(0);
    }

    [Fact]
    public async Task CountReleasedByOrderNumberAsync_CountsOnlyReleasedRows_OfThatHauptFa()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "100", releaseForPicking: true);
        TestDataHelper.CreateOrderWithStatuses(context, "100", releaseForPicking: true);
        TestDataHelper.CreateOrderWithStatuses(context, "100", releaseForPicking: false);
        TestDataHelper.CreateOrderWithStatuses(context, "200", releaseForPicking: true);

        var repo = new ProductionOrderPickingStatusRepository(context);
        var count = await repo.CountReleasedByOrderNumberAsync("100");

        count.Should().Be(2);
    }
}
