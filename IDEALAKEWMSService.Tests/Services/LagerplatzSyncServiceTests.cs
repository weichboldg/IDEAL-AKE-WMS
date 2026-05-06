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
}
