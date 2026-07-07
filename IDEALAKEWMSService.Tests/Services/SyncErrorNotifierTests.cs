using FluentAssertions;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class SyncErrorNotifierTests
{
    private static IConfiguration BuildConfig(bool enabled, params string[] recipients)
    {
        var dict = new Dictionary<string, string?>
        {
            ["ErrorNotification:Enabled"] = enabled ? "true" : "false",
        };
        for (var i = 0; i < recipients.Length; i++)
            dict[$"ErrorNotification:Recipients:{i}"] = recipients[i];

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static SyncErrorNotifier CreateSut(IMailService mail, IConfiguration config) =>
        new(mail, config, NullLogger<SyncErrorNotifier>.Instance);

    [Fact]
    public async Task Disabled_SendetNicht()
    {
        var mail = new Mock<IMailService>();
        var sut = CreateSut(mail.Object, BuildConfig(enabled: false, "a@x"));

        await sut.NotifyAsync("Produktionsauftraege-Sync", new InvalidOperationException("boom"));

        mail.Verify(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task KeineEmpfaenger_SendetNicht()
    {
        var mail = new Mock<IMailService>();
        var sut = CreateSut(mail.Object, BuildConfig(enabled: true));

        await sut.NotifyAsync("Artikel-Sync", new InvalidOperationException("boom"));

        mail.Verify(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Aktiviert_SendetMitDetails()
    {
        var mail = new Mock<IMailService>();
        string? capturedSubject = null;
        string? capturedText = null;
        mail.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>(
                (subject, _, _, text, _) => { capturedSubject = subject; capturedText = text; })
            .Returns(Task.CompletedTask);

        var sut = CreateSut(mail.Object, BuildConfig(enabled: true, "a@x"));
        var ex = new InvalidOperationException("etwas ist schiefgelaufen");

        await sut.NotifyAsync("OSEON-Tracking-Sync", ex);

        mail.Verify(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IEnumerable<string>>(e => e.Contains("a@x")),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        capturedSubject.Should().NotBeNull();
        capturedSubject!.Should().Contain("OSEON-Tracking-Sync");
        capturedText.Should().NotBeNull();
        capturedText!.Should().Contain("etwas ist schiefgelaufen");
        capturedText.Should().Contain(Environment.MachineName);
    }

    [Fact]
    public async Task SendWirft_SchlucktUndLoggt()
    {
        var mail = new Mock<IMailService>();
        mail.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var sut = CreateSut(mail.Object, BuildConfig(enabled: true, "a@x"));

        var act = async () => await sut.NotifyAsync("enaio DMS-Sync", new InvalidOperationException("boom"));

        await act.Should().NotThrowAsync();
    }
}
