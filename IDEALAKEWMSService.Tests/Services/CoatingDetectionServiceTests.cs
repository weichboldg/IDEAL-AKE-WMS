using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class CoatingDetectionServiceTests
{
    [Fact]
    public void Constructor_Succeeds_WithMinimalConfig()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()).Build();
        var svc = new CoatingDetectionService(cfg, NullLogger<CoatingDetectionService>.Instance, new FakeSyncLogger());
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectAndUpdate_Throws_WhenWmsConnectionMissing()
    {
        // No DefaultConnection -> ConnectionStrings.Wms() throws InvalidOperationException
        // Since ISyncLogger integration re-throws, the exception propagates
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()).Build();
        var svc = new CoatingDetectionService(cfg, NullLogger<CoatingDetectionService>.Instance, new FakeSyncLogger());

        var act = () => svc.DetectAndUpdateCoatingFlagsAsync(
            dryRun: false, specificOrderIds: null, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
