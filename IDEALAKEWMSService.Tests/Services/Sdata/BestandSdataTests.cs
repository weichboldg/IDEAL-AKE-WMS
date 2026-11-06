using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using IDEALAKEWMSService.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E2: SData-Bestand-Reader, Weiche, Abgleich-Schutz, G10, DI.</summary>
public class BestandSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static SdataSeiten<T> Seiten<T>(string ressource, params T[] zeilen) => new(zeilen, ressource, 1, zeilen.Length, 1);

    private static void Setup<T>(Mock<ISageSdataReadClient> client, string ressource, SdataSeiten<T>? seiten, Exception? fehler = null)
    {
        var s = client.Setup(c => c.QueryAllAsync<T>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), ressource, It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()));
        if (fehler is not null) s.ThrowsAsync(fehler);
        else s.ReturnsAsync(seiten!);
    }

    private static (SdataBestandReader Reader, Mock<ISageSdataReadClient> Client) Reader(SdataBestandZeile[] bestand, SdataLagerplatzZeile[] plaetze,
        Exception? chargenFehler = null)
    {
        var client = new Mock<ISageSdataReadClient>();
        Setup(client, SdataBestandReader.RessourceBestand, Seiten(SdataBestandReader.RessourceBestand, bestand));
        Setup(client, SdataLagerplatzReader.RessourceLagerplaetze, Seiten(SdataLagerplatzReader.RessourceLagerplaetze, plaetze));
        Setup(client, SdataBestandReader.RessourceChargen, chargenFehler is null ? Seiten(SdataBestandReader.RessourceChargen, default(JsonElement), default) : null, chargenFehler);
        Setup(client, SdataBestandReader.RessourceSeriennummern, Seiten<JsonElement>(SdataBestandReader.RessourceSeriennummern));
        return (new SdataBestandReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataBestandReader>.Instance), client);
    }

    private static SdataLagerplatzZeile Platz(int id, string ort, string kurz) => new(id, ort, kurz, null, null, null, null, null, null, null, null);

    // ----- Reader-Mapping (Fake-Client) -----

    [Fact]
    public async Task Reader_SummiertJeArtikelPlatzLagerort_WieSql_UndZaehltChargen()
    {
        var (reader, client) = Reader(
            new SdataBestandZeile[]
            {
                new("00722001", 11, 150m),
                new("00722001", 11, 50m),     // zweite Auspraegung, gleicher Platz -> SUM
                new("00722001 ", 12, 1m),     // gleicher Kurzname in anderem Lagerort -> eigenes Tupel (Dublette fuer den Abgleich)
                new("86003871", 11, null),    // ohne Bestand -> weg (SQL Bestand IS NOT NULL)
                new("86003871", 99, 3m),      // Platz unbekannt -> Tupel ohne Lagerplatz (Abgleich ueberspringt)
            },
            new[] { Platz(11, "HR", "HR 0 0 0"), Platz(12, "LL", "hr 0 0 0") });

        var dtos = await reader.LesenAsync(Ziel, CancellationToken.None);

        dtos.Should().BeEquivalentTo(new[]
        {
            new SageBestandDto("00722001", "HR 0 0 0", 200m),
            new SageBestandDto("00722001 ", "hr 0 0 0", 1m),
            new SageBestandDto("86003871", null, 3m),
        });
        reader.Hinweise.Should().Contain(h => h.Text.Contains("Chargen") && h.Text.Contains("2 Zeile(n)") && !h.Warnung);
        reader.Hinweise.Should().Contain(h => h.Text.Contains("Seriennummern") && h.Text.Contains("0 Zeile(n)"));
        client.Verify(c => c.QueryAllAsync<SdataBestandZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerbestaendeArtikelInfo", "Mandant eq 1",
            null, SdataBestandReader.OrderByBestand, It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        client.Verify(c => c.QueryAllAsync<SdataLagerplatzZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplaetzeInfo", null,
            SdataBestandReader.SelectPlaetze, "PlatzID", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Reader_ChargenNichtLesbar_IstNurWarnung()
    {
        var (reader, _) = Reader(new SdataBestandZeile[] { new("A", 1, 1m) }, new[] { Platz(1, "HR", "P1") },
            chargenFehler: new SdataLeseException(SdataBestandReader.RessourceChargen, 500, "Spalte unbekannt"));

        (await reader.LesenAsync(Ziel, CancellationToken.None)).Should().ContainSingle();
        reader.Hinweise.Should().Contain(h => h.Warnung && h.Text.StartsWith("Chargen nicht gezählt"));
    }

    [Fact]
    public async Task Reader_SDataFehler_Wirft_KeineLeereListe()
    {
        var client = new Mock<ISageSdataReadClient>();
        Setup<SdataBestandZeile>(client, SdataBestandReader.RessourceBestand, null, new SdataLeseException("LagerbestaendeArtikelInfo", 401, "Anmeldung"));
        var reader = new SdataBestandReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataBestandReader>.Instance);

        await ((Func<Task>)(() => reader.LesenAsync(Ziel, CancellationToken.None))).Should().ThrowAsync<SdataLeseException>();
    }

    [Theory]
    [InlineData(null, 1, "1")]   // Artikelnummer nicht gemappt
    [InlineData("A", null, "1")] // PlatzID nicht gemappt
    [InlineData("A", 1, null)]   // Bestand nirgends gemappt
    public void Reader_FalschGemappteZeilen_Wirft_StattNullsetzen(string? artikel, int? platz, string? bestand)
    {
        var zeilen = new[] { new SdataBestandZeile(artikel, platz, bestand is null ? null : decimal.Parse(bestand)) };

        ((Action)(() => SdataBestandReader.Abbilden(zeilen, new[] { Platz(1, "HR", "P1") }))).Should().Throw<SdataLeseException>();
    }

    // ----- Weiche: alle drei Modi -----

    private sealed class TestSwitch : BestandQuelleSwitch
    {
        public string? Schalter { get; set; }
        public TestSwitch(ISageBestandReader view, ISageBestandReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
    }

    private static (TestSwitch Switch, FakeSageBestandReader View, FakeSageBestandReader Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier)
        Weiche(string? schalter)
    {
        var view = new FakeSageBestandReader { Records = new() { new("A-1", "L-1", 5m), new("A-2", "L-1", 1.50m) } };
        var sdata = new FakeSageBestandReader { Records = new() { new("a-1", "L-1", 5m), new("A-2", "L-1", 2m) } };
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView()
    {
        var (w, _, sdata, logger, _) = Weiche(null);
        sdata.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        (await w.GetAllAsync()).Should().HaveCount(2);
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
        w.SdataHinweise.Should().BeEmpty();
    }

    [Fact]
    public async Task Weiche_SData_LiestNurSData_FehlerWirftDurch_OhneMail()
    {
        var (w, view, sdata, _, notifier) = Weiche("SData");
        view.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        (await w.GetAllAsync()).Should().Contain(d => d.Bestand == 2m);

        sdata.ThrowOnRead = new SdataLeseException("LagerbestaendeArtikelInfo", 500, "kaputt");
        await ((Func<Task>)(() => w.GetAllAsync())).Should().ThrowAsync<SdataLeseException>();
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Weiche_Vergleich_ViewZeilen_AbweichungJeArtikelUndPlatz()
    {
        var (w, _, _, logger, _) = Weiche("Vergleich");

        (await w.GetAllAsync()).Should().Contain(d => d.Bestand == 1.50m, "geschrieben wird nur aus der View");

        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1, "A-1/a-1 gleich (Gross/Klein egal), A-2 Bestand 1.5 vs 2");
        lauf.Events.Should().Contain(e => e.Message.Contains("[A-2 @ L-1] Bestand") && e.Message.Contains("'1.5'") && e.Message.Contains("'2'"));
    }

    [Fact]
    public async Task Weiche_Vergleich_SDataFehler_BlockiertNicht_MeldetUndProtokolliert()
    {
        var (w, _, sdata, logger, notifier) = Weiche("Vergleich");
        sdata.ThrowOnRead = new SdataLeseException("LagerbestaendeArtikelInfo", 0, "nicht erreichbar");

        (await w.GetAllAsync()).Should().HaveCount(2);
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<SdataLeseException>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ----- Abgleich: Quelle-Zeile, kein Nullsetzen bei Ausfall, G10 -----

    private static (LagerbestandSyncService Svc, FakeSageBestandReader View, FakeSageBestandReader Sdata,
        IdealAkeWms.Data.ApplicationDbContext Ctx, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Abgleich(string? schalter)
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.Add(new Article { Id = 1, ArticleNumber = "A-1", Description = "x", Unit = "Stk", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.StorageLocations.Add(new StorageLocation { Id = 1, Code = "L-1", Source = StorageLocationSource.Sage, IsActive = true, CreatedBy = "x", CreatedByWindows = "x" });
        ctx.StockMovements.Add(new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 5m, MovementType = MovementType.SageEinbuchung,
            Timestamp = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();
        var (w, view, sdata, logger, notifier) = Weiche(schalter);
        var svc = new LagerbestandSyncService(ctx, w, new StockMovementRepository(ctx), new ConfigurationBuilder().Build(), notifier.Object,
            NullLogger<LagerbestandSyncService>.Instance, logger);
        return (svc, view, sdata, ctx, logger, notifier);
    }

    [Fact]
    public async Task Abgleich_SData_GleicherBestand_KeineKorrektur_QuelleImProtokoll()
    {
        var (svc, _, _, ctx, logger, _) = Abgleich("SData");

        var r = await svc.RunAsync(dryRun: false);

        (r.CorrectionsPlus, r.CorrectionsMinus, r.NoChange).Should().Be((0, 0, 1));
        ctx.StockMovements.Count().Should().Be(1);
        logger.Runs.Single(x => x.ServiceName == SyncLogServices.Lagerbestand).Events.Should().Contain(("Info", "Quelle: SData", null));
    }

    [Theory]
    [InlineData("SData", 1)]
    [InlineData("View", 0)]
    public async Task Abgleich_LeseFehler_KeinNullsetzen_FehlermailNurBeiSData_G10(string schalter, int mails)
    {
        var (svc, view, sdata, ctx, logger, notifier) = Abgleich(schalter);
        view.ThrowOnRead = new InvalidOperationException("SQL weg");
        sdata.ThrowOnRead = new SdataLeseException("LagerbestaendeArtikelInfo", 0, "nicht erreichbar");

        var r = await svc.RunAsync(dryRun: false);

        r.Errors.Should().Be(1);
        ctx.StockMovements.Count().Should().Be(1, "Lesefehler bucht nichts, setzt nichts auf 0");
        logger.Runs.Single(x => x.ServiceName == SyncLogServices.Lagerbestand).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync("Lagerbestand-Sync", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Exactly(mails));
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_BestandReader_IstWeiche_MitBeidenReadern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddSageSdataReadClient();
        services.AddBestandQuelle();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageBestandReader>().Should().BeOfType<BestandQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageBestandReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataBestandReader>().Should().NotBeNull();
    }
}
