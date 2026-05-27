using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class OseonSyncServiceTests
{
    private static OseonSyncService Build(FakeSyncLogger fakeLogger)
    {
        var config = new ConfigurationBuilder().Build(); // no connection strings
        return new OseonSyncService(config, NullLogger<OseonSyncService>.Instance, fakeLogger);
    }

    [Fact]
    public async Task SyncProductionOrders_writes_lifecycle_via_failure_path()
    {
        // OseonSyncService uses raw ADO.NET with OseonConnection.
        // Missing connection string causes InvalidOperationException inside the try block
        // (connection string guard is inside try) — catch triggers FinishFailedAsync.
        var fakeLogger = new FakeSyncLogger();
        var service = Build(fakeLogger);

        var act = async () => await service.SyncOseonProductionOrdersAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("OseonTracking");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SyncWorkplaces_writes_lifecycle_via_failure_path()
    {
        // SyncWorkplacesToProductionOrdersAsync uses DefaultConnection.
        // Missing connection string causes InvalidOperationException inside the try block.
        var fakeLogger = new FakeSyncLogger();
        var service = Build(fakeLogger);

        var act = async () => await service.SyncWorkplacesToProductionOrdersAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("OseonWorkplaces");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SyncArticleCategories_writes_lifecycle_via_failure_path()
    {
        // SyncArticleCategoriesToWmsAsync uses OseonConnection.
        // Missing connection string causes InvalidOperationException inside the try block.
        var fakeLogger = new FakeSyncLogger();
        var service = Build(fakeLogger);

        var act = async () => await service.SyncArticleCategoriesToWmsAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("OseonArticleCategories");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }
}
