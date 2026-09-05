using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das neue <see cref="ProductionOrderBdeStatusRepository"/> (Phase 1 / v1.11.0).
/// Spec 11.1.
/// </summary>
public class ProductionOrderBdeStatusRepositoryTests
{
    [Fact]
    public async Task SetIsDoneBdeAsync_PersistsValue_AndAuditFields()
    {
        using var context = TestDbContextFactory.Create();
        var created = TestDataHelper.CreateOrderWithStatuses(context, "WA-BDE");

        var repo = new ProductionOrderBdeStatusRepository(context);
        await repo.SetIsDoneBdeAsync(created.Order.Id, true, "alice", "DOMAIN\\alice");

        var reloaded = await repo.GetByProductionOrderIdAsync(created.Order.Id);
        reloaded.Should().NotBeNull();
        reloaded!.IsDoneBde.Should().BeTrue();
        reloaded.ModifiedAt.Should().NotBeNull();
        reloaded.ModifiedBy.Should().Be("alice");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task SetIsDoneBdeAsync_MissingRow_ThrowsInvalidOperationException()
    {
        using var context = TestDbContextFactory.Create();

        var repo = new ProductionOrderBdeStatusRepository(context);
        var act = async () => await repo.SetIsDoneBdeAsync(999, true, "alice", "DOMAIN\\alice");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // --- Kaskade „Alle Sub-FAs fertigmelden" (IDEAL Teil 7/8, Etappe C) ---

    [Fact]
    public async Task SetIsDoneBdeForOrderNumberAsync_SetsAllSubFas_OfThatHauptFa_Only()
    {
        using var context = TestDbContextFactory.Create();
        // Drei Sub-FAs unter HauptFA H1, eine fremde HauptFA H2.
        var h1a = TestDataHelper.CreateOrderWithStatuses(context, "H1");
        var h1b = TestDataHelper.CreateOrderWithStatuses(context, "H1");
        var h1c = TestDataHelper.CreateOrderWithStatuses(context, "H1");
        var h2 = TestDataHelper.CreateOrderWithStatuses(context, "H2");

        var repo = new ProductionOrderBdeStatusRepository(context);
        var affected = await repo.SetIsDoneBdeForOrderNumberAsync("H1", true, "alice", "DOMAIN\\alice");

        affected.Should().Be(3);
        (await repo.GetByProductionOrderIdAsync(h1a.Order.Id))!.IsDoneBde.Should().BeTrue();
        (await repo.GetByProductionOrderIdAsync(h1b.Order.Id))!.IsDoneBde.Should().BeTrue();
        (await repo.GetByProductionOrderIdAsync(h1c.Order.Id))!.IsDoneBde.Should().BeTrue();
        // Fremde HauptFA bleibt unberuehrt.
        (await repo.GetByProductionOrderIdAsync(h2.Order.Id))!.IsDoneBde.Should().BeFalse();
    }

    [Fact]
    public async Task SetIsDoneBdeForOrderNumberAsync_StampsIdenticalTimestamp_AndAuditFields()
    {
        using var context = TestDbContextFactory.Create();
        var a = TestDataHelper.CreateOrderWithStatuses(context, "H1");
        var b = TestDataHelper.CreateOrderWithStatuses(context, "H1");

        var repo = new ProductionOrderBdeStatusRepository(context);
        await repo.SetIsDoneBdeForOrderNumberAsync("H1", true, "alice", "DOMAIN\\alice");

        var ra = await repo.GetByProductionOrderIdAsync(a.Order.Id);
        var rb = await repo.GetByProductionOrderIdAsync(b.Order.Id);
        // AK D: als EINE Kaskade erkennbar — identischer Zeitstempel + Benutzer auf allen Zeilen.
        ra!.ModifiedAt.Should().Be(rb!.ModifiedAt);
        ra.ModifiedBy.Should().Be("alice");
        rb.ModifiedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task SetIsDoneBdeForOrderNumberAsync_UnknownOrderNumber_ReturnsZero()
    {
        using var context = TestDbContextFactory.Create();
        TestDataHelper.CreateOrderWithStatuses(context, "H1");

        var repo = new ProductionOrderBdeStatusRepository(context);
        var affected = await repo.SetIsDoneBdeForOrderNumberAsync("DOES-NOT-EXIST", true, "alice", "DOMAIN\\alice");

        affected.Should().Be(0);
    }
}
