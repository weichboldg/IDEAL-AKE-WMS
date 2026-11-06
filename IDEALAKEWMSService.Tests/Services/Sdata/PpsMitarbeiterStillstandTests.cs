using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services.Sdata;
using IDEALAKEWMSService.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E6b (A11/A12, G14): PPS-Mitarbeiter-Cache, Stillstandsgruende, Empty-Guard,
/// keine automatische Verknuepfung, Gates aus = kein Lauf, DI.</summary>
public class PpsMitarbeiterStillstandTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static Mock<ISageSdataReadClient> Client<T>(string ressource, params T[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<T>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<T>(zeilen, ressource, 1, zeilen.Length, 1));
        return client;
    }

    private static SdataPpsMitarbeiterZeile Ma(string? nummer, string nachname = "Huber", string vorname = "Max") =>
        new(nummer, "MC " + nummer, vorname, nachname, "G1 ", "K-" + nummer, 77, true, "SP1", "4711", "akenb\\x");

    private static BdeOperator Op(string pnr, string nachname, string vorname, string? sageNr = null, bool aktiv = true) => new()
    {
        PersonnelNumber = pnr, LastName = nachname, FirstName = vorname, IsActive = aktiv, SageMitarbeiternummer = sageNr,
        CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t",
    };

    // ----- PPS-Mitarbeiter -----

    [Fact]
    public async Task Mitarbeiter_Mapping_Abfrage_Cache_KeineAnlage_KeineNamensVerknuepfung_Warnungen()
    {
        using var ctx = TestDbContextFactory.Create();
        // "Huber Max" heisst in Sage gleich, hat aber eine andere Personalnummer -> darf NICHT verknuepft werden.
        ctx.BdeOperators.AddRange(Op("100", "Huber", "Max"), Op("900", "Alt", "Zugeordnet", sageNr: "999"), Op("200", "Maier", "Anna", aktiv: false));
        ctx.SagePpsMitarbeiter.Add(new SagePpsMitarbeiter { Nummer = "ALT", SyncedAt = DateTime.Now.AddDays(-1) });
        await ctx.SaveChangesAsync();
        var client = Client(PpsMitarbeiterSyncService.Ressource, Ma(" 4711 "), Ma("4712", "Bauer", "Tom"));
        var log = new FakeSyncLogger();
        var svc = new PpsMitarbeiterSyncService(ctx, client.Object, new ConfigurationBuilder().Build(),
            NullLogger<PpsMitarbeiterSyncService>.Instance, log);

        await svc.RunAsync(dryRun: false, ziel: Ziel);

        client.Verify(c => c.QueryAllAsync<SdataPpsMitarbeiterZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsMitarbeiterInfo", null,
            PpsMitarbeiterSyncService.Select, "Nummer", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        PpsMitarbeiterSyncService.Select.Should().NotContain("USER_");
        ctx.SagePpsMitarbeiter.Select(m => m.Nummer).Should().BeEquivalentTo(new[] { "4711", "4712" }, "Vollersatz: ALT faellt aus dem Cache");
        var m = ctx.SagePpsMitarbeiter.Single(x => x.Nummer == "4711");
        (m.Gruppe, m.Kartennummer, m.ExterneNummer, m.Aktiv, m.UserName).Should().Be(("G1", "K- 4711", 77, (bool?)true, "akenb\\x"));
        ctx.BdeOperators.Should().HaveCount(3, "der Abgleich legt nie BDE-Mitarbeiter an");
        ctx.BdeOperators.Single(o => o.PersonnelNumber == "100").SageMitarbeiternummer.Should().BeNull("nie ueber den Namen verknuepfen");

        var lauf = log.Runs.Single(r => r.ServiceName == SyncLogServices.PpsMitarbeiter);
        lauf.FinishedSuccess.Should().BeTrue();
        lauf.FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["gelesen"] = 2, ["uebersprungen"] = 0, ["nicht_zugeordnet"] = 2, ["bde_ohne_sage_nummer"] = 1, ["sage_nummer_unbekannt"] = 1,
        });
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.StartsWith("1 aktive BDE-Mitarbeiter ohne Sage-Mitarbeiternummer")
            && e.Message.Contains("100 Huber Max") && !e.Message.Contains("Maier"));
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("999 → 900"));
    }

    [Fact]
    public async Task Mitarbeiter_EmptyGuard_LeereAntwort_LaesstCacheStehen_UndMeldet()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SagePpsMitarbeiter.Add(new SagePpsMitarbeiter { Nummer = "4711", SyncedAt = DateTime.Now });
        ctx.BdeOperators.Add(Op("100", "Huber", "Max", sageNr: "4711"));
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();
        var svc = new PpsMitarbeiterSyncService(ctx, Client<SdataPpsMitarbeiterZeile>(PpsMitarbeiterSyncService.Ressource).Object,
            new ConfigurationBuilder().Build(), NullLogger<PpsMitarbeiterSyncService>.Instance, log);

        await svc.RunAsync(dryRun: false, ziel: Ziel);

        ctx.SagePpsMitarbeiter.Should().ContainSingle(m => m.Nummer == "4711");
        var lauf = log.Runs.Single();
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard"));
        lauf.FinalCounts!["sage_nummer_unbekannt"].Should().Be(0, "geprueft gegen den unveraenderten Cache");
    }

    [Fact]
    public async Task Mitarbeiter_DublettenUndLeereNummer_Uebersprungen_DryRun_SchreibtNicht()
    {
        using var ctx = TestDbContextFactory.Create();
        var log = new FakeSyncLogger();
        var svc = new PpsMitarbeiterSyncService(ctx,
            Client(PpsMitarbeiterSyncService.Ressource, Ma("4711"), Ma("4711 ", "Doppelt"), Ma(" ")).Object,
            new ConfigurationBuilder().Build(), NullLogger<PpsMitarbeiterSyncService>.Instance, log);

        await svc.RunAsync(dryRun: true, ziel: Ziel);
        ctx.SagePpsMitarbeiter.Should().BeEmpty("DryRun schreibt nichts");
        log.Runs.Single().FinalCounts!["uebersprungen"].Should().Be(2);
        log.Runs.Single().Events.Should().Contain(e => e.Message.Contains("mehrfach") && e.Message.Contains("4711"));

        await svc.RunAsync(dryRun: false, ziel: Ziel);
        ctx.SagePpsMitarbeiter.Should().ContainSingle().Which.Nachname.Should().Be("Huber", "die erste Zeile gewinnt");
    }

    [Fact]
    public async Task Mitarbeiter_SDataFehler_LaufFehler_Wirft_CacheBleibt()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SagePpsMitarbeiter.Add(new SagePpsMitarbeiter { Nummer = "4711", SyncedAt = DateTime.Now });
        await ctx.SaveChangesAsync();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataPpsMitarbeiterZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("PpsMitarbeiterInfo", 500, "kaputt"));
        var log = new FakeSyncLogger();
        var svc = new PpsMitarbeiterSyncService(ctx, client.Object, new ConfigurationBuilder().Build(),
            NullLogger<PpsMitarbeiterSyncService>.Instance, log);

        await ((Func<Task>)(() => svc.RunAsync(false, ziel: Ziel))).Should().ThrowAsync<SdataLeseException>();
        log.Runs.Single().FinishedFailed.Should().BeTrue();
        ctx.SagePpsMitarbeiter.Should().ContainSingle();
    }

    // ----- Stillstandsgruende -----

    private static StillstandsgruendeSyncService Stillstand(IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger log, params SdataStillstandZeile[] zeilen) =>
        new(ctx, Client(StillstandsgruendeSyncService.Ressource, zeilen).Object, new ConfigurationBuilder().Build(),
            NullLogger<StillstandsgruendeSyncService>.Instance, log);

    [Fact]
    public async Task Stillstand_Anlegen_Aktualisieren_Kennzeichnen_Wiederfinden_Audit()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Stillstandsgruende.AddRange(
            new Stillstandsgrund { Nummer = "10", Bezeichnung = "Material fehlt", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Stillstandsgrund { Nummer = "20", Bezeichnung = "Wartung", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
            new Stillstandsgrund { Nummer = "30", Bezeichnung = "Alt", SageMissingSince = DateTime.Now.AddDays(-3), CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Stillstand(ctx, log, new("10", "Material fehlt"), new("30 ", null), new("40", " Störung ")).RunAsync(dryRun: false, ziel: Ziel);

        var g = ctx.Stillstandsgruende.ToDictionary(x => x.Nummer);
        g.Keys.Should().BeEquivalentTo("10", "20", "30", "40");
        (g["40"].Bezeichnung, g["40"].CreatedBy).Should().Be(("Störung", "StillstandsgruendeSync"));
        g["10"].ModifiedAt.Should().BeNull("unveraendert");
        (g["20"].SageMissingSince != null, g["20"].ModifiedBy).Should().Be((true, "StillstandsgruendeSync"));
        (g["30"].SageMissingSince, g["30"].Bezeichnung, g["30"].ModifiedByWindows).Should().Be(((DateTime?)null, "Alt", "StillstandsgruendeSync"),
            "wieder da; Bezeichnung null = nicht schreiben (F1)");
        log.Runs.Single(r => r.ServiceName == SyncLogServices.Stillstandsgruende).FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["gelesen"] = 3, ["neu"] = 1, ["aktualisiert"] = 0, ["unveraendert"] = 1, ["uebersprungen"] = 0,
            ["nicht_in_sage"] = 1, ["nicht_in_sage_neu"] = 1, ["wieder_in_sage"] = 1,
        });
    }

    [Fact]
    public async Task Stillstand_EmptyGuard_KennzeichnetNichts()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Stillstandsgruende.Add(new Stillstandsgrund { Nummer = "10", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();

        await Stillstand(ctx, log).RunAsync(dryRun: false, ziel: Ziel);

        ctx.Stillstandsgruende.Single().SageMissingSince.Should().BeNull();
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard"));
        StillstandsgruendeSyncService.Select.Should().Be("Stillstandnummer,Bezeichnung", "Aktiv steht nicht im Schema (P-S1)");
    }

    // ----- Gates + DI -----

    [Fact]
    public async Task SyncWorker_GatesAus_OhneDb_KeinLauf()
    {
        // Default false + keine DB erreichbar -> Safe-Reader liefert false -> die Services werden nie aufgeloest.
        var provider = new Mock<IServiceProvider>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["Sync:PpsMitarbeiterEnabled"] = "true", // IConfiguration-Seed wirkt nicht (DB-first)
            ["Sync:StillstandsgruendeEnabled"] = "true",
        }).Build();

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, factory.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await worker.StartAsync(cts.Token);
        await Task.Delay(250);
        await worker.StopAsync(CancellationToken.None);

        provider.Verify(p => p.GetService(typeof(PpsMitarbeiterSyncService)), Times.Never);
        provider.Verify(p => p.GetService(typeof(StillstandsgruendeSyncService)), Times.Never);
    }

    [Fact]
    public void DI_BeideAbgleicheLoesenAuf()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSageSdataReadClient();
        services.AddScoped<PpsMitarbeiterSyncService>();
        services.AddScoped<StillstandsgruendeSyncService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<PpsMitarbeiterSyncService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<StillstandsgruendeSyncService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(new[] { SyncLogServices.PpsMitarbeiter, SyncLogServices.Stillstandsgruende });
        SyncLogServices.DisplayNames.Should().ContainKeys(SyncLogServices.PpsMitarbeiter, SyncLogServices.Stillstandsgruende);
    }
}
