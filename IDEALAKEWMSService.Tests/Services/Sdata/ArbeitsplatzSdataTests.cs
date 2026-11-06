using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E6a/G12: SData-Arbeitsplatz-Reader, Weiche, beide Abgleiche ueber die Weiche,
/// Nicht-null-Regel, Kuerzel nicht geliefert (P-A1), DI.</summary>
public class ArbeitsplatzSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static Mock<ISageSdataReadClient> Client(params SdataArbeitsplatzZeile[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataArbeitsplatzZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataArbeitsplatzZeile>(zeilen, SdataArbeitsplatzReader.Ressource, 1, zeilen.Length, 1));
        return client;
    }

    // ----- Reader-Mapping (Fake-Client) -----

    [Fact]
    public async Task Reader_NurAktive_ZusatzfelderGetrimmt_OhneKuerzel_UndAbfrage()
    {
        var client = Client(
            new SdataArbeitsplatzZeile("9999", "Testplatz", "SP1 ", " 4711", true, "T01 "),
            new SdataArbeitsplatzZeile("8888", "Inaktiv", null, null, false, null),
            new SdataArbeitsplatzZeile("7777", "Ohne Aktiv", null, null, null, null));
        var reader = new SdataArbeitsplatzReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataArbeitsplatzReader>.Instance);

        var r = await reader.LesenAsync(Ziel, CancellationToken.None);

        r.SourceExists.Should().BeTrue();
        r.ArbeitsschrittGeliefert.Should().BeFalse("P-A1: USER_ArbeitsSchritt ist nicht im SData-Schema");
        r.Rows.Should().ContainSingle().Which.Should().Be(new SageArbeitsplatzDto("9999", "Testplatz", null, "T01", "4711", "SP1"));
        client.Verify(c => c.QueryAllAsync<SdataArbeitsplatzZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsArbeitsplaetzeInfo", null,
            SdataArbeitsplatzReader.Select, "Arbeitsplatznummer", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        SdataArbeitsplatzReader.Select.Should().NotContain("USER_", "ein Feld ausserhalb des Schemas bricht die ganze Abfrage (HTTP 500)");
    }

    [Fact]
    public async Task Reader_SDataFehler_Wirft_KeineLeereListe()
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataArbeitsplatzZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("PpsArbeitsplaetzeInfo", 500, "kaputt"));
        var reader = new SdataArbeitsplatzReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataArbeitsplatzReader>.Instance);

        await ((Func<Task>)(() => reader.LesenAsync(Ziel, CancellationToken.None))).Should().ThrowAsync<SdataLeseException>();
    }

    // ----- Weiche -----

    private sealed class FakeReader : ISageArbeitsplatzReader
    {
        public SageArbeitsplatzReadResult Result { get; set; } = new(true, new());
        public Exception? ThrowOnRead { get; set; }
        public List<(string Source, int Mandant)> Calls { get; } = new();

        public Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default)
        {
            Calls.Add((quotedSource, mandant));
            if (ThrowOnRead != null) throw ThrowOnRead;
            return Task.FromResult(Result);
        }
    }

    private sealed class TestSwitch : ArbeitsplatzQuelleSwitch
    {
        public string? Schalter { get; set; }
        public TestSwitch(ISageArbeitsplatzReader view, ISageArbeitsplatzReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
    }

    private static (TestSwitch Switch, FakeReader View, FakeReader Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Weiche(string? schalter)
    {
        var view = new FakeReader { Result = new(true, new() { new("2300", "Kanterei W1", "KA"), new("2400", "Schweissen", "SW") }) };
        var sdata = new FakeReader
        {
            Result = new(true, new() { new("2300", "Kanterei W1", null, "T01", "4711", "SP1"), new("2400 ", "Schweissen neu", null, "T02") },
                ArbeitsschrittGeliefert: false)
        };
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView_ReichtQuelleUndMandantDurch()
    {
        var (w, view, sdata, logger, _) = Weiche(null);
        sdata.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        var r = await w.GetActiveAsync("[KHKPpsArbeitsplaetze]", 3);

        r.Should().BeSameAs(view.Result, "View-Weg bit-gleich: das Ergebnis des SQL-Readers unveraendert");
        view.Calls.Should().Equal(("[KHKPpsArbeitsplaetze]", 3));
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_LiestNurSData_FehlerWirftDurch()
    {
        var (w, view, sdata, _, notifier) = Weiche("SData");
        view.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        (await w.GetActiveAsync("[x]", 1)).ArbeitsschrittGeliefert.Should().BeFalse();

        sdata.ThrowOnRead = new SdataLeseException("PpsArbeitsplaetzeInfo", 500, "kaputt");
        await ((Func<Task>)(() => w.GetActiveAsync("[x]", 1))).Should().ThrowAsync<SdataLeseException>();
        notifier.VerifyNoOtherCalls(); // Mail schickt RunResilientAsync des Abgleichs, nicht die Weiche
    }

    [Fact]
    public async Task Weiche_Vergleich_ViewZeilen_AbweichungJeNummer_KuerzelNichtVerglichen()
    {
        var (w, view, _, logger, _) = Weiche("Vergleich");

        (await w.GetActiveAsync("[x]", 1)).Should().BeSameAs(view.Result, "geschrieben wird nur aus der View");

        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1, "2300 gleich (Kuerzel wird bis P-A1 nicht verglichen), 2400 Name abweichend");
        lauf.Events.Should().Contain(e => e.Message.Contains("[2400] Name") && e.Message.Contains("'Schweissen neu'"));
    }

    // ----- Werkbank-Anlage ueber die Weiche -----

    private static ProductionWorkplace Werkbank(string nummer, string name, string code) => new()
    {
        SageArbeitsplatznummer = nummer, Name = name, ArbeitsschrittCode = code,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    private static ProductionWorkplaceSyncService Werkbankabgleich(IdealAkeWms.Data.ApplicationDbContext ctx, ISageArbeitsplatzReader reader, FakeSyncLogger log) =>
        new(ctx, new ConfigurationBuilder().Build(), new Mock<IMailService>().Object, reader, new UnknownArbeitsplatzState(),
            NullLogger<ProductionWorkplaceSyncService>.Instance, log);

    [Fact]
    public async Task Werkbank_SData_SchreibtZusatzfelder_KeineAnlage_KeinKuerzel_ViewLaufLaesstSieStehen()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.Add(Werkbank("2300", "Kanterei W1", "KA"));
        await ctx.SaveChangesAsync();
        var (w, view, sdata, logger, _) = Weiche("SData");
        sdata.Result = new(true, new() { new("2300", "Kanterei neu", null, "T01", "4711", "SP1"), new("5000", "Neu in Sage", null, "T09") },
            ArbeitsschrittGeliefert: false);
        var svc = Werkbankabgleich(ctx, w, logger);

        var r = await svc.RunAsync(dryRun: false);

        r.Inserted.Should().Be(0, "ohne Kuerzel keine Anlage (Pflicht-Filter wie heute)");
        var wp = ctx.ProductionWorkplaces.Single();
        (wp.Name, wp.ArbeitsschrittCode).Should().Be(("Kanterei W1", "KA"), "Kuerzel null = nicht schreiben, Name-Abgleich haengt am Kuerzel");
        (wp.SageBdeTerminalId, wp.SageKostenstelle, wp.SageSchichtplannummer).Should().Be(("T01", "4711", "SP1"));
        (wp.ModifiedBy, wp.SageMissingSince).Should().Be(("ProductionWorkplaceSync", (DateTime?)null));
        var lauf = logger.Runs.Single(x => x.ServiceName == SyncLogServices.ProductionWorkplaceSync);
        lauf.Events.Should().Contain(("Info", "Quelle: SData", null));
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("P-A1"));
        lauf.Events.Should().Contain(e => e.Message.StartsWith("Sage-Zusatzfelder") && e.Message.EndsWith(": 1"));
        lauf.FinalCounts!.Keys.Should().BeEquivalentTo(new[] { "neu", "aktualisiert", "abweichung", "uebersprungen", "problematisch",
            "nicht_in_sage", "nicht_in_sage_neu", "wieder_in_sage" }, "Count-Keys unveraendert (AK 1)");

        // Rueckfall View: Zusatzfelder bleiben (nicht genullt), Kuerzel-Abgleich wie heute
        w.Schalter = "View";
        view.Result = new(true, new() { new("2300", "Kanterei W1", "KA") });
        await svc.RunAsync(dryRun: false);
        (wp.SageBdeTerminalId, wp.SageKostenstelle, wp.SageSchichtplannummer).Should().Be(("T01", "4711", "SP1"));
        logger.Runs.Last(x => x.ServiceName == SyncLogServices.ProductionWorkplaceSync).Events
            .Should().Contain(("Info", "Quelle: View", null)).And.NotContain(e => e.Message.Contains("P-A1"));
    }

    [Fact]
    public async Task Werkbank_NichtNullRegel_NullUeberschreibtNicht_GleicheWerte_KeineAenderung_DryRunSchreibtNicht()
    {
        using var ctx = TestDbContextFactory.Create();
        var wp = Werkbank("2300", "Kanterei W1", "KA");
        wp.SageBdeTerminalId = "T01"; wp.SageKostenstelle = "4711"; wp.SageSchichtplannummer = "SP1";
        ctx.ProductionWorkplaces.Add(wp);
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();
        var reader = new FakeReader { Result = new(true, new() { new("2300", "Kanterei W1", "KA", "T01", null, null) }) };
        var svc = Werkbankabgleich(ctx, reader, log);

        await svc.RunAsync(dryRun: false);
        (wp.SageBdeTerminalId, wp.SageKostenstelle, wp.SageSchichtplannummer, wp.ModifiedAt).Should().Be(("T01", "4711", "SP1", (DateTime?)null));

        reader.Result = new(true, new() { new("2300", "Kanterei W1", "KA", "T02") });
        await svc.RunAsync(dryRun: true);
        wp.SageBdeTerminalId.Should().Be("T01", "DryRun schreibt nichts");
        log.Runs.Last().Events.Should().Contain(e => e.Message.StartsWith("Sage-Zusatzfelder") && e.Message.Contains("[DryRun]"));

        await svc.RunAsync(dryRun: false);
        (wp.SageBdeTerminalId, wp.SageKostenstelle).Should().Be(("T02", "4711"));
    }

    [Fact]
    public async Task Werkbank_Neuanlage_UebernimmtZusatzfelder_WennKuerzelGeliefert()
    {
        using var ctx = TestDbContextFactory.Create();
        var reader = new FakeReader { Result = new(true, new() { new("2300", "Kanterei W1", "KA", "T01", "4711", "SP1") }) };

        await Werkbankabgleich(ctx, reader, new FakeSyncLogger()).RunAsync(dryRun: false);

        var wp = await ctx.ProductionWorkplaces.SingleAsync();
        (wp.SageBdeTerminalId, wp.SageKostenstelle, wp.SageSchichtplannummer).Should().Be(("T01", "4711", "SP1"));
    }

    [Fact]
    public async Task Werkbank_SData_FehltInSage_WirdGekennzeichnet_WieHeute()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.ProductionWorkplaces.AddRange(Werkbank("2300", "Kanterei W1", "KA"), Werkbank("2400", "Schweissen", "SW"));
        await ctx.SaveChangesAsync();
        var (w, _, sdata, logger, _) = Weiche("SData");
        sdata.Result = new(true, new() { new("2300", "Kanterei W1", null) }, ArbeitsschrittGeliefert: false);

        await Werkbankabgleich(ctx, w, logger).RunAsync(dryRun: false);

        ctx.ProductionWorkplaces.Single(x => x.SageArbeitsplatznummer == "2400").SageMissingSince.Should().NotBeNull();
        ctx.ProductionWorkplaces.Single(x => x.SageArbeitsplatznummer == "2300").SageMissingSince.Should().BeNull();
    }

    // ----- Arbeitsgang-Abgleich ueber dieselbe Weiche -----

    private static OperationConfigSyncService Kuerzelabgleich(IdealAkeWms.Data.ApplicationDbContext ctx, ISageArbeitsplatzReader reader, FakeSyncLogger log) =>
        new(ctx, new ConfigurationBuilder().Build(), reader, NullLogger<OperationConfigSyncService>.Instance, log);

    [Fact]
    public async Task Kuerzel_SData_OhneKuerzel_KeineAnlage_KeineKennzeichnung_Warnung()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.OperationConfigs.Add(new OperationConfig
        {
            OperationName = "KA", Source = OperationConfigSource.Sage, IsExternalSystemRelevant = false,
            CreatedAt = DateTime.Now, CreatedBy = "x", CreatedByWindows = "x",
        });
        await ctx.SaveChangesAsync();
        var (w, _, _, logger, _) = Weiche("SData");

        var r = await Kuerzelabgleich(ctx, w, logger).RunAsync(dryRun: false);

        r.ErrorDetails.Should().Be("Kuerzel nicht geliefert.");
        ctx.OperationConfigs.Single().SageMissingSince.Should().BeNull("sonst waere jedes Kuerzel 'nicht in Sage'");
        var lauf = logger.Runs.Single(x => x.ServiceName == SyncLogServices.OperationConfigSync);
        lauf.FinishedSuccess.Should().BeTrue();
        lauf.FinalMessageSuffix.Should().Be("Kuerzel nicht geliefert");
        lauf.Events.Should().Contain(("Info", "Quelle: SData", null));
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("P-A1"));
    }

    [Fact]
    public async Task Kuerzel_View_UeberWeiche_WieHeute()
    {
        using var ctx = TestDbContextFactory.Create();
        var (w, _, sdata, logger, _) = Weiche("View");
        sdata.ThrowOnRead = new InvalidOperationException("darf nicht gelesen werden");

        var r = await Kuerzelabgleich(ctx, w, logger).RunAsync(dryRun: false);

        r.Inserted.Should().Be(2);
        ctx.OperationConfigs.Select(c => c.OperationName).Should().BeEquivalentTo("KA", "SW");
        logger.Runs.Single(x => x.ServiceName == SyncLogServices.OperationConfigSync).Events.Should().Contain(("Info", "Quelle: View", null));
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_ArbeitsplatzReader_IstWeiche_MitBeidenReadern_UndBeideAbgleicheLoesenAuf()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSingleton(new Mock<IMailService>().Object);
        services.AddSingleton<IUnknownArbeitsplatzState, UnknownArbeitsplatzState>();
        services.AddScoped<ProductionWorkplaceSyncService>();
        services.AddScoped<OperationConfigSyncService>();
        services.AddSageSdataReadClient();
        services.AddArbeitsplatzQuelle();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageArbeitsplatzReader>().Should().BeOfType<ArbeitsplatzQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageArbeitsplatzReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataArbeitsplatzReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ProductionWorkplaceSyncService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<OperationConfigSyncService>().Should().NotBeNull();
    }
}
