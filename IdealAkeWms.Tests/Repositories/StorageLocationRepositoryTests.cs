using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;

namespace IdealAkeWms.Tests.Repositories;

public class StorageLocationRepositoryTests
{
    [Fact]
    public async Task GetActiveOrderedExcludingPickingTransport_FiltersInactiveAndPickingTransport()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("A-01", isActive: true,  isPickingTransport: false),
            New("A-02", isActive: false, isPickingTransport: false),
            New("WAGEN-1", isActive: true, isPickingTransport: true)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActiveOrderedExcludingPickingTransportAsync();

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("A-01");
    }

    [Fact]
    public async Task GetActivePickingTransport_FiltersInactive()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("WAGEN-1", isActive: true,  isPickingTransport: true),
            New("WAGEN-2", isActive: false, isPickingTransport: true)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActivePickingTransportLocationsAsync();

        result.Should().ContainSingle().Which.Code.Should().Be("WAGEN-1");
    }

    [Fact]
    public async Task GetActiveOrderedExcludingPickingTransport_FiltersNonBookable()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("AKTIV-BUCH",     isActive: true,  isPickingTransport: false, istBuchbar: true),
            New("AKTIV-NICHT",    isActive: true,  isPickingTransport: false, istBuchbar: false),
            New("INAKTIV-BUCH",   isActive: false, isPickingTransport: false, istBuchbar: true)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActiveOrderedExcludingPickingTransportAsync();

        result.Should().ContainSingle();
        result[0].Code.Should().Be("AKTIV-BUCH");
    }

    [Fact]
    public async Task GetActivePickingTransportLocations_FiltersNonBookable()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.StorageLocations.AddRange(
            New("WAGEN-BUCH",   isActive: true, isPickingTransport: true,  istBuchbar: true),
            New("WAGEN-NICHT",  isActive: true, isPickingTransport: true,  istBuchbar: false)
        );
        await ctx.SaveChangesAsync();
        var repo = new StorageLocationRepository(ctx);

        var result = await repo.GetActivePickingTransportLocationsAsync();

        result.Should().ContainSingle().Which.Code.Should().Be("WAGEN-BUCH");
    }

    private static StorageLocation New(string code, bool isActive, bool isPickingTransport, bool istBuchbar = true) => new()
    {
        Code = code,
        BarcodeValue = code,
        IsActive = isActive,
        IsPickingTransport = isPickingTransport,
        IstBuchbar = istBuchbar,
        Source = StorageLocationSource.Manual,
        CreatedBy = "tester",
        CreatedByWindows = "tester"
    };
}
