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

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E3a: Artikel-Reader (SData), Weiche inkl. Sperre P-R1, Satellit-Schreibregel, DI.</summary>
public class ArtikelSdataTests
{
    private static readonly SdataLeseZiel Ziel = new("https://sagetest01:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static SdataArtikelZeile Artikel(string nr, string? gruppe = "940", int? platz = null, string? bez = "Teil") =>
        new(nr, bez, "Bez2", "MC-" + nr, gruppe, "9", "94", null, "Hersteller", null, "Stk", 2, true, platz, "FIFO", 1, 0, 0,
            true, false, false, false, false, true, 0, "P", 1.23456m, 1m, 100m, "Z-1", 10m, 20m, 30m, "kg", "Langer Text");

    private static SdataArtikelVarianteZeile Variante(string nr, int auspraegung, decimal? melde, string? lieferant = "K31013") =>
        new(nr, auspraegung, "4000000000000", "A", 200m, 180m, 150m, 2.5m, 12.345678m, 50m, melde, 7, lieferant, "K1", 14, 11, 5m);

    // ----- Mapping -----

    [Fact]
    public void Abbilden_WieSqlSelect_GruppeReinerCode_Meldebestand0IstNull_RessourceOhneStammNurNummer()
    {
        var dtos = SdataArtikelReader.Abbilden(
            new[] { Artikel("00722001", platz: 11), Artikel("86003871", gruppe: null), Artikel("00722001 ") },
            new[] { "R-OHNE-STAMM" },
            new[] { Variante("00722001", 2, 99m), Variante("00722001", 0, 85m), Variante("86003871", 0, 0m) },
            new[] { new SdataLagerplatzZeile(11, "HR", " HR 0 0 0 ", null, null, null, null, null, null, null, null) });

        dtos.Should().HaveCount(3, "00722001 mit Leerzeichen am Ende ist dieselbe Nummer (SQL GROUP BY)");
        var a = dtos[0];
        (a.ArticleNumber, a.Description, a.Unit, a.ArticleGroup, a.ReorderLevel, a.PrimaryStorageLocation, a.Matchcode)
            .Should().Be(("00722001", "Teil", "Stk", "940", 85m, "HR 0 0 0", "MC-00722001"));
        a.Details!.Hauptlieferant.Should().Be("K31013");
        a.Details.Wiederbeschaffungszeit.Should().Be(14);
        a.Details.SagePlatzId.Should().Be(11);
        a.Details.Schwund.Should().Be(1.235m, "decimal(18,3) – gerundet, sonst jede Lauf eine Aenderung");
        a.Details.MittlererEK.Should().Be(12.3457m);
        a.Details.IstBestellartikel.Should().BeTrue();
        dtos[1].ReorderLevel.Should().BeNull("Meldebestand 0 = kein Meldebestand (wie ParseReorderLevel)");
        dtos[2].Should().Be(new SageArtikelDto("R-OHNE-STAMM", null, null, null, null, null, null), "LEFT JOIN ohne Artikelstamm, ohne Details");
    }

    [Fact]
    public void Abbilden_ZeileOhneArtikelnummer_Wirft()
    {
        ((Action)(() => SdataArtikelReader.Abbilden(new[] { Artikel(" ") }, Array.Empty<string>(),
            Array.Empty<SdataArtikelVarianteZeile>(), Array.Empty<SdataLagerplatzZeile>()))).Should().Throw<SdataLeseException>();
        ((Action)(() => SdataArtikelReader.Abbilden(new[] { Artikel("A") }, Array.Empty<string>(),
            new[] { Variante(null!, 0, 1m) }, Array.Empty<SdataLagerplatzZeile>()))).Should().Throw<SdataLeseException>();
    }

    [Fact]
    public void InFilter_QuotetUndVerdoppeltHochkomma() =>
        SdataArtikelReader.InFilter(new[] { "A1", "O'Neil" }).Should().Be("Artikelnummer in ('A1','O''Neil')");

    [Fact]
    public async Task LesenAsync_Bestellartikel_RessourcenInBloecken_Varianten_Plaetze()
    {
        var client = new Mock<ISageSdataReadClient>();
        static SdataSeiten<T> S<T>(string r, params T[] z) => new(z, r, 1, z.Length, 1);
        client.Setup(c => c.QueryAllAsync<SdataArtikelZeile>(Ziel, SageSdataReadClient.VertragResources, "Artikel",
                SdataArtikelReader.WhereBestellartikel, It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("Artikel", Artikel("B-1")));
        client.Setup(c => c.QueryAllAsync<SdataRessourceZeile>(Ziel, SageSdataReadClient.VertragResources, "PpsRessourcenPositionenInfo",
                SdataArtikelReader.WhereRessourcen, "Ressourcenummer", SdataArtikelReader.OrderByRessourcen, It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("PpsRessourcenPositionenInfo", new SdataRessourceZeile("b-1"), new SdataRessourceZeile("R-2"), new SdataRessourceZeile("R-2")));
        client.Setup(c => c.QueryAllAsync<SdataArtikelZeile>(Ziel, SageSdataReadClient.VertragResources, "Artikel",
                "Artikelnummer in ('R-2')", It.IsAny<string?>(), "Artikelnummer", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("Artikel", Artikel("R-2")));
        client.Setup(c => c.QueryAllAsync<SdataArtikelVarianteZeile>(Ziel, SageSdataReadClient.VertragResources, "ArtikelVariantenExtended",
                "Artikelnummer in ('B-1','R-2')", It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S("ArtikelVariantenExtended", Variante("B-1", 0, 3m)));
        client.Setup(c => c.QueryAllAsync<SdataLagerplatzZeile>(Ziel, SageSdataReadClient.VertragResources, "LagerplaetzeInfo",
                null, SdataArtikelReader.SelectPlaetze, "PlatzID", It.IsAny<CancellationToken>(), It.IsAny<int>()))
            .ReturnsAsync(S<SdataLagerplatzZeile>("LagerplaetzeInfo"));
        var reader = new SdataArtikelReader(client.Object, new ConfigurationBuilder().Build(), NullLogger<SdataArtikelReader>.Instance);

        var dtos = await reader.LesenAsync(Ziel, CancellationToken.None);

        dtos.Select(d => d.ArticleNumber).Should().Equal("B-1", "R-2");
        dtos[0].ReorderLevel.Should().Be(3m);
        reader.Hinweise.Should().Contain(h => h.Text.Contains("ArtikelVariantenExtended") && h.Text.Contains("1 Block"));
    }

    // ----- Weiche -----

    private sealed class FakeArtikelReader : ISageArtikelReader
    {
        public SageArtikelLesung Lesung { get; set; } = new(new());
        public Exception? ThrowOnRead { get; set; }
        public int Aufrufe { get; private set; }
        public Task<SageArtikelLesung> GetAllAsync(CancellationToken ct = default)
        {
            Aufrufe++;
            if (ThrowOnRead is not null) throw ThrowOnRead;
            return Task.FromResult(Lesung);
        }
    }

    private sealed class TestSwitch : ArtikelQuelleSwitch
    {
        public string? Schalter { get; set; }
        /// <summary>Wert von Sync:ArticlesViewName; Default = IDEAL-View gesetzt (gesperrt). null = AKE.</summary>
        public string? ArtikelView { get; set; } = "vw_IDEAL_AKE_WMS_Artikel";
        public Exception? ArtikelViewFehler { get; set; }
        public TestSwitch(ISageArtikelReader view, ISageArtikelReader sdata, ISyncErrorNotifier notifier, ISyncLogger logger)
            : base(view, sdata, new ConfigurationBuilder().Build(), notifier, logger) { }
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);
        protected override Task<string?> LeseArtikelViewAsync(CancellationToken ct) =>
            ArtikelViewFehler is null ? Task.FromResult(ArtikelView) : Task.FromException<string?>(ArtikelViewFehler);
    }

    private static (TestSwitch W, FakeArtikelReader View, FakeArtikelReader Sdata, FakeSyncLogger Logger) Weiche(string? schalter)
    {
        var view = new FakeArtikelReader { Lesung = new(new() { new("A-1", "Teil", "Stk", "940", 5m, "L-1", "MC") }) };
        var sdata = new FakeArtikelReader
        {
            Lesung = new(new() { new("a-1", "Teil", "Stk", "940 - Gruppe", 5m, "L-1", "MC", new SageArtikelDetails { Hauptlieferant = "K1" }) }),
        };
        var logger = new FakeSyncLogger();
        return (new TestSwitch(view, sdata, new Mock<ISyncErrorNotifier>().Object, logger) { Schalter = schalter }, view, sdata, logger);
    }

    [Fact]
    public async Task Weiche_View_LiestNurView()
    {
        var (w, _, sdata, logger) = Weiche("View");
        (await w.GetAllAsync()).Artikel.Single().Details.Should().BeNull();
        sdata.Aufrufe.Should().Be(0);
        logger.Runs.Should().BeEmpty();
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
    }

    [Fact]
    public async Task Weiche_SData_AnAke_OhneArtikelView_LiestSData_G16neu()
    {
        var (w, view, sdata, logger) = Weiche("SData");
        w.ArtikelView = null;

        var l = await w.GetAllAsync();

        (view.Aufrufe, sdata.Aufrufe).Should().Be((0, 1));
        l.Artikel.Single().Details!.Hauptlieferant.Should().Be("K1", "SData fuellt ArticleSageDetails");
        w.GenutzteQuelle.Should().Be(SageQuelle.SData);
        w.QuellenWarnung.Should().BeNull();
        logger.Runs.Should().BeEmpty("keine Sperre, keine Warnung");
    }

    [Fact]
    public async Task Weiche_SData_ArtikelViewNichtLesbar_BleibtGesperrt()
    {
        var (w, view, sdata, logger) = Weiche("SData");
        w.ArtikelViewFehler = new InvalidOperationException("DB weg");

        await w.GetAllAsync();

        (view.Aufrufe, sdata.Aufrufe).Should().Be((1, 0), "fail-safe: ohne Beleg AKE/IDEAL nie SData");
        w.QuellenWarnung.Should().Contain("gesperrt bis P-R1").And.Contain("nicht lesbar");
    }

    [Fact]
    public async Task Weiche_SData_GesperrtBisPR1_WirktWieView_MitWarnung()
    {
        var (w, view, sdata, logger) = Weiche("SData");

        var l = await w.GetAllAsync();

        l.Artikel.Single().Details.Should().BeNull("gesperrt: View gelesen, Satellit bleibt unberuehrt");
        (view.Aufrufe, sdata.Aufrufe).Should().Be((1, 0));
        w.GenutzteQuelle.Should().Be(SageQuelle.View);
        logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich).Events
            .Should().ContainSingle(e => e.Level == "Warning" && e.Message.Contains("gesperrt bis P-R1"));
        w.QuellenWarnung.Should().Contain("vw_IDEAL_AKE_WMS_Artikel", "B6: der Bereichslauf nennt den Grund");
    }

    [Theory]
    [InlineData("FaStruktur", null, "P-F2")]
    [InlineData("Artikel", "vw_X", "P-R1")]
    [InlineData("Artikel", " ", null)]
    [InlineData("Artikel", null, null)]
    [InlineData("Lagerplaetze", "vw_X", null)]
    public void SageQuelleSperre_Regel(string bereich, string? view, string? beginntMit)
    {
        var grund = SageQuelleSperre.Grund(bereich, view);
        if (beginntMit is null) grund.Should().BeNull();
        else grund.Should().StartWith(beginntMit);
    }

    [Fact]
    public async Task Weiche_Vergleich_GibtViewZurueck_OhneDetails_MeldetGruppenFormat()
    {
        var (w, _, sdata, logger) = Weiche("Vergleich");

        var l = await w.GetAllAsync();

        l.Artikel.Single().Details.Should().BeNull("im Vergleich wird der Satellit nie geschrieben (nur View-Zeilen)");
        sdata.Aufrufe.Should().Be(1);
        var lauf = logger.Runs.Single(r => r.ServiceName == SyncLogServices.SageQuellenvergleich);
        lauf.FinalCounts!["abweichend"].Should().Be(1);
        lauf.Events.Should().Contain(e => e.Message.Contains("[A-1] ArticleGroup") && e.Message.Contains("'940 - Gruppe'"));
    }

    // ----- Satellit-Schreibregel -----

    [Fact]
    public async Task Writer_NurMitDetails_LegtAn_AendertNurBeiAenderung_MitAudit()
    {
        var ctx = TestDbContextFactory.Create();
        ctx.Articles.AddRange(
            new Article { Id = 1, ArticleNumber = "A-1", CreatedBy = "x", CreatedByWindows = "x" },
            new Article { Id = 2, ArticleNumber = "B-2", CreatedBy = "x", CreatedByWindows = "x" });
        ctx.SaveChanges();
        var details = new SageArtikelDetails { Hauptlieferant = "K31013", Wiederbeschaffungszeit = 14, IstBestellartikel = true };

        var r1 = await ArticleSageDetailsWriter.SchreibenAsync(ctx, new[]
        {
            new SageArtikelDto("a-1 ", null, null, null, null, null, null, details),
            new SageArtikelDto("B-2", null, null, null, null, null, null),           // View-Zeile: nie anfassen
            new SageArtikelDto("FEHLT", null, null, null, null, null, null, details),
        }, CancellationToken.None);

        r1.Should().Be((1, 0, 1));
        var zeile = ctx.ArticleSageDetails.Single();
        (zeile.ArticleId, zeile.Hauptlieferant, zeile.Wiederbeschaffungszeit, zeile.CreatedBy).Should().Be((1, "K31013", 14, "IDEALAKEWMSService"));
        zeile.ModifiedAt.Should().BeNull();

        (await ArticleSageDetailsWriter.SchreibenAsync(ctx, new[] { new SageArtikelDto("A-1", null, null, null, null, null, null, details) },
            CancellationToken.None)).Should().Be((0, 0, 0));
        zeile.ModifiedAt.Should().BeNull("unveraendert = kein Audit-Update");

        (await ArticleSageDetailsWriter.SchreibenAsync(ctx, new[] { new SageArtikelDto("A-1", null, null, null, null, null, null,
            details with { Wiederbeschaffungszeit = 21 }) }, CancellationToken.None)).Should().Be((0, 1, 0));
        zeile.Wiederbeschaffungszeit.Should().Be(21);
        (zeile.ModifiedAt, zeile.ModifiedBy, zeile.ModifiedByWindows).Should().Match<(DateTime?, string?, string?)>(t =>
            t.Item1 != null && t.Item2 == "IDEALAKEWMSService" && !string.IsNullOrEmpty(t.Item3));

        (await ArticleSageDetailsWriter.SchreibenAsync(ctx, new[] { new SageArtikelDto("A-1", null, null, null, null, null, null) },
            CancellationToken.None)).Should().Be((0, 0, 0));
        ctx.ArticleSageDetails.Single().Wiederbeschaffungszeit.Should().Be(21, "View-Lauf (ohne Details) laesst den Satelliten unveraendert");
    }

    [Fact]
    public void Details_HabenJedeSpalteDesSatelliten_GleichNamigUndTyp()
    {
        // SetValues kopiert nach Namen: jedes DTO-Feld muss eine gleichnamige Entity-Property mit gleichem Typ haben (und umgekehrt).
        var dto = typeof(SageArtikelDetails).GetProperties().Where(p => p.Name != "EqualityContract").ToDictionary(p => p.Name, p => p.PropertyType);
        var basis = typeof(AuditableEntity).GetProperties().Select(p => p.Name).Concat(new[] { "ArticleId", "Article" }).ToHashSet();
        var entity = typeof(ArticleSageDetail).GetProperties().Where(p => !basis.Contains(p.Name)).ToDictionary(p => p.Name, p => p.PropertyType);
        dto.Should().BeEquivalentTo(entity);
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_ArtikelReader_IstWeiche_MitBeidenReadern()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddScoped(_ => new Mock<ISyncErrorNotifier>().Object);
        services.AddSageSdataReadClient();
        services.AddArtikelQuelle();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISageArtikelReader>().Should().BeOfType<ArtikelQuelleSwitch>();
        scope.ServiceProvider.GetRequiredService<SageArtikelReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SdataArtikelReader>().Should().NotBeNull();
    }
}
