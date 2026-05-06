using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;            // TestDbContextFactory liegt im Web-Test-Projekt
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class LagerplatzSyncServiceTests
{
    private const string SyncUser_For_Tests = "system:sync";

    private static (LagerplatzSyncService service, FakeSageLagerplatzReader reader, IdealAkeWms.Data.ApplicationDbContext ctx, SyncLogRepository syncLogs)
        Build()
    {
        var ctx = TestDbContextFactory.Create();
        var reader = new FakeSageLagerplatzReader();
        var syncLogs = new SyncLogRepository(ctx);
        var service = new LagerplatzSyncService(ctx, reader, syncLogs, NullLogger<LagerplatzSyncService>.Instance);
        return (service, reader, ctx, syncLogs);
    }

    [Fact]
    public async Task Run_EmptyDb_ThreeSagePlaetze_InsertsThree()
    {
        var (svc, reader, ctx, syncLogs) = Build();
        reader.Records = new()
        {
            new("HALLE-1", "A-01-01", "Regal A1"),
            new("HALLE-1", "A-01-02", "Regal A2"),
            new("HALLE-2", "B-01-01", "Lager Aussen"),
        };

        var result = await svc.RunAsync();

        result.Inserted.Should().Be(3);
        result.Updated.Should().Be(0);
        result.Conflicts.Should().Be(0);

        var stored = ctx.StorageLocations.OrderBy(s => s.Code).ToList();
        stored.Should().HaveCount(3);
        stored[0].Code.Should().Be("A-01-01");
        stored[0].Zone.Should().Be("HALLE-1");
        stored[0].Description.Should().Be("Regal A1");
        stored[0].Source.Should().Be(StorageLocationSource.Sage);
        stored[0].IsActive.Should().BeTrue();
        stored[0].BarcodeValue.Should().Be("A-01-01");
        stored[0].Capacity.Should().BeNull();
        stored[0].IsPickingTransport.Should().BeFalse();

        var summary = (await syncLogs.GetRecentAsync("Lagerplatz", null, 10)).FirstOrDefault();
        summary.Should().NotBeNull();
        summary!.Level.Should().Be(SyncLogLevel.Info);
        summary.Message.Should().Contain("3 neu");
    }

    [Fact]
    public async Task Run_ExistingSageRecord_DescriptionDiff_UpdatesAndStampsModified()
    {
        var (svc, reader, ctx, _) = Build();
        ctx.StorageLocations.Add(new StorageLocation
        {
            Code = "A-01-01", Zone = "HALLE-1", Description = "Alte Bezeichnung",
            BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        });
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "A-01-01", "Neue Bezeichnung") };

        var result = await svc.RunAsync();

        result.Updated.Should().Be(1);
        result.Inserted.Should().Be(0);
        var sl = ctx.StorageLocations.Single();
        sl.Description.Should().Be("Neue Bezeichnung");
        sl.ModifiedAt.Should().NotBeNull();
        sl.ModifiedBy.Should().Be(SyncUser_For_Tests);
    }

    [Fact]
    public async Task Run_ExistingSageRecord_NoDiff_DoesNotUpdate()
    {
        var (svc, reader, ctx, _) = Build();
        var original = new StorageLocation
        {
            Code = "A-01-01", Zone = "HALLE-1", Description = "Regal A1",
            BarcodeValue = "A-01-01", Source = StorageLocationSource.Sage, IsActive = true,
            CreatedAt = DateTime.Now.AddDays(-30), CreatedBy = SyncUser_For_Tests, CreatedByWindows = "x"
        };
        ctx.StorageLocations.Add(original);
        await ctx.SaveChangesAsync();
        reader.Records = new() { new("HALLE-1", "A-01-01", "Regal A1") };

        var result = await svc.RunAsync();

        result.Updated.Should().Be(0);
        var sl = ctx.StorageLocations.Single();
        sl.ModifiedAt.Should().BeNull();
    }
}
