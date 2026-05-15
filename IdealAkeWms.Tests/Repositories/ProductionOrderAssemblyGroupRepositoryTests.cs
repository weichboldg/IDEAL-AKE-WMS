using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das neue <see cref="ProductionOrderAssemblyGroupRepository"/> (Phase 1 / v1.11.0).
/// Spec 11.1.
/// </summary>
public class ProductionOrderAssemblyGroupRepositoryTests
{
    [Fact]
    public async Task GetByPoAndKeyAsync_ReturnsRow()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-1");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var row = await repo.GetByPoAndKeyAsync(created.Order.Id, "VK");

        row.Should().NotBeNull();
        row!.ProductionOrderId.Should().Be(created.Order.Id);
        row.GroupKey.Should().Be("VK");
        row.IsApplicable.Should().BeFalse();
    }

    [Fact]
    public async Task GetByPoAndKeyAsync_UnknownKey_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-2");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var row = await repo.GetByPoAndKeyAsync(created.Order.Id, "XX");

        row.Should().BeNull();
    }

    [Fact]
    public async Task SetIsApplicableAsync_UnknownGroupKey_ThrowsArgumentException()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-3");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var act = async () => await repo.SetIsApplicableAsync(created.Order.Id, "XX", true, "alice", "DOMAIN\\alice");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*XX*");
    }

    [Fact]
    public async Task SetIsApplicableAsync_HappyPath_SetsValueAndAudit()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-4");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        await repo.SetIsApplicableAsync(created.Order.Id, "VK", true, "alice", "DOMAIN\\alice");

        var row = await repo.GetByPoAndKeyAsync(created.Order.Id, "VK");
        row!.IsApplicable.Should().BeTrue();
        row.ModifiedAt.Should().NotBeNull();
        row.ModifiedBy.Should().Be("alice");
        row.ModifiedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task GetIsApplicablePivotAsync_ReturnsDictPerOrderPerKey()
    {
        using var context = TestDbContextFactory.Create();
        var a = TestDataHelper.CreateOrderWithStatuses(context, "WA-A",
            applicableGroups: new Dictionary<string, bool> { { "VK", true }, { "VL", true } });
        var b = TestDataHelper.CreateOrderWithStatuses(context, "WA-B",
            applicableGroups: new Dictionary<string, bool> { { "VE", true } });
        var c = TestDataHelper.CreateOrderWithStatuses(context, "WA-C");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var pivot = await repo.GetIsApplicablePivotAsync(new[] { a.Order.Id, b.Order.Id, c.Order.Id });

        pivot.Should().HaveCount(3);

        pivot[a.Order.Id]["VK"].Should().BeTrue();
        pivot[a.Order.Id]["VL"].Should().BeTrue();
        pivot[a.Order.Id]["VE"].Should().BeFalse();
        pivot[a.Order.Id]["VT"].Should().BeFalse();
        pivot[a.Order.Id]["VA"].Should().BeFalse();

        pivot[b.Order.Id]["VE"].Should().BeTrue();
        pivot[b.Order.Id]["VK"].Should().BeFalse();

        pivot[c.Order.Id].Should().HaveCount(5);
        pivot[c.Order.Id].Values.Should().AllSatisfy(v => v.Should().BeFalse());
    }

    [Fact]
    public async Task GetIsApplicablePivotAsync_EmptyInput_ReturnsEmpty()
    {
        using var context = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var pivot = await repo.GetIsApplicablePivotAsync(Array.Empty<int>());

        pivot.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByProductionOrderIdAsync_Returns5Groups()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-FULL");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var rows = await repo.GetByProductionOrderIdAsync(created.Order.Id);

        rows.Should().HaveCount(5);
        rows.Select(r => r.GroupKey).Should().BeEquivalentTo(new[] { "VK", "VL", "VE", "VT", "VA" });
    }

    // --- Phase 4 / v1.13.0 erweiterte Methoden ---

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsEntity()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-AG-BYID");
        var vlGroup = created.Groups.First(g => g.GroupKey == "VL");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var loaded = await repo.GetByIdAsync(vlGroup.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(vlGroup.Id);
        loaded.GroupKey.Should().Be("VL");
        loaded.ProductionOrderId.Should().Be(created.Order.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var loaded = await repo.GetByIdAsync(999_999);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByProductionOrderIdsAsync_MultipleOrders_ReturnsAllGroups()
    {
        using var context = TestDbContextFactory.Create();
        var a = TestDataHelper.CreateOrderWithStatuses(context, "WA-BULK-A");
        var b = TestDataHelper.CreateOrderWithStatuses(context, "WA-BULK-B");
        var c = TestDataHelper.CreateOrderWithStatuses(context, "WA-BULK-C");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var rows = await repo.GetByProductionOrderIdsAsync(new[] { a.Order.Id, b.Order.Id, c.Order.Id });

        // 3 orders x 5 groups each = 15
        rows.Should().HaveCount(15);
        rows.Select(r => r.ProductionOrderId).Distinct().Should().BeEquivalentTo(
            new[] { a.Order.Id, b.Order.Id, c.Order.Id });
    }

    [Fact]
    public async Task GetByProductionOrderIdsAsync_EmptyInput_ReturnsEmpty()
    {
        using var context = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var rows = await repo.GetByProductionOrderIdsAsync(Array.Empty<int>());

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task SetIsCompletedAsync_True_SetsCompletedAtAndBy_AndModifiedAudit()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-COMPLETE-1");
        var vkGroup = created.Groups.First(g => g.GroupKey == "VK");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        await repo.SetIsCompletedAsync(vkGroup.Id, true, "alice", "alice", "DOMAIN\\alice");

        context.ChangeTracker.Clear();
        var reloaded = await context.ProductionOrderAssemblyGroups.FindAsync(vkGroup.Id);
        reloaded!.IsCompleted.Should().BeTrue();
        reloaded.CompletedAt.Should().NotBeNull();
        reloaded.CompletedBy.Should().Be("alice");
        reloaded.ModifiedAt.Should().NotBeNull();
        reloaded.ModifiedBy.Should().Be("alice");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task SetIsCompletedAsync_False_ClearsCompletedAtAndBy()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-COMPLETE-2");
        var vkGroup = created.Groups.First(g => g.GroupKey == "VK");

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        // First set to true
        await repo.SetIsCompletedAsync(vkGroup.Id, true, "alice", "alice", "DOMAIN\\alice");
        // Then reset
        await repo.SetIsCompletedAsync(vkGroup.Id, false, "bob", "bob", "DOMAIN\\bob");

        context.ChangeTracker.Clear();
        var reloaded = await context.ProductionOrderAssemblyGroups.FindAsync(vkGroup.Id);
        reloaded!.IsCompleted.Should().BeFalse();
        reloaded.CompletedAt.Should().BeNull();
        reloaded.CompletedBy.Should().BeNull();
        reloaded.ModifiedBy.Should().Be("bob");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\bob");
    }

    [Fact]
    public async Task SetIsCompletedAsync_UnknownId_Throws()
    {
        using var context = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupRepository(context);
        var act = async () => await repo.SetIsCompletedAsync(999_999, true, "alice", "alice", "DOMAIN\\alice");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
