using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>
/// Lifecycle failure-path tests for BomCacheSyncService.
/// For the ComputeHash static-method tests see BomCacheSyncServiceHashTests.cs.
/// </summary>
public class BomCacheSyncServiceLifecycleTests
{
    [Fact]
    public async Task SyncBomCacheAsync_writes_lifecycle_via_failure_path()
    {
        // BomCacheSyncService uses raw ADO.NET (ServiceSettings + BOM reads via DefaultConnection).
        // Missing connection string causes InvalidOperationException AFTER
        // BeginRunAsync — verifies run was started and finished-failed.
        var config = new ConfigurationBuilder().Build(); // no connection strings
        var fakeLogger = new FakeSyncLogger();
        var service = new BomCacheSyncService(config, NullLogger<BomCacheSyncService>.Instance, fakeLogger);

        var act = async () => await service.SyncBomCacheAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("BomCache");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }
}
