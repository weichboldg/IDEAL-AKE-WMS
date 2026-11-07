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

/// <summary>Spec 2026-11-08-wareneingang-aus-sage-bestellungen E1 (+ Entscheidung E0-Blocker), AK 1-5.</summary>
public class EinkaufBestellungenSyncTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static SdataEinkaufPositionZeile Pos(int belPosId, string artikel, string kto = "K31013", string kz = "EBB",
        decimal bestellt = 10m, decimal geliefert = 0m, string me = "Stk", string lme = "Stk", int? platz = 11, int sperre = 0,
        string? termin = "2026-11-20T00:00:00+01:00") =>
        new(belPosId / 10, belPosId, kz, $"2026-{belPosId / 10}", kto, "Lieferant " + kto, sperre, termin, null, artikel, "Bez", null,
            0, 0, me, me, lme, bestellt, geliefert, platz, "HR;0;0;0");

    private static (EinkaufBestellungenSyncService svc, Mock<ISageSdataReadClient> client) Svc(IdealAkeWms.Data.ApplicationDbContext ctx,
        FakeSyncLogger log, params SdataEinkaufPositionZeile[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(),
                EinkaufBestellungenSyncService.Ressource, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataEinkaufPositionZeile>(zeilen, EinkaufBestellungenSyncService.Ressource, 1, zeilen.Length, 1));
        return (new(ctx, client.Object, new ConfigurationBuilder().Build(), NullLogger<EinkaufBestellungenSyncService>.Instance, log), client);
    }

    private static async Task SeedStammAsync(IdealAkeWms.Data.ApplicationDbContext ctx)
    {
        ctx.Articles.Add(new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.Creditors.Add(new Creditor { Id = 3, Name = "Lieferant", SageKto = "K31013", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", SageLagerplatzId = 11, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Lauf_NurEbb_Zuordnung_Basiseinheit_EinheitAbweichend_Sperre_AK2_AK3()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log,
            Pos(101, "00722001", bestellt: 9000m, geliefert: 8698m),
            Pos(102, "FEHLT", kto: "K99999", platz: 77, me: "RLL", lme: "Stk", sperre: 1, termin: null),
            Pos(103, "00722001", kz: "EBZ"));

        await svc.RunAsync(dryRun: false, ziel: Ziel);

        client.Verify(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(),
            "EinkaufPositionenInfo", "Belegkennzeichen eq 'EBB'", It.IsAny<string?>(), "BelPosId", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        var zeilen = ctx.SagePurchaseOrderLines.OrderBy(z => z.BelPosId).ToList();
        zeilen.Select(z => z.BelPosId).Should().Equal(101, 102);
        var a = zeilen[0];
        (a.ArticleId, a.CreditorId, a.StorageLocationId, a.MengeOffen, a.EinheitAbweichend, a.LieferantGesperrt, a.Liefertermin)
            .Should().Be(((int?)5, (int?)3, (int?)9, 302m, false, false, (DateTime?)new DateTime(2026, 11, 20)));
        var b = zeilen[1];
        (b.ArticleId, b.CreditorId, b.StorageLocationId, b.EinheitAbweichend, b.LieferantGesperrt, b.Liefertermin)
            .Should().Be(((int?)null, (int?)null, (int?)null, true, true, (DateTime?)null));

        var lauf = log.Runs.Single(r => r.ServiceName == SyncLogServices.EinkaufBestellungen);
        lauf.FinalCounts!["uebernommen"].Should().Be(2);
        lauf.FinalCounts["belegart_ausgeschlossen"].Should().Be(1);
        (lauf.FinalCounts["artikel_fehlt"], lauf.FinalCounts["kreditor_fehlt"], lauf.FinalCounts["lagerplatz_fehlt"], lauf.FinalCounts["einheit_abweichend"])
            .Should().Be((1, 1, 1, 1));
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("FEHLT"))
            .And.Contain(e => e.Level == "Warning" && e.Message.Contains("K99999") && e.Message.Contains("Sync:KreditorenEnabled"))
            .And.Contain(e => e.Level == "Warning" && e.Message.Contains("ID 77"));
    }

    [Fact]
    public async Task Lauf_ErsetztCache_EmptyGuard_AK4()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        await Svc(ctx, new FakeSyncLogger(), Pos(101, "00722001"), Pos(102, "00722001")).svc.RunAsync(false, ziel: Ziel);
        await Svc(ctx, new FakeSyncLogger(), Pos(102, "00722001")).svc.RunAsync(false, ziel: Ziel);
        ctx.SagePurchaseOrderLines.Select(z => z.BelPosId).Should().Equal(102);

        var log = new FakeSyncLogger();
        await Svc(ctx, log).svc.RunAsync(false, ziel: Ziel);
        ctx.SagePurchaseOrderLines.Should().HaveCount(1, "0 Positionen bei nicht leerem Cache: unveraendert");
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard"));
    }

    [Fact]
    public async Task Lesefehler_LaufFehler_Wirft_CacheUnveraendert_AK4()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.SagePurchaseOrderLines.Add(new SagePurchaseOrderLine { BelPosId = 1, BelId = 1, Belegkennzeichen = "EBB", Belegnummer = "2026-1", Artikelnummer = "A" });
        await ctx.SaveChangesAsync();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ThrowsAsync(new SdataLeseException("EinkaufPositionenInfo", 500, "kaputt"));
        var log = new FakeSyncLogger();
        var svc = new EinkaufBestellungenSyncService(ctx, client.Object, new ConfigurationBuilder().Build(),
            NullLogger<EinkaufBestellungenSyncService>.Instance, log);

        await ((Func<Task>)(() => svc.RunAsync(false, ziel: Ziel))).Should().ThrowAsync<SdataLeseException>();
        log.Runs.Single().FinishedFailed.Should().BeTrue();
        ctx.SagePurchaseOrderLines.Should().HaveCount(1);
    }

    [Fact]
    public void Vertrag_NurBelegteFelder_NieAllInfo_AK5()
    {
        EinkaufBestellungenSyncService.Ressource.Should().Be("EinkaufPositionenInfo");
        EinkaufBestellungenSyncService.Select.Should().NotContain("AllInfo").And.NotContain("BelPosID").And.Contain("PositionGGBestellt");
        EinkaufBestellungenSyncService.Where.Should().NotContain(" ne ").And.NotContain(" gt ''");
    }

    [Fact]
    public async Task Gate_Aus_KeinLauf_AK1()
    {
        var provider = new Mock<IServiceProvider>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WorkerSettings:SyncIntervalMinutes"] = "0",
            ["Sync:EinkaufBestellungenEnabled"] = "true", // IConfiguration-Seed wirkt nicht (DB-first, Default false)
        }).Build();

        using var worker = new SyncWorker(Mock.Of<ILogger<SyncWorker>>(), config, factory.Object);
        // Never-Pruefung: ein zu frueher Stopp kann nur falsch-gruen, nie zufaellig rot sein (wie die Kontokorrent-Gate-Tests).
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await worker.StopAsync(CancellationToken.None);

        provider.Verify(p => p.GetService(typeof(EinkaufBestellungenSyncService)), Times.Never);
    }

    [Fact]
    public void DI_LoestAuf_LogNameRegistriert()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSageSdataReadClient();
        services.AddScoped<EinkaufBestellungenSyncService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var s = sp.CreateScope();

        s.ServiceProvider.GetRequiredService<EinkaufBestellungenSyncService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(SyncLogServices.EinkaufBestellungen);
        SyncLogServices.DisplayNames[SyncLogServices.EinkaufBestellungen].Should().Be("Offene Bestellungen (Sage)");
    }
}
