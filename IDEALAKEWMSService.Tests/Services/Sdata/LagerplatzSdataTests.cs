using FluentAssertions;
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

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E1: SData-Lagerplatz-Reader, Weiche, Nicht-null-Regel, Lagerorte, G10, DI.</summary>
public class LagerplatzSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static SdataSeiten<T> Seiten<T>(string ressource, params T[] zeilen) => new(zeilen, ressource, 1, zeilen.Length, 1);

    private static (SdataLagerplatzReader Reader, Mock<ISageSdataReadClient> Client) Reader(SdataLagerortZeile[] orte, SdataLagerplatzZeile[] plaetze)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataLagerortZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(Seiten(SdataLagerplatzReader.RessourceLagerorte, orte));
        client.Setup(c => c.QueryAllAsync<SdataLagerplatzZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(Seiten(SdataLagerplatzReader.RessourceLagerplaetze, plaetze));
        return (new SdataLagerplatzReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataLagerplatzReader>.Instance), client);
    }

    private static SdataLagerplatzZeile Platz(int id, string ort, string kurz, string? bez = null, bool? gesperrt = false, decimal? breite = null) =>
        new(id, ort, kurz, bez, gesperrt, false, breite, null, null, null, null);

    // ----- Reader-Mapping (Fake-Client) -----

    [Fact]
    public async Task Reader_NurAktiveLagerorte_LeftJoin_FelderUndAbfrage()
    {
        var (reader, client) = Reader(
            new[]
            {
                new SdataLagerortZeile("HR", "Hochregal", "HRL", true, true),
                new SdataLagerortZeile("ALT", "Altlager", null, false, false),   // inaktiv -> nicht in der Menge
                new SdataLagerortZeile("LEER", "Ohne Plaetze", null, null, true), // aktiv ohne Platz -> eine Zeile ohne Kurzbezeichnung
            },
            new[]
            {
                Platz(11, "hr ", "HR 0 0 0", "Regal 0", gesperrt: true, breite: 1.23456m), // Join wie SQL: Gross/Klein + Leerzeichen am Ende egal
                Platz(12, "ALT", "ALT 1"),
                Platz(13, "WEG", "WEG 1"), // Lagerort nicht in der Menge
            });

        var dtos = await reader.LesenAsync(Ziel, CancellationToken.None);

        dtos.Should().HaveCount(2);
        var hr = dtos.Single(d => d.Kurzbezeichnung == "HR 0 0 0");
        hr.Should().BeEquivalentTo(new SageLagerplatzDto("HR", "HR 0 0 0", "Regal 0", 11, new SageLagerortDto("HR", "Hochregal", "HRL", true),
            IstGesperrt: true, IstGesperrtInventur: false, Breite: 1.235m));
        var leer = dtos.Single(d => d.Lagerkennung == "LEER");
        leer.Kurzbezeichnung.Should().BeNull();
        leer.Lagerort!.IstDispoLager.Should().BeFalse("null aus Sage = nein");

        client.Verify(c => c.QueryAllAsync<SdataLagerortZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerorteInfo", "Mandant eq 1",
            SdataLagerplatzReader.SelectLagerorte, "Lagerkennung", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        client.Verify(c => c.QueryAllAsync<SdataLagerplatzZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplaetzeInfo", null,
            SdataLagerplatzReader.SelectLagerplaetze, "PlatzID", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Reader_SDataFehler_Wirft_KeineLeereListe()
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataLagerortZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("LagerorteInfo", 401, "Anmeldung"));
        var reader = new SdataLagerplatzReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataLagerplatzReader>.Instance);

        await ((Func<Task>)(() => reader.LesenAsync(Ziel, CancellationToken.None))).Should().ThrowAsync<SdataLeseException>();
    }

    // ----- Weiche: alle drei Modi -----

    private sealed class TestSwitch : LagerplatzQuelleSwitch
    {
        public string? Schalter { get; set; }
        public TestSwitch(ISageLagerplatzReader view, ISageLagerplatzReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
    }

    private static (TestSwitch Switch, FakeSageLagerplatzReader View, FakeSageLagerplatzReader Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier)
        Weiche(string? schalter)
    {
        var view = new FakeSageLagerplatzReader { Records = new() { new("HR", "P1", "Platz 1", 1), new("HR", "P2", "Platz 2", 2) } };
        var sdata = new FakeSageLagerplatzReader { Records = new() { new("HR", "p1", "Platz 1", 1, new("HR", "Hochregal", null, false)), new("HR", "P2", "Platz zwei", 2) } };
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView()
    {
        var (w, _, sdata, logger, _) = Weiche("View");
        sdata.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        (await w.GetAllActiveAsync()).Select(d => d.Kurzbezeichnung).Should().Equal("P1", "P2");
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_LiestNurSData_FehlerWirftDurch()
    {
        var (w, view, sdata, _, notifier) = Weiche("SData");
        view.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        (await w.GetAllActiveAsync()).Should().HaveCount(2).And.Contain(d => d.Lagerort != null);

        sdata.ThrowOnRead = new SdataLeseException("LagerplaetzeInfo", 500, "kaputt");
        await ((Func<Task>)(() => w.GetAllActiveAsync())).Should().ThrowAsync<SdataLeseException>();
        notifier.VerifyNoOtherCalls(); // Mail im SData-Modus schickt der Abgleich (G10), nicht die Weiche
    }

    [Fact]
    public async Task Weiche_Vergleich_ViewZeilen_AbweichungJeKurzbezeichnung()
    {
        var (w, _, _, logger, _) = Weiche("Vergleich");

        var ergebnis = await w.GetAllActiveAsync();

        ergebnis.Should().OnlyContain(d => d.Lagerort == null, "geschrieben wird nur aus der View");
        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1, "P1/p1 gleich (Gross/Klein egal), P2 Description abweichend");
        lauf.Events.Should().Contain(e => e.Message.Contains("[P2] Description") && e.Message.Contains("'Platz 2'") && e.Message.Contains("'Platz zwei'"));
    }

    [Fact]
    public async Task Weiche_Vergleich_SDataFehler_BlockiertNicht_MeldetUndProtokolliert()
    {
        var (w, _, sdata, logger, notifier) = Weiche("Vergleich");
        sdata.ThrowOnRead = new SdataLeseException("LagerplaetzeInfo", 0, "nicht erreichbar");

        (await w.GetAllActiveAsync()).Select(d => d.Kurzbezeichnung).Should().Equal("P1", "P2");

        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<SdataLeseException>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ----- Abgleich: Nicht-null-Regel, Lagerorte, Quelle-Zeile, G10 -----

    private static (LagerplatzSyncService Svc, TestSwitch Switch, FakeSageLagerplatzReader View, FakeSageLagerplatzReader Sdata,
        IdealAkeWms.Data.ApplicationDbContext Ctx, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Abgleich(string? schalter)
    {
        var ctx = TestDbContextFactory.Create();
        var (w, view, sdata, logger, notifier) = Weiche(schalter);
        var svc = new LagerplatzSyncService(ctx, w, NullLogger<LagerplatzSyncService>.Instance, notifier.Object, logger);
        return (svc, w, view, sdata, ctx, logger, notifier);
    }

    [Fact]
    public async Task Abgleich_SData_SchreibtNeueFelderUndLagerort_ViewLaufLaesstSieStehen()
    {
        var (svc, w, view, sdata, ctx, logger, _) = Abgleich("SData");
        var ort = new SageLagerortDto("HR", "Hochregal", "HRL", true);
        sdata.Records = new() { new("HR", "P1", "Platz 1", 1, ort, true, false, 1.5m, 2m, 3m, 4m, 500m) };

        await svc.RunAsync();

        var p = ctx.StorageLocations.Single();
        p.StorageArea!.Lagerkennung.Should().Be("HR");
        (p.SageIstGesperrt, p.SageIstGesperrtInventur, p.SageBreite, p.SageTragkraft).Should().Be((true, false, 1.5m, 500m));
        p.IstBuchbar.Should().BeFalse("Neuanlage wie heute");
        var area = ctx.StorageAreas.Single();
        (area.Bezeichnung, area.Lagertechnik, area.IstDispoLager, area.IsActive, area.CreatedBy).Should().Be(("Hochregal", "HRL", true, true, "system:sync"));
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.Lagerplatz).Events.Should().Contain(("Info", "Quelle: SData", null));

        // Rueckfall auf View: Platzbezeichnung aendert sich, SData-only-Felder bleiben (nicht genullt), Lagerort unberuehrt
        p.IstBuchbar = true;
        await ctx.SaveChangesAsync();
        w.Schalter = "View";
        view.Records = new() { new("HR", "P1", "Platz 1 neu", 1) };

        var r = await svc.RunAsync();

        r.Updated.Should().Be(1);
        p.Description.Should().Be("Platz 1 neu");
        p.StorageAreaId.Should().Be(area.Id);
        (p.SageIstGesperrt, p.SageBreite, p.SageTragkraft).Should().Be((true, 1.5m, 500m));
        p.IstBuchbar.Should().BeTrue("WMS-gefuehrt, nie ueberschrieben");
        area.SageMissingSince.Should().BeNull("View liefert keine Lagerort-Daten -> Tabelle unberuehrt");
        logger.Runs.Last(x => x.ServiceName == SyncLogServices.Lagerplatz).Events.Should().Contain(("Info", "Quelle: View", null));
    }

    [Fact]
    public async Task Abgleich_SData_UnveraenderteDaten_KeinUpdate_GeaenderteSperre_Update()
    {
        var (svc, _, _, sdata, ctx, _, _) = Abgleich("SData");
        var ort = new SageLagerortDto("HR", "Hochregal", null, false);
        sdata.Records = new() { new("HR", "P1", "Platz 1", 1, ort, false, false) };
        await svc.RunAsync();

        (await svc.RunAsync()).Updated.Should().Be(0, "gleiche Werte = keine Aenderung");

        sdata.Records = new() { new("HR", "P1", "Platz 1", 1, ort, true, false) };
        (await svc.RunAsync()).Updated.Should().Be(1);
        ctx.StorageLocations.Single().SageIstGesperrt.Should().BeTrue();
        ctx.StorageLocations.Single().ModifiedBy.Should().Be("system:sync");
    }

    [Fact]
    public async Task Abgleich_SData_LagerortFehlt_SageMissingSince_KommtZurueck_Geleert()
    {
        var (svc, _, _, sdata, ctx, _, _) = Abgleich("SData");
        sdata.Records = new()
        {
            new("HR", "P1", "Platz 1", 1, new("HR", "Hochregal", null, false)),
            new("ALT", "A1", "Alt 1", 2, new("ALT", "Altlager", null, false)),
        };
        await svc.RunAsync();

        sdata.Records = new() { new("HR", "P1", "Platz 1", 1, new("HR", "Hochregal", null, false)) }; // ALT in Sage inaktiv
        var r = await svc.RunAsync();

        r.Deactivated.Should().Be(1, "Platz des inaktiven Lagerorts wird wie heute deaktiviert");
        var alt = ctx.StorageAreas.Single(a => a.Lagerkennung == "ALT");
        alt.SageMissingSince.Should().NotBeNull();
        alt.IsActive.Should().BeFalse();
        ctx.StorageAreas.Count().Should().Be(2, "Lagerorte werden nie geloescht");

        sdata.Records.Add(new("ALT", "A1", "Alt 1", 2, new("ALT", "Altlager", null, false)));
        await svc.RunAsync();
        alt.SageMissingSince.Should().BeNull();
        alt.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Abgleich_ManuellerPlatzGleicherCode_BleibtKonflikt_OhneLagerort()
    {
        var (svc, _, _, sdata, ctx, _, _) = Abgleich("SData");
        ctx.StorageLocations.Add(new StorageLocation { Code = "P1", Source = StorageLocationSource.Manual, CreatedBy = "x", CreatedByWindows = "x" });
        await ctx.SaveChangesAsync();
        sdata.Records = new() { new("HR", "P1", "Platz 1", 1, new("HR", "Hochregal", null, false), true) };

        (await svc.RunAsync()).Conflicts.Should().Be(1);

        var p = ctx.StorageLocations.Single();
        (p.StorageAreaId, p.SageIstGesperrt).Should().Be(((int?)null, (bool?)null));
    }

    [Theory]
    [InlineData("SData", 1)]
    [InlineData("View", 0)]
    public async Task Abgleich_LeseFehler_FehlermailNurBeiSData_G10(string schalter, int mails)
    {
        var (svc, _, view, sdata, ctx, logger, notifier) = Abgleich(schalter);
        ctx.StorageLocations.Add(new StorageLocation { Code = "P9", Source = StorageLocationSource.Sage, IsActive = true, CreatedBy = "x", CreatedByWindows = "x" });
        await ctx.SaveChangesAsync();
        view.ThrowOnRead = new InvalidOperationException("SQL weg");
        sdata.ThrowOnRead = new SdataLeseException("LagerplaetzeInfo", 0, "nicht erreichbar");

        var r = await svc.RunAsync();

        r.Errors.Should().Be(1);
        ctx.StorageLocations.Single().IsActive.Should().BeTrue("Lesefehler deaktiviert nichts");
        logger.Runs.Single(x => x.ServiceName == SyncLogServices.Lagerplatz).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync("Lagerplatz-Sync", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Exactly(mails));
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_LagerplatzReader_IstWeiche_MitBeidenReadern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddSageSdataReadClient();
        services.AddLagerplatzQuelle();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageLagerplatzReader>().Should().BeOfType<LagerplatzQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageLagerplatzReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataLagerplatzReader>().Should().NotBeNull();
    }
}
