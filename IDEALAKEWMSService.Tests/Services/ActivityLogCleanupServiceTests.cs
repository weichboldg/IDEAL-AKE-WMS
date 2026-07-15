using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class ActivityLogCleanupServiceTests
{
    private static (ActivityLogCleanupService svc, Mock<ISyncLogRepository> repo, Mock<ISyncLogger> logger, Mock<ISyncRun> run) Build()
    {
        var repo = new Mock<ISyncLogRepository>();
        var run = new Mock<ISyncRun>();
        var logger = new Mock<ISyncLogger>();
        logger.Setup(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(run.Object);
        var svc = new ActivityLogCleanupService(repo.Object, logger.Object, NullLogger<ActivityLogCleanupService>.Instance);
        return (svc, repo, logger, run);
    }

    [Fact]
    public async Task RunAsync_disabled_skips_without_delete_or_run()
    {
        var (svc, repo, logger, _) = Build();

        var result = await svc.RunAsync(retentionDays: 0, dryRun: false, CancellationToken.None);

        result.Skipped.Should().BeTrue();
        result.Deleted.Should().Be(0);
        logger.Verify(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_enabled_deletes_and_writes_one_run_with_count()
    {
        var (svc, repo, logger, run) = Build();
        repo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await svc.RunAsync(retentionDays: 180, dryRun: false, CancellationToken.None);

        result.Skipped.Should().BeFalse();
        result.Deleted.Should().Be(7);
        logger.Verify(l => l.BeginRunAsync(SyncLogServices.CleanupActivityLog, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.FinishSuccessAsync(
            It.Is<IReadOnlyDictionary<string, int>>(d => d["geloescht"] == 7),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_dryRun_passes_dryRun_to_repo()
    {
        var (svc, repo, _, _) = Build();
        repo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await svc.RunAsync(retentionDays: 180, dryRun: true, CancellationToken.None);

        result.Deleted.Should().Be(3);
        repo.Verify(r => r.DeleteOlderThanAsync(It.IsAny<System.DateTime>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
