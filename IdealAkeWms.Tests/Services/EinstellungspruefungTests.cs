using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.Settings;
using IdealAkeWms.Services.SyncLogger;
using Moq;

namespace IdealAkeWms.Tests.Services;

/// <summary>Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung, E4b (AK 18): Bestandspruefung beim Web-Start.</summary>
public class EinstellungspruefungTests
{
    private static readonly IReadOnlyDictionary<SettingStammdaten, IReadOnlyList<string>> Stamm =
        new Dictionary<SettingStammdaten, IReadOnlyList<string>>
        {
            [SettingStammdaten.Bestellempfaengergruppe] = new[] { "7" },
            [SettingStammdaten.Lagerplatz] = new[] { "NAN" },
        };

    [Fact]
    public void Befunde_JeUngueltigemWert_MitKeyRohwertErlaubtWirksam()
    {
        var befunde = Einstellungspruefung.Befunde(
            new (string, string?)[] { ("LagerbestellungAktiv", "1"), ("BdeAktiv", "true"), ("DefaultLagerbestellempfaengerId", "99"), ("Unbekannt", "x") },
            new (string, string?)[] { ("Sync:Quelle:Lagerplaetze", "sdata"), ("Sync:BomCacheWeeks", "8"), ("Sync:ProductionOrdersEnabled", "") },
            Stamm);

        befunde.Should().HaveCount(3);
        befunde.Should().Contain(b => b.Contains("LagerbestellungAktiv") && b.Contains("„1“") && b.Contains("Wirksam: „true“"));
        befunde.Should().Contain(b => b.Contains("DefaultLagerbestellempfaengerId") && b.Contains("„99“"));
        befunde.Should().Contain(b => b.Contains("Sync:Quelle:Lagerplaetze") && b.Contains("„sdata“") && b.Contains("„View“, „SData“, „Vergleich“"));
    }

    [Fact]
    public async Task Protokoll_NurBeiBefunden_EinLaufMitWarnzeilen()
    {
        var run = new Mock<ISyncRun>();
        var logger = new Mock<ISyncLogger>();
        logger.Setup(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(run.Object);

        await Einstellungspruefung.ProtokollierenAsync(logger.Object, Array.Empty<string>());
        logger.Verify(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never, "ohne ungueltige Werte kein Lauf");

        await Einstellungspruefung.ProtokollierenAsync(logger.Object, new[] { "a", "b" });
        logger.Verify(l => l.BeginRunAsync(SyncLogServices.Einstellungspruefung, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.LogWarningAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        SyncLogServices.DisplayName(SyncLogServices.Einstellungspruefung).Should().Be("Einstellungsprüfung");
    }
}
