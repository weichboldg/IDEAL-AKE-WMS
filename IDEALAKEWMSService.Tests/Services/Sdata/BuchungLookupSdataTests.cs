using FluentAssertions;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E7/F9: SData-Lagerbuchungs-Lookup (Memo), Weiche, DI. Keine Buchung, Fake-Client.</summary>
public class BuchungLookupSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01.ake.at:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static Mock<ISageSdataReadClient> Client(params SdataBuchungZeile[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataBuchungZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataBuchungZeile>(zeilen, SdataBuchungLookupReader.Ressource, 1, zeilen.Length, 1));
        return client;
    }

    private static SdataBuchungLookupReader Reader(Mock<ISageSdataReadClient> client) => new(client.Object, new ConfigurationBuilder().Build());

    // ----- Reader-Mapping -----

    [Fact]
    public async Task Reader_Treffer_True_AbfrageMitMemoMuster_Count1_OrderBy()
    {
        var client = Client(new SdataBuchungZeile(865823));

        (await Reader(client).ExistsAsync(Ziel, "SM#19376#", CancellationToken.None)).Should().BeTrue();

        client.Verify(c => c.QueryAllAsync<SdataBuchungZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplatzbuchungenInfo",
            "Memo like '%SM#19376#%'", "BuchungsID", "BuchungsID", It.IsAny<CancellationToken>(), 1), Times.Once);
    }

    [Fact]
    public async Task Reader_MitKuerzel_SuchtGenauDieKennung_v1_99_9()
    {
        var client = Client(new SdataBuchungZeile(1));
        (await Reader(client).ExistsAsync(Ziel, "SM#DEV#7#", CancellationToken.None)).Should().BeTrue();
        client.Verify(c => c.QueryAllAsync<SdataBuchungZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplatzbuchungenInfo",
            "Memo like '%SM#DEV#7#%'", "BuchungsID", "BuchungsID", It.IsAny<CancellationToken>(), 1), Times.Once);
    }

    [Fact]
    public async Task Reader_KeinTreffer_False()
    {
        (await Reader(Client()).ExistsAsync(Ziel, "SM#7#", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Reader_SDataFehler_Wirft_NieFalse()
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataBuchungZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("LagerplatzbuchungenInfo", 503, "nicht verfügbar"));

        await ((Func<Task>)(() => Reader(client).ExistsAsync(Ziel, "SM#7#", CancellationToken.None))).Should().ThrowAsync<SdataLeseException>();
    }

    [Fact]
    public async Task Reader_OhneKonfiguration_Wirft()
    {
        // ExistsAsync(marker) liest das Ziel aus ServiceSettings/appsettings; fehlt es, darf nie "nicht gebucht" herauskommen.
        await ((Func<Task>)(() => Reader(Client()).ExistsAsync("SM#7#"))).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Url_KodiertRauteUndProzent()
    {
        // '#' wuerde die URL als Fragment abschneiden -> %23; '%' -> %25 (E0). TODO(UAT E7): Dekodierung am Sage-Server.
        SageSdataReadClient.BuildQueryUrl(Ziel, SageSdataReadClient.VertragResources, SdataBuchungLookupReader.Ressource,
                SdataBuchungLookupReader.Where("SM#12#"), SdataBuchungLookupReader.Select, SdataBuchungLookupReader.OrderBy, 1, 1)
            .Should().EndWith("&where=Memo like '%25SM%2312%23%25'");
    }

    // ----- Weiche -----

    private sealed class FakeLookup : ISageBuchungLookupReader
    {
        public bool Ergebnis { get; set; }
        public Exception? Fehler { get; set; }
        public int Aufrufe { get; private set; }

        public Task<bool> ExistsAsync(string marker, CancellationToken ct = default)
        {
            Aufrufe++;
            return Fehler is null ? Task.FromResult(Ergebnis) : Task.FromException<bool>(Fehler);
        }
    }

    private sealed class TestSwitch : BuchungLookupQuelleSwitch
    {
        public string? Schalter { get; set; }

        public TestSwitch(ISageBuchungLookupReader view, ISageBuchungLookupReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }

        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
    }

    private static (TestSwitch W, FakeLookup View, FakeLookup Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Weiche(string? schalter)
    {
        var view = new FakeLookup();
        var sdata = new FakeLookup();
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("View")]
    public async Task Weiche_DefaultView_NurSqlReader(string? schalter)
    {
        var (w, view, sdata, logger, _) = Weiche(schalter);
        view.Ergebnis = true;

        (await w.ExistsAsync("SM#5#")).Should().BeTrue();
        sdata.Aufrufe.Should().Be(0);
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_NurSData_FehlerWirftDurch_KeineMail()
    {
        var (w, view, sdata, _, notifier) = Weiche("SData");
        sdata.Ergebnis = true;

        (await w.ExistsAsync("SM#5#")).Should().BeTrue();
        view.Aufrufe.Should().Be(0);

        sdata.Fehler = new SdataLeseException("LagerplatzbuchungenInfo", 0, "nicht erreichbar");
        await ((Func<Task>)(() => w.ExistsAsync("SM#5#"))).Should().ThrowAsync<SdataLeseException>("der Worker sendet dann nicht neu");
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Weiche_Vergleich_ViewErgebnisGilt_AbweichungProtokolliert()
    {
        var (w, view, sdata, logger, _) = Weiche("Vergleich");
        view.Ergebnis = false;
        sdata.Ergebnis = true;

        (await w.ExistsAsync("SM#5#")).Should().BeFalse("im Vergleich gilt das View-Ergebnis");

        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1);
        lauf.Events.Should().Contain(e => e.Message.Contains("[SM#5#]") && e.Message.Contains("fehlt in View"));
    }

    [Fact]
    public async Task Weiche_Vergleich_SDataFehler_ViewErgebnis_MeldetUndProtokolliert()
    {
        var (w, view, sdata, logger, notifier) = Weiche("Vergleich");
        view.Ergebnis = true;
        sdata.Fehler = new SdataLeseException("LagerplatzbuchungenInfo", 500, "kaputt");

        (await w.ExistsAsync("SM#5#")).Should().BeTrue();
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<SdataLeseException>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Weiche_Vergleich_ViewFehler_WirftDurch()
    {
        var (w, view, _, _, _) = Weiche("Vergleich");
        view.Fehler = new InvalidOperationException("Sage-DB weg");

        await ((Func<Task>)(() => w.ExistsAsync("SM#5#"))).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Weiche_UnbekannterWert_View_MitWarnung()
    {
        var (w, view, sdata, logger, _) = Weiche("sdata");
        view.Ergebnis = true;

        (await w.ExistsAsync("SM#5#")).Should().BeTrue();
        sdata.Aufrufe.Should().Be(0);
        logger.Runs.Single().Events.Should().Contain(e => e.Message.Contains("LagerbuchungLookup"));
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_BuchungLookup_IstWeiche_MitBeidenReadern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddSageSdataReadClient();
        services.AddBuchungLookupQuelle();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageBuchungLookupReader>().Should().BeOfType<BuchungLookupQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageBuchungLookupReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataBuchungLookupReader>().Should().NotBeNull();
    }
}
