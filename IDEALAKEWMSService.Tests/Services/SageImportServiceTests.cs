using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

public class SageImportServiceTests
{
    private static SageImportService Build(FakeSyncLogger fakeLogger)
    {
        var config = new ConfigurationBuilder().Build(); // no connection strings
        var bomMock = new Mock<IBomCacheSyncService>();
        var coatingMock = new Mock<ICoatingDetectionService>();
        return new SageImportService(
            config,
            NullLogger<SageImportService>.Instance,
            fakeLogger,
            bomMock.Object,
            coatingMock.Object);
    }

    [Fact]
    public async Task SyncProductionOrdersAsync_writes_lifecycle_via_failure_path()
    {
        // SageImportService uses raw ADO.NET with SageConnection.
        // Missing connection string causes InvalidOperationException AFTER
        // BeginRunAsync — verifies run was started and finished-failed.
        var fakeLogger = new FakeSyncLogger();
        var service = Build(fakeLogger);

        var act = async () => await service.SyncProductionOrdersAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("ProductionOrder");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SyncArticlesAsync_writes_lifecycle_via_failure_path()
    {
        // SageImportService uses raw ADO.NET with SageConnection.
        // Missing connection string causes InvalidOperationException AFTER
        // BeginRunAsync — verifies run was started and finished-failed.
        var fakeLogger = new FakeSyncLogger();
        var service = Build(fakeLogger);

        var act = async () => await service.SyncArticlesAsync(dryRun: false, ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("Article");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }

    [Theory]
    [InlineData(null,        null)]
    [InlineData("",          null)]
    [InlineData("   ",       null)]
    [InlineData("0",         null)]
    [InlineData("0.0000",    null)]
    [InlineData("10.5",      "10.5")]
    [InlineData("abc",       null)]
    [InlineData("-5",        "-5")]
    public void ParseReorderLevel_handles_edge_cases(string? raw, string? expected)
    {
        var result = SageImportHelpers.ParseReorderLevel(raw);
        if (expected is null)
            result.Should().BeNull();
        else
            result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }
}
