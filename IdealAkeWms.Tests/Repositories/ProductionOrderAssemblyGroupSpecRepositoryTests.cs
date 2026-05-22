using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// Tests fuer das neue <see cref="ProductionOrderAssemblyGroupSpecRepository"/> (Phase 4 / v1.13.0).
/// Spec 12.2.
/// </summary>
public class ProductionOrderAssemblyGroupSpecRepositoryTests
{
    private static ProductionOrderAssemblyGroup SeedGroup(ApplicationDbContext ctx, string orderNumber, string groupKey = "VK")
    {
        var created = TestDataHelper.CreateOrderWithStatuses(ctx, orderNumber);
        return created.Groups.First(g => g.GroupKey == groupKey);
    }

    private static ProductionOrderAssemblyGroupSpec MakeSpec(int groupId, string description, int sortOrder = 0)
        => new()
        {
            AssemblyGroupId = groupId,
            Description = description,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            CreatedByWindows = "test"
        };

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsEntity()
    {
        using var ctx = TestDbContextFactory.Create();
        var grp = SeedGroup(ctx, "FA-SPEC-1");
        var spec = MakeSpec(grp.Id, "Lueftermotor", 10);
        ctx.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var loaded = await repo.GetByIdAsync(spec.Id);

        loaded.Should().NotBeNull();
        loaded!.Description.Should().Be("Lueftermotor");
        loaded.AssemblyGroupId.Should().Be(grp.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        using var ctx = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var loaded = await repo.GetByIdAsync(999_999);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByAssemblyGroupIdAsync_ReturnsOrderedBySortOrderThenId()
    {
        using var ctx = TestDbContextFactory.Create();
        var grp = SeedGroup(ctx, "FA-SPEC-SORT");

        ctx.ProductionOrderAssemblyGroupSpecs.AddRange(
            MakeSpec(grp.Id, "B", sortOrder: 20),
            MakeSpec(grp.Id, "A", sortOrder: 10),
            MakeSpec(grp.Id, "C", sortOrder: 10));
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var rows = await repo.GetByAssemblyGroupIdAsync(grp.Id);

        rows.Select(s => s.Description).Should().Equal("A", "C", "B");
    }

    [Fact]
    public async Task GetByAssemblyGroupIdsAsync_MultipleGroups_ReturnsGrouping()
    {
        using var ctx = TestDbContextFactory.Create();
        var grpA = SeedGroup(ctx, "FA-BULK-A", "VK");
        var grpB = SeedGroup(ctx, "FA-BULK-B", "VL");

        ctx.ProductionOrderAssemblyGroupSpecs.AddRange(
            MakeSpec(grpA.Id, "A1", sortOrder: 10),
            MakeSpec(grpA.Id, "A2", sortOrder: 20),
            MakeSpec(grpB.Id, "B1", sortOrder: 10));
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var result = await repo.GetByAssemblyGroupIdsAsync(new[] { grpA.Id, grpB.Id });

        result.Should().HaveCount(2);
        result[grpA.Id].Select(s => s.Description).Should().Equal("A1", "A2");
        result[grpB.Id].Select(s => s.Description).Should().Equal("B1");
    }

    [Fact]
    public async Task GetByAssemblyGroupIdsAsync_EmptyInput_ReturnsEmptyDictionary()
    {
        using var ctx = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var result = await repo.GetByAssemblyGroupIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_PersistsEntity_AndReturnsAssignedId()
    {
        using var ctx = TestDbContextFactory.Create();
        var grp = SeedGroup(ctx, "FA-ADD");

        var spec = new ProductionOrderAssemblyGroupSpec
        {
            AssemblyGroupId = grp.Id,
            Description = "Neue Auspraegung",
            Quantity = 2.5m,
            Notes = "Bitte beachten",
            SortOrder = 5,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "alice",
            CreatedByWindows = "DOMAIN\\alice"
        };

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var newId = await repo.AddAsync(spec);

        newId.Should().BeGreaterThan(0);
        var reloaded = await ctx.ProductionOrderAssemblyGroupSpecs.FindAsync(newId);
        reloaded.Should().NotBeNull();
        reloaded!.Description.Should().Be("Neue Auspraegung");
        reloaded.Quantity.Should().Be(2.5m);
        reloaded.Notes.Should().Be("Bitte beachten");
        reloaded.CreatedBy.Should().Be("alice");
        reloaded.CreatedByWindows.Should().Be("DOMAIN\\alice");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var grp = SeedGroup(ctx, "FA-UPDATE");
        var spec = MakeSpec(grp.Id, "Original", sortOrder: 10);
        ctx.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await ctx.SaveChangesAsync();

        spec.Description = "Geaendert";
        spec.Quantity = 7m;
        spec.ModifiedAt = DateTime.UtcNow;
        spec.ModifiedBy = "bob";
        spec.ModifiedByWindows = "DOMAIN\\bob";

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        await repo.UpdateAsync(spec);

        ctx.ChangeTracker.Clear();
        var reloaded = await ctx.ProductionOrderAssemblyGroupSpecs.FindAsync(spec.Id);
        reloaded!.Description.Should().Be("Geaendert");
        reloaded.Quantity.Should().Be(7m);
        reloaded.ModifiedAt.Should().NotBeNull();
        reloaded.ModifiedBy.Should().Be("bob");
        reloaded.ModifiedByWindows.Should().Be("DOMAIN\\bob");
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        using var ctx = TestDbContextFactory.Create();
        var grp = SeedGroup(ctx, "FA-DELETE");
        var spec = MakeSpec(grp.Id, "Zum Loeschen");
        ctx.ProductionOrderAssemblyGroupSpecs.Add(spec);
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        await repo.DeleteAsync(spec.Id);

        var reloaded = await ctx.ProductionOrderAssemblyGroupSpecs.FindAsync(spec.Id);
        reloaded.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_NoThrow()
    {
        using var ctx = TestDbContextFactory.Create();

        var repo = new ProductionOrderAssemblyGroupSpecRepository(ctx);
        var act = async () => await repo.DeleteAsync(999_999);

        await act.Should().NotThrowAsync();
    }
}
