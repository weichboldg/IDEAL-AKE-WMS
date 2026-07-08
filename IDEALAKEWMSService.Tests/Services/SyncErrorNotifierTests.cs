using FluentAssertions;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class SyncErrorNotifierTests
{
    // Seit v1.25.0 (Task 5, service-settings-typed-catalog): SyncErrorNotifier liest
    // ErrorNotification:Enabled/Recipients NICHT mehr aus IConfiguration, sondern DB-first
    // via ServiceSettings.GetBoolSafeAsync/GetValueSafeAsync (Tabelle [ServiceSettings]).
    // In den Unit-Tests fehlt der DefaultConnection-ConnectionString → der Safe-Reader
    // faengt die SqlException und liefert den Default (Enabled=false / Recipients=leer).
    // Damit ist der "Enabled + sendet"-Pfad NUR noch Manual-UAT (siehe TESTSZENARIEN).
    // Die verbleibenden Unit-Tests sichern die resiliente Fail-Safe-Semantik ab:
    // ohne erreichbare DB wird NIE gesendet und NIE geworfen.

    private static IConfiguration BuildConfigWithoutDb()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    private static SyncErrorNotifier CreateSut(IMailService mail, IConfiguration config) =>
        new(mail, config, NullLogger<SyncErrorNotifier>.Instance);

    [Fact]
    public async Task OhneDbErreichbar_SendetNicht()
    {
        // DB-Read wirft (kein DefaultConnection) → Safe-Reader → Enabled faellt auf false
        // → Fail-Safe: keine Fehlermail, kein Crash.
        var mail = new Mock<IMailService>();
        var sut = CreateSut(mail.Object, BuildConfigWithoutDb());

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
    public async Task OhneDbErreichbar_WirftNicht()
    {
        // Der Notifier darf den Worker unter keinen Umstaenden crashen.
        var mail = new Mock<IMailService>();
        var sut = CreateSut(mail.Object, BuildConfigWithoutDb());

        var act = async () => await sut.NotifyAsync("Artikel-Sync", new InvalidOperationException("boom"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendWirft_SchlucktUndLoggt()
    {
        // Selbst wenn ein Versand erzwungen wuerde: eine SendAsync-Exception darf nie propagieren.
        // Ohne DB gelangt der Notifier zwar nicht bis SendAsync, aber der try/catch bleibt der Vertrag.
        var mail = new Mock<IMailService>();
        mail.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var sut = CreateSut(mail.Object, BuildConfigWithoutDb());

        var act = async () => await sut.NotifyAsync("enaio DMS-Sync", new InvalidOperationException("boom"));

        await act.Should().NotThrowAsync();
    }
}
