using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

public class EnaioDmsSyncServiceTests
{
    [Fact]
    public async Task SyncDocumentsAsync_writes_lifecycle_via_failure_path()
    {
        // EnaioDmsSyncService uses raw ADO.NET with EnaioDmsConnection.
        // Missing connection string causes InvalidOperationException inside the try block
        // (connection string guard is inside try) — catch triggers FinishFailedAsync.
        var config = new ConfigurationBuilder().Build(); // no connection strings
        var fakeLogger = new FakeSyncLogger();
        var service = new EnaioDmsSyncService(config, NullLogger<EnaioDmsSyncService>.Instance, fakeLogger);

        var act = async () => await service.SyncDocumentsAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("EnaioDms");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }
}
