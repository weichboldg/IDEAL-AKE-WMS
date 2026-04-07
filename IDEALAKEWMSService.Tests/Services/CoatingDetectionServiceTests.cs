using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
        var svc = new CoatingDetectionService(cfg, NullLogger<CoatingDetectionService>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectAndUpdate_ReportsError_WhenWmsConnectionMissing()
    {
        // No DefaultConnection -> ConnectionStrings.Wms() throws InvalidOperationException
        // Wrapped in try/catch by service, returns SyncResult with errors > 0
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()).Build();
        var svc = new CoatingDetectionService(cfg, NullLogger<CoatingDetectionService>.Instance);

        var result = await svc.DetectAndUpdateCoatingFlagsAsync(
            dryRun: false, specificOrderIds: null, CancellationToken.None);

        result.Errors.Should().BeGreaterThan(0);
        result.ErrorDetails.Should().NotBeNullOrEmpty();
    }
}
