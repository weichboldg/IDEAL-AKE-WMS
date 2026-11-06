using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E4a: FA-Struktur-Kanten aus SData (Regel F6), Weiche inkl. Sperre P-F2,
/// Vergleich der Kanten, Abgleich ueber das Reader-Interface (Empty-Guard, ungueltiger View-Name), DI.</summary>
public class FaStrukturSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);
    private static readonly DateTime Jetzt = new(2026, 11, 6, 12, 0, 0);

    private static SdataFaPositionZeile Pos(int belPosId, int belId, int position, int? nachfolger, string typ, string nr, decimal menge = 1m) =>
        new(belPosId, belId, position, nachfolger, typ, nr, menge);

    /// <summary>Struktur 835 am AKE-Test (sage-sdata-commonwawiservicesresources): 7 FAs, 17 Positionen.</summary>
    private static (SdataFaKopfZeile[] Koepfe, SdataFaPositionZeile[] Positionen) Struktur835()
    {
        var koepfe = new[] { new SdataFaKopfZeile(835, 835, "86003871") }
            .Concat(Enumerable.Range(836, 6).Select(id => new SdataFaKopfZeile(id, 835, "BG-" + id))).ToArray();
        var pos = new List<SdataFaPositionZeile>();
        for (var i = 1; i <= 6; i++) pos.Add(Pos(10600 + i, 835, i, 835 + i, "MA", "BG-" + (835 + i)));
        pos.Add(Pos(10607, 835, 7, 0, "MA", "KAUF-1"));
        pos.Add(Pos(10608, 835, 8, null, "MA", "KAUF-2"));
        pos.Add(Pos(10656, 835, 9, 0, "AG", "999"));
        pos.Add(Pos(10641, 836, 1, 0, "MA", "00722001", 0.6m));
        pos.Add(Pos(10642, 836, 2, 0, "AG", "10"));
        for (var id = 837; id <= 841; id++) pos.Add(Pos(10600 + id, id, 1, 0, "MA ", "T-" + id));
        pos.Add(Pos(10700, 837, 2, 0, "AG", "20"));
        pos.Should().HaveCount(17);
        return (koepfe, pos.ToArray());
    }

    // ----- Projektion / Hierarchie-Regel F6 -----

    [Fact]
    public void Projizieren_Struktur835_WurzelKantenBlaetter_OhneAg()
    {
        var (koepfe, pos) = Struktur835();

        var k = SdataFaStrukturReader.Projizieren(koepfe, pos, Jetzt);

        k.Should().HaveCount(1 + 6 + 2 + 1 + 5, "Wurzel + 6 Kanten + 2 Blaetter an 835 + 836er Blatt + je ein Blatt 837–841; AG faellt weg");
        k.Should().OnlyContain(n => n.HauptFA == 835 && n.HauptArtnr == "86003871" && n.SyncedAt == Jetzt);

        var wurzel = k.Single(n => n.VaterFA == null);
        (wurzel.SubFA, wurzel.Position).Should().Be((835, (int?)null), "Wurzel: SubFA = HauptFA, Position NULL");

        k.Where(n => n.VaterFA == 835 && n.SubFA != 0).Select(n => n.SubFA).Should().Equal(836, 837, 838, 839, 840, 841);
        k.Where(n => n.VaterFA == 835 && n.SubFA == 0).Select(n => n.Artnr).Should().Equal("KAUF-1", "KAUF-2");

        var blatt = k.Single(n => n.VaterFA == 836);
        (blatt.SubFA, blatt.Position, blatt.Artnr, blatt.Sollmenge, blatt.Fertigungmenge).Should().Be((0, (int?)1, "00722001", 0.6m, 0.6m));
        k.Should().NotContain(n => n.Artnr == "999" || n.Artnr == "10" || n.Artnr == "20");
    }

    [Fact]
    public void Projizieren_Zwischenebene_VaterIstSubFaDerEbeneDarueber_HauptFaBleibtStruktur()
    {
        var koepfe = new[] { new SdataFaKopfZeile(900, 900, "GERAET"), new SdataFaKopfZeile(901, 900, "BG1"), new SdataFaKopfZeile(902, 900, "BG2") };
        var pos = new[]
        {
            Pos(1, 900, 1, 901, "MA", "BG1"),
            Pos(2, 901, 1, 902, "MA", "BG2"),
            Pos(3, 902, 1, 0, "MA", "BLECH"),
        };

        var k = SdataFaStrukturReader.Projizieren(koepfe, pos, Jetzt);

        k.Select(n => (n.HauptFA, n.VaterFA, n.Position, n.SubFA)).Should().Equal(
            (900, (int?)null, (int?)null, 900),
            (900, 900, 1, 901),
            (900, 901, 1, 902),
            (900, 902, 1, 0));
    }

    [Fact]
    public void Projizieren_HauptFaNichtInMenge_KeineWurzel_Hinweis_FremdePositionenFallenWeg()
    {
        var hinweise = new List<SdataHinweis>();
        var k = SdataFaStrukturReader.Projizieren(
            new[] { new SdataFaKopfZeile(951, 950, "BG") },
            new[] { Pos(1, 951, 1, 0, "MA", "T"), Pos(2, 777, 1, 0, "MA", "FREMD") }, Jetzt, hinweise);

        k.Should().ContainSingle().Which.Should().Match<FaHierarchyNode>(n => n.HauptFA == 950 && n.VaterFA == 951 && n.HauptArtnr == null);
        hinweise.Should().ContainSingle(h => h.Warnung && h.Text.Contains("950"));
    }

    [Fact]
    public void Projizieren_ZeileOhneBelId_Wirft()
    {
        ((Action)(() => SdataFaStrukturReader.Projizieren(new[] { new SdataFaKopfZeile(null, 1, "A") },
            Array.Empty<SdataFaPositionZeile>(), Jetzt))).Should().Throw<SdataLeseException>();
        ((Action)(() => SdataFaStrukturReader.Projizieren(new[] { new SdataFaKopfZeile(1, 1, "A") },
            new[] { new SdataFaPositionZeile(1, null, 1, 0, "MA", "X", 1m) }, Jetzt))).Should().Throw<SdataLeseException>();
    }

    [Fact]
    public void WhereKoepfe_OffeneUndBeendeteDerLetzten30Tage() =>
        SdataFaStrukturReader.WhereKoepfe(new DateTime(2026, 11, 6, 15, 30, 0))
            .Should().Be("IstBeendet eq 0 or Istendtermin ge @2026-10-07@");

    [Fact]
    public async Task LesenAsync_KoepfeMitFenster_PositionenInOrBloecken_OhneFaInfos()
    {
        var (koepfe, pos) = Struktur835();
        var client = new Mock<ISageSdataReadClient>();
        client.Setup(c => c.QueryAllAsync<SdataFaKopfZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegeInfo",
                SdataFaStrukturReader.WhereKoepfe(Jetzt), SdataFaStrukturReader.SelectKoepfe, "BelID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaKopfZeile>(koepfe, "PpsFaBelegeInfo", 1, koepfe.Length, 1));
        client.Setup(c => c.QueryAllAsync<SdataFaPositionZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsFaBelegePositionenInfo",
                "BelID eq 835 or BelID eq 836 or BelID eq 837 or BelID eq 838 or BelID eq 839 or BelID eq 840 or BelID eq 841",
                SdataFaStrukturReader.SelectPositionen, "BelPosID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(new SdataSeiten<SdataFaPositionZeile>(pos, "PpsFaBelegePositionenInfo", 1, pos.Length, 1));
        var reader = new SdataFaStrukturReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataFaStrukturReader>.Instance);

        var l = await reader.LesenAsync(Ziel, Jetzt, CancellationToken.None);

        l.Knoten.Should().HaveCount(15);
        (l.Infos.Count, l.InfosAnzahl).Should().Be((0, 0L), "G11: FAInfos bleiben SQL – im Modus SData wuerde der Empty-Guard greifen");
        reader.Hinweise.Should().Contain(h => h.Text.Contains("PpsFaBelegePositionenInfo") && h.Text.Contains("1 Block"));
    }

    // ----- Weiche -----

    private sealed class FakeReader : ISageFaStrukturReader
    {
        public FaStrukturLesung Lesung { get; set; } = new(new(), new(), 0, 0);
        public Exception? ThrowOnRead { get; set; }
        public int Aufrufe { get; private set; }
        public Task<FaStrukturLesung> LesenAsync(CancellationToken ct = default)
        {
            Aufrufe++;
            if (ThrowOnRead is not null) throw ThrowOnRead;
            return Task.FromResult(Lesung);
        }
    }

    private sealed class TestSwitch : FaStrukturQuelleSwitch
    {
        public string? Schalter { get; set; }
        public TestSwitch(ISageFaStrukturReader view, ISageFaStrukturReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
    }

    private static FaHierarchyNode N(int haupt, int? vater, int? pos, int sub, string? artnr = null, decimal menge = 1m) =>
        new() { HauptFA = haupt, VaterFA = vater, Position = pos, SubFA = sub, HauptArtnr = "G", Artnr = artnr, Sollmenge = menge, Fertigungmenge = menge };

    private static (TestSwitch W, FakeReader View, FakeReader Sdata, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Weiche(string? schalter)
    {
        var view = new FakeReader
        {
            Lesung = new(new() { N(835, null, null, 835), N(835, 835, 1, 836, "BG"), N(835, 836, 1, 0, "00722001", 0.6m) },
                new() { new FaHierarchyOrderInfo { HauptFA = 835 } }, 3, 1),
        };
        var sdata = new FakeReader
        {
            Lesung = new(new()
            {
                N(835, null, null, 835, "WURZEL-ARTNR", 99m), // Wurzel-Artnr/Menge werden nicht verglichen
                N(835, 835, 2, 836, "BG"),                     // Eltern-Kante weicht ab (Position)
                N(835, 836, 1, 0, "00722001", 0.60m),          // 0,60 = 0,6
                N(999, null, null, 999),                       // Struktur ohne FAInfos -> nicht im Vergleich
            }, new(), 4, 0),
        };
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new TestSwitch(view, sdata, notifier.Object, logger) { Schalter = schalter }, view, sdata, logger, notifier);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView()
    {
        var (w, view, sdata, logger, _) = Weiche("View");
        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);
        sdata.Aufrufe.Should().Be(0);
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_GesperrtBisPF2_WirktWieView_MitWarnung()
    {
        var (w, view, sdata, logger, _) = Weiche("SData");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);

        (view.Aufrufe, sdata.Aufrufe).Should().Be((1, 0));
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).Events
            .Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("gesperrt bis P-F2"));
    }

    [Fact]
    public async Task Weiche_Vergleich_GibtViewZurueck_MeldetElternKante_OhneStrukturenOhneFaInfos()
    {
        var (w, view, sdata, logger, _) = Weiche("Vergleich");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung, "geschrieben wird nur aus der View");

        sdata.Aufrufe.Should().Be(1);
        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1);
        lauf.FinalCounts["Schlüssel SData"].Should().Be(3, "Struktur 999 hat keine FAInfos (EXISTS wie im View-Select)");
        lauf.Events.Should().ContainSingle(e => e.Level == "Warning").Which.Message.Should().Contain("[FA 836] Position").And.Contain("'1'").And.Contain("'2'");
    }

    [Fact]
    public async Task Weiche_Vergleich_SdataFehler_ViewDatenWeiter_Mail()
    {
        var (w, view, sdata, logger, notifier) = Weiche("Vergleich");
        sdata.ThrowOnRead = new SdataLeseException("PpsFaBelegeInfo", 500, "Ungültiger Spaltenname");

        (await w.LesenAsync()).Should().BeSameAs(view.Lesung);

        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Schluessel_FaUeberBelId_BlattUeberPlatzImVater()
    {
        FaStrukturQuelleSwitch.Schluessel(N(835, null, null, 835)).Should().Be("FA 835");
        FaStrukturQuelleSwitch.Schluessel(N(835, 835, 1, 836)).Should().Be("FA 836");
        FaStrukturQuelleSwitch.Schluessel(N(835, 836, 3, 0)).Should().Be("Blatt 836/3");
    }

    // ----- Abgleich ueber das Reader-Interface (Extraktion E4a; Pfade ohne Schreib-SQL) -----

    private static (FaHierarchySyncService S, FakeSyncLogger Logger, Mock<ISyncErrorNotifier> Notifier) Abgleich(ISageFaStrukturReader reader)
    {
        var logger = new FakeSyncLogger();
        var notifier = new Mock<ISyncErrorNotifier>();
        return (new FaHierarchySyncService(TestDbContextFactory.Create(), reader, notifier.Object,
            NullLogger<FaHierarchySyncService>.Instance, logger), logger, notifier);
    }

    [Fact]
    public async Task Abgleich_LeererViewRead_UeberspringtOhneMutation_MitMail()
    {
        var (s, logger, notifier) = Abgleich(new FakeReader { Lesung = new(new(), new(), 5, 0) });

        var r = await s.RunAsync(dryRun: false);

        r.Skipped.Should().BeTrue();
        var lauf = logger.Runs.Single();
        lauf.FinalMessageSuffix.Should().Be("[uebersprungen: leerer View-Read]");
        lauf.Events.Should().Contain(e => e.Level == "Warning" && e.Message.Contains("FAListe=5, FAInfos=0"));
        notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: leerer View-Read", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abgleich_UngueltigerViewName_Uebersprungen_Fehler_Mail_KeinThrow()
    {
        var (s, logger, notifier) = Abgleich(new FakeReader { ThrowOnRead = new FaHierarchyViewNameException("[x];DROP") });

        var r = await s.RunAsync(dryRun: false);

        r.Skipped.Should().BeTrue();
        logger.Runs.Single().FinishedFailed.Should().BeTrue();
        notifier.Verify(n => n.NotifyAsync("FaHierarchy-Sync: ungueltiger View-Name", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abgleich_DryRun_ZaehltKnotenUndInfos_SchreibtNichts()
    {
        var reader = new FakeReader { Lesung = new(new() { N(1, null, null, 1) }, new() { new FaHierarchyOrderInfo { HauptFA = 1 } }, 1, 1) };
        var (s, logger, _) = Abgleich(reader);

        var r = await s.RunAsync(dryRun: true);

        (r.Nodes, r.OrderInfos, r.DryRun).Should().Be((1, 1, true));
        logger.Runs.Single().FinalCounts!["neu"].Should().Be(2);
    }

    [Fact]
    public async Task Abgleich_LeseFehler_FinishFailed_Rethrow()
    {
        var (s, logger, _) = Abgleich(new FakeReader { ThrowOnRead = new InvalidOperationException("View fehlt") });

        await s.Invoking(x => x.RunAsync(dryRun: false)).Should().ThrowAsync<InvalidOperationException>();
        logger.Runs.Single().FinishedFailed.Should().BeTrue();
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_FaStrukturReader_IstWeiche_MitBeidenReadern_AbgleichAufloesbar()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddSageSdataReadClient();
        services.AddFaStrukturQuelle();
        services.AddScoped<IFaHierarchySyncService, FaHierarchySyncService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageFaStrukturReader>().Should().BeOfType<FaStrukturQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageFaStrukturReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataFaStrukturReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IFaHierarchySyncService>().Should().NotBeNull();
    }
}
