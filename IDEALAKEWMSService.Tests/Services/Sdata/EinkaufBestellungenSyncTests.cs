using System.Text.Json;
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
        string? termin = "2026-11-20T00:00:00+01:00", string? basis = null, decimal? mengeBasis = null) =>
        new(belPosId / 10, belPosId, kz, $"2026-{belPosId / 10}", kto, "Lieferant " + kto, sperre, termin, null, artikel, "Bez", null,
            0, 0, me, basis ?? me, lme, bestellt, geliefert, platz, "HR;0;0;0", mengeBasis);

    /// <summary>v1.99.15: Der Fake antwortet je where wie Sage (eq 'EBB' / eq 'EBZ', ohne Gross/Klein und Leerzeichen); Zeilen fremder
    /// Belegart liefert er in der EBB-Antwort mit (prueft die doppelte Absicherung).</summary>
    private static List<SdataEinkaufPositionZeile> Antwort(string? where, SdataEinkaufPositionZeile[] zeilen) =>
        zeilen.Where(z => z.Belegkennzeichen!.Trim().ToUpperInvariant() is var kz && kz is "EBB" or "EBZ"
                ? where == EinkaufBestellungenSyncService.Where(kz)
                : where == EinkaufBestellungenSyncService.Where("EBB")).ToList();

    private static (EinkaufBestellungenSyncService svc, Mock<ISageSdataReadClient> client) Svc(IdealAkeWms.Data.ApplicationDbContext ctx,
        FakeSyncLogger log, params SdataEinkaufPositionZeile[] zeilen)
    {
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(),
                EinkaufBestellungenSyncService.Ressource, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync((SdataLeseZiel _, string _, string _, string? where, string? _, string _, CancellationToken _, int _) =>
            {
                var a = Antwort(where, zeilen);
                return new SdataSeiten<SdataEinkaufPositionZeile>(a, EinkaufBestellungenSyncService.Ressource, 1, a.Count, 1);
            });
        Koepfe(client);   // v1.99.33 Teil B: Standard = Kopf-Lesen erfolgreich, ohne Zeilen
        return (new(ctx, client.Object, new ConfigurationBuilder().Build(), NullLogger<EinkaufBestellungenSyncService>.Instance, log), client);
    }

    /// <summary>v1.99.33 Teil B: RealTimeData-Antwort fuer <see cref="EinkaufBestellungenSyncService.KopfQuelle"/> (Feldnamen wie P-1 am AKE-Test,
    /// fallstricke §41). <paramref name="status"/> != 200 = Lesefehler.</summary>
    private static void Koepfe(Mock<ISageSdataReadClient> client, int status = 200, params string[] json) =>
        client.Setup(c => c.GetAsync<Dictionary<string, JsonElement>>(It.IsAny<SdataLeseZiel>(), EinkaufBestellungenSyncService.KopfQuelle,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new SdataErgebnis<Dictionary<string, JsonElement>>(
                status == 200 ? json.Select(j => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(j)!).ToList() : [],
                new SdataCall(EinkaufBestellungenSyncService.KopfQuelle, "u", status, 1, json.Length, status == 200 ? null : $"HTTP {status} Internal Server Error", null)));

    private static async Task SeedStammAsync(IdealAkeWms.Data.ApplicationDbContext ctx)
    {
        ctx.Articles.Add(new Article { Id = 5, ArticleNumber = "00722001", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.Creditors.Add(new Creditor { Id = 3, Name = "Lieferant", SageKto = "K31013", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        ctx.StorageLocations.Add(new StorageLocation { Id = 9, Code = "HR 0 0 0", SageLagerplatzId = 11, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Lauf_EbbUndEbz_Zuordnung_Basiseinheit_EinheitAbweichend_Sperre_AK2_AK3()
    {
        // v1.99.15 (Teil 3, E1) BEWUSST GEAENDERT: bis v1.99.14 erwartete dieser Test, dass EBZ (103) NICHT uebernommen und nur EBB gelesen
        // wird. Jetzt: zwei Abfragen (EBB, EBZ), EBZ uebernommen; eine fremde Belegart (ERK) faengt die doppelte Absicherung ab.
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log,
            Pos(101, "00722001", bestellt: 9000m, geliefert: 8698m),
            Pos(102, "FEHLT", kto: "K99999", platz: 77, me: "RLL", lme: "Stk", sperre: 1, termin: null),
            Pos(103, "00722001", kz: "EBZ", bestellt: 34800m, geliefert: 33604m, mengeBasis: 2400m),
            Pos(104, "00722001", kz: "ERK"));

        await svc.RunAsync(dryRun: false, ziel: Ziel);

        foreach (var where in new[] { "Belegkennzeichen eq 'EBB'", "Belegkennzeichen eq 'EBZ'" })
            client.Verify(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(),
                "EinkaufPositionenInfo", where, It.IsAny<string?>(), "BelPosId", It.IsAny<CancellationToken>(), It.IsAny<int>()), Times.Once);
        // v1.99.33 BEWUSST GEAENDERT: bis v1.99.32 HaveCount(2) ueber alle Aufrufe; seit Teil B kommt der Kopf-Lesevorgang (GetAsync,
        // RealTimeData) hinzu - die Aussage „zwei Positions-Abfragen, kein or“ gilt weiter fuer QueryAllAsync.
        client.Invocations.Count(i => i.Method.Name == nameof(ISageSdataReadClient.QueryAllAsync)).Should().Be(2, "genau zwei Abfragen, kein or");
        var zeilen = ctx.SagePurchaseOrderLines.OrderBy(z => z.BelPosId).ToList();
        zeilen.Select(z => z.BelPosId).Should().Equal(101, 102, 103);
        var abruf = zeilen[2];
        (abruf.Belegkennzeichen, abruf.MengeBestellt, abruf.MengeGeliefert, abruf.MengeOffen, abruf.IstRahmenabruf)
            .Should().Be(("EBZ", 2400m, 0m, 2400m, true), "EBZ: Abrufmenge aus MengeBasis, PositionGG* (Rahmen) wirken nicht");
        (zeilen[0].Belegkennzeichen, zeilen[0].IstRahmenabruf).Should().Be(("EBB", false));
        var a = zeilen[0];
        (a.ArticleId, a.CreditorId, a.StorageLocationId, a.MengeOffen, a.EinheitAbweichend, a.LieferantGesperrt, a.Liefertermin)
            .Should().Be(((int?)5, (int?)3, (int?)9, 302m, false, false, (DateTime?)new DateTime(2026, 11, 20)));
        var b = zeilen[1];
        (b.ArticleId, b.CreditorId, b.StorageLocationId, b.EinheitAbweichend, b.LieferantGesperrt, b.Liefertermin)
            .Should().Be(((int?)null, (int?)null, (int?)null, true, true, (DateTime?)null));

        var lauf = log.Runs.Single(r => r.ServiceName == SyncLogServices.EinkaufBestellungen);
        lauf.FinalCounts!["uebernommen"].Should().Be(3);
        lauf.FinalCounts["belegart_ausgeschlossen"].Should().Be(1);
        (lauf.FinalCounts["positionen"], lauf.FinalCounts["positionen_ebb"], lauf.FinalCounts["positionen_ebz"]).Should().Be((4, 3, 1));
        (lauf.FinalCounts["artikel_fehlt"], lauf.FinalCounts["kreditor_fehlt"], lauf.FinalCounts["lagerplatz_fehlt"], lauf.FinalCounts["einheit_abweichend"])
            .Should().Be((1, 1, 1, 1));
        lauf.FinalCounts["menge_fehlt"].Should().Be(0);
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("FEHLT"))
            .And.Contain(e => e.Level == "Warning" && e.Message.Contains("K99999") && e.Message.Contains("Sync:KreditorenEnabled"))
            .And.Contain(e => e.Level == "Warning" && e.Message.Contains("ID 77"));
    }

    [Theory]
    [InlineData("PKG", "PKG", "PKG", false)] // EK023579: alles PKG -> buchbar
    [InlineData("PKG", "Stk", "PKG", true)]  // EK003604: Basis weicht ab -> gesperrt (v1.86.0, gebucht wird in der Basiseinheit)
    [InlineData("PKG", "Stk", "Stk", true)]  // Bestelleinheit weicht ab (E0 B-2)
    public async Task EinheitAbweichend_BestellOderBasisUngleichLager_Nachbesserung_v1_86(string me, string basis, string lme, bool gesperrt)
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        await Svc(ctx, new FakeSyncLogger(), Pos(101, "00722001", me: me, basis: basis, lme: lme)).svc.RunAsync(false, ziel: Ziel);
        ctx.SagePurchaseOrderLines.Single().EinheitAbweichend.Should().Be(gesperrt);
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
        EinkaufBestellungenSyncService.Select.Should().NotContain("AllInfo").And.NotContain("BelPosID").And.Contain("PositionGGBestellt")
            .And.Contain("MengeBasis");
        EinkaufBestellungenSyncService.Belegarten.Should().Equal("EBB", "EBZ");
        foreach (var art in EinkaufBestellungenSyncService.Belegarten)
            EinkaufBestellungenSyncService.Where(art).Should().Be($"Belegkennzeichen eq '{art}'").And.NotContain(" ne ").And.NotContain(" or ");
    }

    [Fact]
    public async Task Ebz_OhneMengeBasis_MengeFehlt_Warnung_Rest0()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        await Svc(ctx, log, Pos(101, "00722001"), Pos(103, "00722001", kz: "ebz ", mengeBasis: null)).svc.RunAsync(false, ziel: Ziel);
        var abruf = ctx.SagePurchaseOrderLines.Single(z => z.BelPosId == 103);
        (abruf.Belegkennzeichen, abruf.MengeBestellt, abruf.MengeOffen).Should().Be(("EBZ", 0m, 0m), "gelesener Wert getrimmt und gross; ohne MengeBasis nie buchbar");
        log.Runs.Single().FinalCounts!["menge_fehlt"].Should().Be(1);
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("MengeBasis"));
    }

    [Theory]
    [InlineData("EBB")]
    [InlineData("EBZ")]
    public async Task EmptyGuard_JeBelegart_CacheUnveraendert_AK4(string leer)
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        await Svc(ctx, new FakeSyncLogger(), Pos(101, "00722001"), Pos(103, "00722001", kz: "EBZ", mengeBasis: 5m)).svc.RunAsync(false, ziel: Ziel);

        var log = new FakeSyncLogger();
        var andere = leer == "EBB" ? Pos(113, "00722001", kz: "EBZ", mengeBasis: 7m) : Pos(111, "00722001");
        await Svc(ctx, log, andere).svc.RunAsync(false, ziel: Ziel);

        ctx.SagePurchaseOrderLines.Select(z => z.BelPosId).OrderBy(x => x).Should().Equal(new[] { 101, 103 }, "ganzer Cache bleibt stehen");
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Empty-Guard")
            && e.Message.Contains($"Belegkennzeichen eq '{leer}'") && e.Message.Contains("Cache hat 1"));
    }

    [Fact]
    public async Task EmptyGuard_BelegartOhneCacheZeilen_KeinGuard()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        await Svc(ctx, new FakeSyncLogger(), Pos(101, "00722001")).svc.RunAsync(false, ziel: Ziel);   // erster Lauf nach Deploy: Cache nur EBB, EBZ 0
        ctx.SagePurchaseOrderLines.Should().HaveCount(1);
        await Svc(ctx, new FakeSyncLogger(), Pos(101, "00722001"), Pos(103, "00722001", kz: "EBZ", mengeBasis: 5m)).svc.RunAsync(false, ziel: Ziel);
        ctx.SagePurchaseOrderLines.Should().HaveCount(2);
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

    // ----- v1.99.31 (Spec 2026-11-16-offene-bestellungen-sofort-neu-lesen F1) -----

    [Fact]
    public async Task Anstoss_SchreibtAnlass_UndSperreBelegt_LiefertFalseOhneLauf_F1_6_F1_7()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, _) = Svc(ctx, log, Pos(101, "00722001"));

        (await svc.TryRunExclusiveAsync(false, "Sage-Wareneingang (1 Beleg(e) übergeben)", ziel: Ziel)).Should().BeTrue();
        log.Runs.Single().Events.First().Should().Be(("Info", "Anlass: Sage-Wareneingang (1 Beleg(e) übergeben)", (string?)null));
        log.Runs.Single().FinishedSuccess.Should().BeTrue();

        // Ein Lauf haelt die Sperre (Client blockiert) -> der Anstoss liest nicht und meldet false; der regulaere Lauf wartet.
        using var ctx2 = TestDbContextFactory.Create();
        var halten = new TaskCompletionSource();
        var blockClient = new Mock<ISageSdataReadClient>();
        blockClient.Setup(c => c.QueryAllAsync<SdataEinkaufPositionZeile>(It.IsAny<SdataLeseZiel>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .Returns(async () => { await halten.Task; return new SdataSeiten<SdataEinkaufPositionZeile>(new List<SdataEinkaufPositionZeile>(), EinkaufBestellungenSyncService.Ressource, 0, 0, 1); });
        var regulaer = new EinkaufBestellungenSyncService(ctx2, blockClient.Object, new ConfigurationBuilder().Build(),
            NullLogger<EinkaufBestellungenSyncService>.Instance, new FakeSyncLogger()).RunAsync(false, ziel: Ziel);
        var log3 = new FakeSyncLogger();
        var (svc3, client3) = Svc(ctx, log3, Pos(101, "00722001"));
        try
        {
            (await svc3.TryRunExclusiveAsync(false, "x", ziel: Ziel)).Should().BeFalse();
            log3.Runs.Should().BeEmpty();
            client3.Invocations.Should().BeEmpty();
        }
        finally
        {
            halten.SetResult();
            await regulaer;
        }
        (await svc3.TryRunExclusiveAsync(false, "x", ziel: Ziel)).Should().BeTrue("nach dem Ende ist die Sperre frei");
    }

    // ---- v1.99.33 Teil B (Spec 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf, AK 16-18, 20) ----

    [Fact]
    public async Task Kopf_ZuordnungUeberBelId_WhereNurBelId_Counts_AK20()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log, Pos(101, "00722001"), Pos(102, "00722001"), Pos(111, "00722001"), Pos(121, "00722001"));
        Koepfe(client, 200,
            """{"BelID":10,"KHKEKBelege_USER_AB":" AB-4711 ","KHKEKBelege_USER_Kom":"K-12"}""",
            """{"BelID":11,"KHKEKBelege_USER_AB":"","KHKEKBelege_USER_Kom":null}""");

        await svc.RunAsync(false, ziel: Ziel);

        client.Verify(c => c.GetAsync<Dictionary<string, JsonElement>>(It.IsAny<SdataLeseZiel>(), "dtsEKBelegMst.Sage.Wawi",
            "BelID eq 10 or BelID eq 11 or BelID eq 12", "BelID,KHKEKBelege_USER_AB,KHKEKBelege_USER_Kom", "BelID", It.IsAny<CancellationToken>()), Times.Once);
        var z = ctx.SagePurchaseOrderLines.OrderBy(l => l.BelPosId).ToList();
        z.Select(l => (l.AbNummer, l.Kommission)).Should().Equal(("AB-4711", "K-12"), ("AB-4711", "K-12"), (null, null), (null, null));
        var c = log.Runs.Single().FinalCounts!;
        (c["ab_nummer"], c["kommission"], c["kopf_ohne_treffer"], c["kopf_zu_lang"]).Should().Be((1, 1, 1, 0));
        log.Runs.Single().Events.Should().NotContain(e => e.Message.Contains("liefert KHKEKBelege"))
            .And.Contain(e => e.Level == "Warning" && e.Message.Contains("ohne Kopf") && e.Message.Contains("12"));
    }

    [Fact]
    public async Task Kopf_FeldFehltInJederZeile_Warnung_WerteLeer_LaufErfolgreich_AK16()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log, Pos(101, "00722001"));
        Koepfe(client, 200, """{"BelID":10,"KHKEKBelege_USER_Kom":"K-12"}""");   // P-1b: falscher Name kommt still nicht

        await svc.RunAsync(false, ziel: Ziel);

        var l = ctx.SagePurchaseOrderLines.Single();
        (l.AbNummer, l.Kommission).Should().Be(((string?)null, "K-12"));
        log.Runs.Single().FinishedSuccess.Should().BeTrue();
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.StartsWith("Sage liefert KHKEKBelege_USER_AB nicht"))
            .And.NotContain(e => e.Message.Contains("KHKEKBelege_USER_Kom nicht"));
    }

    [Fact]
    public async Task Kopf_Lesefehler_PositionenTrotzdem_WerteVomLetztenLauf_Warnung_AK17()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        ctx.SagePurchaseOrderLines.Add(new SagePurchaseOrderLine { BelPosId = 101, BelId = 10, Belegkennzeichen = "EBB", Belegnummer = "2026-10",
            Artikelnummer = "00722001", AbNummer = "ALT-AB", Kommission = "ALT-K" });
        await ctx.SaveChangesAsync();
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log, Pos(101, "00722001"), Pos(102, "00722001"), Pos(111, "00722001"));
        Koepfe(client, 500);

        await svc.RunAsync(false, ziel: Ziel);

        var z = ctx.SagePurchaseOrderLines.OrderBy(l => l.BelPosId).ToList();
        z.Select(l => l.BelPosId).Should().Equal(101, 102, 111);
        z.Select(l => l.AbNummer).Should().Equal("ALT-AB", "ALT-AB", null);
        log.Runs.Single().FinishedSuccess.Should().BeTrue();
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("Kopfdaten (AB-Nummer/Kommission) nicht lesbar")
            && e.Message.Contains("Werte vom letzten Lauf"));
    }

    [Fact]
    public async Task Kopf_WertZuLang_NichtGeschrieben_NieGekuerzt_Zaehlwert_AK18()
    {
        using var ctx = TestDbContextFactory.Create();
        await SeedStammAsync(ctx);
        var log = new FakeSyncLogger();
        var (svc, client) = Svc(ctx, log, Pos(101, "00722001"));
        Koepfe(client, 200, $$"""{"BelID":10,"KHKEKBelege_USER_AB":"{{new string('A', 251)}}","KHKEKBelege_USER_Kom":"{{new string('K', 200)}}"}""");

        await svc.RunAsync(false, ziel: Ziel);

        var l = ctx.SagePurchaseOrderLines.Single();
        l.AbNummer.Should().BeNull();
        l.Kommission.Should().HaveLength(200, "genau die Spaltenlaenge passt");
        log.Runs.Single().FinalCounts!["kopf_zu_lang"].Should().Be(1);
        log.Runs.Single().Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("länger als die WMS-Spalte") && e.Message.Contains("251 > 250"));
    }

    [Theory]
    [InlineData(0, 2, true, null, 0, false, false)]      // nichts uebergeben
    [InlineData(1, 2, true, null, 0, true, false)]       // erster Anstoss sofort
    [InlineData(1, 2, true, -1, 0, false, false)]        // im Fenster (1 min nach dem letzten): offen lassen
    [InlineData(2, 2, true, -2, 0, true, false)]         // Fenster abgelaufen
    [InlineData(1, 0, true, null, 0, false, true)]       // 0 = aus: verwerfen
    [InlineData(1, 2, false, null, 0, false, true)]      // Gate aus: verwerfen
    public void AnstossEntscheidung_F1_2_bis_F1_4(int belege, int minuten, bool gate, int? letzterVorMin, int _, bool laufen, bool verwerfen)
    {
        var jetzt = new DateTime(2026, 11, 17, 10, 0, 0);
        SageBookingWorker.AnstossEntscheidung(belege, minuten, gate, letzterVorMin is { } m ? jetzt.AddMinutes(m) : null, jetzt)
            .Should().Be((laufen, verwerfen));
    }
}
