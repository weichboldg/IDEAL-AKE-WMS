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
}
