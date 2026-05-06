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

    private static StorageLocation New(string code, bool isActive, bool isPickingTransport) => new()
    {
        Code = code,
        BarcodeValue = code,
        IsActive = isActive,
        IsPickingTransport = isPickingTransport,
        Source = StorageLocationSource.Manual,
        CreatedBy = "tester",
        CreatedByWindows = "tester"
    };
}
