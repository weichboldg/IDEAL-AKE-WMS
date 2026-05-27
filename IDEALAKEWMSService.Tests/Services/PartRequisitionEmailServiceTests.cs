using FluentAssertions;
using IDEALAKEWMSService.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services;

public class PartRequisitionEmailServiceTests
{
    [Fact]
    public async Task Send_writes_lifecycle_to_synclogger_when_no_pending_emails()
    {
        // PartRequisitionEmailService uses raw ADO.NET (no mockable DB layer).
        // A missing/empty connection string causes InvalidOperationException AFTER
        // BeginRunAsync — so we verify the run was started and finished-failed.
        var config = new ConfigurationBuilder().Build(); // no connection string
        var mailMock = new Mock<IMailService>();
        var fakeLogger = new FakeSyncLogger();
        var service = new PartRequisitionEmailService(
            config, mailMock.Object,
            NullLogger<PartRequisitionEmailService>.Instance, fakeLogger);

        Func<Task> act = () => service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

        // The service throws because DefaultConnection is null — that's expected here.
        await act.Should().ThrowAsync<Exception>();

        // The run MUST have been started and properly finished-failed before throwing.
        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("PartRequisitionEmail");
        fakeLogger.Runs[0].FinishedFailed.Should().BeTrue();
    }
}
