using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using FluentAssertions;

namespace IdealAkeWms.Tests.Repositories;

public class ProductionWorkplaceRepositoryTests
{
    private static ProductionWorkplace CreateWorkplace(string name, string? hall = null, int? overridePrePickingDays = null)
    {
        return new ProductionWorkplace
        {
            Name = name,
            Hall = hall,
            OverridePrePickingDays = overridePrePickingDays,
            CreatedAt = DateTime.Now,
            CreatedBy = "Test",
            CreatedByWindows = "TEST\\user"
        };
    }

    [Fact]
    public async Task AddAsync_PersistsWorkplace_WithAllFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank A", "Halle 1", 2);
        await repo.AddAsync(workplace);

        var loaded = await repo.GetByIdAsync(workplace.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Werkbank A");
        loaded.Hall.Should().Be("Halle 1");
        loaded.OverridePrePickingDays.Should().Be(2);
    }

    [Fact]
    public async Task AddAsync_PersistsWorkplace_WithOptionalFieldsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Werkbank Minimal");
        await repo.AddAsync(workplace);

        var loaded = await repo.GetByIdAsync(workplace.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Werkbank Minimal");
        loaded.Hall.Should().BeNull();
        loaded.OverridePrePickingDays.Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrderedAsync_ReturnsAlphabeticallySorted()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        await repo.AddAsync(CreateWorkplace("Zuschnitt"));
        await repo.AddAsync(CreateWorkplace("Montage"));
        await repo.AddAsync(CreateWorkplace("Lackierung"));

        var result = await repo.GetAllOrderedAsync();

        result.Should().HaveCount(3);
        result.Select(w => w.Name).Should().ContainInOrder("Lackierung", "Montage", "Zuschnitt");
    }

    [Fact]
    public async Task GetAllOrderedAsync_EmptyDb_ReturnsEmpty()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetAllOrderedAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetByIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Alt", "Halle Alt", 3);
        await repo.AddAsync(workplace);

        workplace.Name = "Neu";
        workplace.Hall = "Halle Neu";
        workplace.OverridePrePickingDays = 5;
        workplace.ModifiedAt = DateTime.Now;
        workplace.ModifiedBy = "Editor";
        workplace.ModifiedByWindows = "TEST\\editor";
        await repo.UpdateAsync(workplace);

        var loaded = await repo.GetByIdAsync(workplace.Id);

        loaded!.Name.Should().Be("Neu");
        loaded.Hall.Should().Be("Halle Neu");
        loaded.OverridePrePickingDays.Should().Be(5);
        loaded.ModifiedBy.Should().Be("Editor");
    }

    [Fact]
    public async Task UpdateAsync_ClearOverridePrePickingDays_SetsNull()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        var workplace = CreateWorkplace("Test", null, 7);
        await repo.AddAsync(workplace);

        workplace.OverridePrePickingDays = null;
        await repo.UpdateAsync(workplace);

        var loaded = await repo.GetByIdAsync(workplace.Id);
        loaded!.OverridePrePickingDays.Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrderedAsync_MultipleWorkplacesWithSameHall_SortsCorrectly()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);

        await repo.AddAsync(CreateWorkplace("Beta", "Halle 2"));
        await repo.AddAsync(CreateWorkplace("Alpha", "Halle 2"));
        await repo.AddAsync(CreateWorkplace("Gamma", "Halle 1"));

        var result = await repo.GetAllOrderedAsync();

        result.Select(w => w.Name).Should().ContainInOrder("Alpha", "Beta", "Gamma");
    }

    private static ProductionWorkplace NewWp(string name, bool bdeAktiv)
        => new()
        {
            Name = name,
            BdeAktiv = bdeAktiv,
            CreatedAt = DateTime.Now,
            CreatedBy = "t",
            CreatedByWindows = "t"
        };

    [Fact]
    public async Task GetBdeActiveAsync_ReturnsOnlyActiveWorkplaces()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(
            NewWp("Active A", bdeAktiv: true),
            NewWp("Inactive B", bdeAktiv: false),
            NewWp("Active C", bdeAktiv: true));
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetBdeActiveAsync();

        result.Should().HaveCount(2);
        result.Select(w => w.Name).Should().BeEquivalentTo(new[] { "Active A", "Active C" });
    }

    [Fact]
    public async Task GetBdeActiveAsync_OrdersByName()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(
            NewWp("Zeta", bdeAktiv: true),
            NewWp("Alpha", bdeAktiv: true));
        await ctx.SaveChangesAsync();

        var repo = new ProductionWorkplaceRepository(ctx);

        var result = await repo.GetBdeActiveAsync();

        result.Select(w => w.Name).Should().ContainInOrder("Alpha", "Zeta");
    }

    [Fact]
    public async Task SetWorkSteps_SyncsJunctionRows()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);
        ctx.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "A1" });
        ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VK", Name = "K" }, new WorkStep { Id = 11, Code = "VL", Name = "L" });
        ctx.ProductionWorkplaceWorkSteps.Add(new ProductionWorkplaceWorkStep { ProductionWorkplaceId = 1, WorkStepId = 10 });
        await ctx.SaveChangesAsync();

        await repo.SetWorkStepsAsync(1, new List<int> { 11 }); // 10 raus, 11 rein

        ctx.ProductionWorkplaceWorkSteps.Should().ContainSingle(x => x.WorkStepId == 11);
    }

    [Fact]
    public async Task GetWorkStepIds_ReturnsOnlyIdsOfWorkplace()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ProductionWorkplaceRepository(ctx);
        ctx.ProductionWorkplaces.AddRange(
            new ProductionWorkplace { Id = 1, Name = "A1" },
            new ProductionWorkplace { Id = 2, Name = "A2" });
        ctx.WorkSteps.AddRange(new WorkStep { Id = 10, Code = "VK", Name = "K" }, new WorkStep { Id = 11, Code = "VL", Name = "L" });
        ctx.ProductionWorkplaceWorkSteps.AddRange(
            new ProductionWorkplaceWorkStep { ProductionWorkplaceId = 1, WorkStepId = 10 },
            new ProductionWorkplaceWorkStep { ProductionWorkplaceId = 2, WorkStepId = 11 });
        await ctx.SaveChangesAsync();

        var result = await repo.GetWorkStepIdsAsync(1);

        result.Should().BeEquivalentTo(new List<int> { 10 });
    }
}
