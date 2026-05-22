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
}
