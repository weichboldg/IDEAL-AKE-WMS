using FluentAssertions;
using IDEALAKEWMSService.Services.Sdata;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IDEALAKEWMSService.Tests.Services.Sdata;

/// <summary>Spec 2026-11-06-sage-sdata-stammdaten-sync, E0: Quellenschalter, Weiche, Vergleich, SData-Ziel, DI.</summary>
public class SageQuelleTests
{
    // ----- Schalter (AK 4) -----

    [Theory]
    [InlineData(null, SageQuelle.View)]
    [InlineData("", SageQuelle.View)]
    [InlineData("View", SageQuelle.View)]
    [InlineData("SData", SageQuelle.SData)]
    [InlineData("Vergleich", SageQuelle.Vergleich)]
    public void Parse_GueltigeWerte_OhneWarnung(string? wert, SageQuelle erwartet)
    {
        var w = SageQuelleSchalter.Parse(SageQuelleSchalter.Lagerplaetze, wert);
        w.Quelle.Should().Be(erwartet);
        w.Warnung.Should().BeNull();
    }

    [Theory]
    [InlineData("sdata ")]
    [InlineData("SData ")]
    [InlineData("sdata")]
    [InlineData("Neu")]
    [InlineData(" ")]
    public void Parse_UnbekannterWert_ViewMitWarnung(string wert)
    {
        var w = SageQuelleSchalter.Parse(SageQuelleSchalter.Lagerbestand, wert);
        w.Quelle.Should().Be(SageQuelle.View);
        w.Warnung.Should().Contain("Sync:Quelle:Lagerbestand").And.Contain($"'{wert}'");
    }

    [Fact]
    public void Parse_SDataGesperrt_ViewMitWarnung_VergleichErlaubt()
    {
        var w = SageQuelleSchalter.Parse(SageQuelleSchalter.FaStruktur, "SData", "P-F2");
        w.Quelle.Should().Be(SageQuelle.View);
        w.Warnung.Should().Contain("P-F2");
        SageQuelleSchalter.Parse(SageQuelleSchalter.FaStruktur, "Vergleich", "P-F2").Should().Be(new SageQuellenWahl(SageQuelle.Vergleich, null));
    }

    [Fact]
    public void AlleBereiche_StehenImSettingsKatalog_MitDefaultView()
    {
        foreach (var bereich in new[] { SageQuelleSchalter.Lagerplaetze, SageQuelleSchalter.Lagerbestand, SageQuelleSchalter.Artikel,
                     SageQuelleSchalter.FaStruktur, SageQuelleSchalter.Arbeitsplaetze, SageQuelleSchalter.LagerbuchungLookup })
        {
            ServiceSettingDefinitions.TryGet(SageQuelleSchalter.Key(bereich), out var def).Should().BeTrue(bereich);
            def!.DefaultValue.Should().Be("View");
        }
    }

    // ----- Weiche -----

    private sealed record Zeile(string Code, string? Text, decimal? Menge);

    private sealed class TestWeiche : SageQuelleWeiche
    {
        public string? Schalter { get; set; }
        public string? Gesperrt { get; set; }
        public int ViewAufrufe, SdataAufrufe;
        public Func<List<Zeile>> View { get; set; } = () => new();
        public Func<List<Zeile>> Sdata { get; set; } = () => new();

        public TestWeiche(ISyncLogger logger) : base(new ConfigurationBuilder().Build(), logger) { }
        protected override string Bereich => SageQuelleSchalter.Lagerplaetze;
        protected override string? SdataGesperrtBis => Gesperrt;
        protected override Task<string?> LeseSchalterAsync(CancellationToken ct) => Task.FromResult(Schalter);

        public Task<List<Zeile>> GetAllAsync() => LeseAsync<List<Zeile>, Zeile>(
            _ => { ViewAufrufe++; return Task.FromResult(View()); },
            _ => { SdataAufrufe++; return Task.FromResult(Sdata()); },
            l => l, z => z.Code,
            new SdataVergleich.Feld<Zeile>[] { new("Text", z => z.Text), new("Menge", z => z.Menge) },
            CancellationToken.None);
    }

    private static (TestWeiche Weiche, Mock<ISyncLogger> Logger, Mock<ISyncRun> Run) Baue(string? schalter)
    {
        var run = new Mock<ISyncRun>();
        var logger = new Mock<ISyncLogger>();
        logger.Setup(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(run.Object);
        return (new TestWeiche(logger.Object) { Schalter = schalter }, logger, run);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("View")]
    public async Task Weiche_Default_NurView_KeinProtokoll(string? schalter)
    {
        var (w, logger, _) = Baue(schalter);
        w.View = () => new() { new("A", "x", 1) };

        (await w.GetAllAsync()).Should().ContainSingle(z => z.Code == "A");

        w.SdataAufrufe.Should().Be(0);
        logger.Verify(l => l.BeginRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never, "AK 1: kein Eintrag SageQuellenvergleich");
    }

    [Fact]
    public async Task Weiche_Unbekannt_View_GenauEineWarnung()
    {
        var (w, logger, run) = Baue("Neu");

        await w.GetAllAsync();

        w.ViewAufrufe.Should().Be(1);
        w.SdataAufrufe.Should().Be(0);
        logger.Verify(l => l.BeginRunAsync(SyncLogServices.SageQuellenvergleich, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.LogWarningAsync(It.Is<string>(m => m.Contains("'Neu'")), SageQuelleSchalter.Lagerplaetze, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Weiche_SData_NurSData_FehlerWirftDurch()
    {
        var (w, _, _) = Baue("SData");
        w.Sdata = () => new() { new("S", null, null) };
        (await w.GetAllAsync()).Single().Code.Should().Be("S");
        w.ViewAufrufe.Should().Be(0);

        w.Sdata = () => throw new SdataLeseException("LagerplaetzeInfo", 500, "kaputt");
        await ((Func<Task>)w.GetAllAsync).Should().ThrowAsync<SdataLeseException>();
        w.ViewAufrufe.Should().Be(0, "kein stiller Rueckfall auf View");
    }

    [Fact]
    public async Task Weiche_SDataGesperrt_LiestView()
    {
        var (w, _, run) = Baue("SData");
        w.Gesperrt = "P-F2";

        await w.GetAllAsync();

        w.SdataAufrufe.Should().Be(0);
        w.ViewAufrufe.Should().Be(1);
        run.Verify(r => r.LogWarningAsync(It.Is<string>(m => m.Contains("P-F2")), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Weiche_Vergleich_GibtViewZurueck_ProtokolliertAbweichungen()
    {
        var (w, logger, run) = Baue("Vergleich");
        w.View = () => new() { new("A", "x", 1.50m), new("B", "y", 2) };
        w.Sdata = () => new() { new("a", "x", 1.5m), new("B", "z", 2), new("C", "n", 0) };

        var ergebnis = await w.GetAllAsync();

        ergebnis.Select(z => z.Code).Should().Equal("A", "B");
        logger.Verify(l => l.BeginRunAsync(SyncLogServices.SageQuellenvergleich, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.LogWarningAsync(It.Is<string>(m => m.Contains("[B] Text") && m.Contains("'y'") && m.Contains("'z'")), SageQuelleSchalter.Lagerplaetze, It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.LogWarningAsync(It.Is<string>(m => m.Contains("[C] (fehlt in View)")), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        run.Verify(r => r.FinishSuccessAsync(It.Is<IReadOnlyDictionary<string, int>>(c => c["abweichend"] == 2 && c["Schlüssel View"] == 2 && c["Schlüssel SData"] == 3),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Weiche_Vergleich_SDataFehler_LaufFehlgeschlagen_UndWirft()
    {
        var (w, _, run) = Baue("Vergleich");
        w.Sdata = () => throw new SdataLeseException("LagerplaetzeInfo", 0, "Zeitüberschreitung");

        await ((Func<Task>)w.GetAllAsync).Should().ThrowAsync<SdataLeseException>();

        w.ViewAufrufe.Should().Be(1);
        run.Verify(r => r.FinishFailedAsync(It.Is<string>(m => m.Contains("Zeitüberschreitung")), It.IsAny<IReadOnlyDictionary<string, int>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ----- SdataVergleich -----

    private static readonly SdataVergleich.Feld<Zeile>[] Felder = { new("Text", z => z.Text), new("Menge", z => z.Menge) };

    [Fact]
    public void Vergleich_Gleich_KeineAbweichung_DezimalskalaUndGrossKlein()
    {
        var e = SdataVergleich.Vergleiche(new[] { new Zeile("x1", "a", 1.50m) }, new[] { new Zeile("X1", "a", 1.5m) }, z => z.Code, Felder);

        (e.SchluesselView, e.SchluesselSData, e.AbweichendeSchluessel).Should().Be((1, 1, 0));
        e.Beispiele.Should().BeEmpty();
    }

    [Fact]
    public void Vergleich_FehlendeUndMehrfache_UndFeldNull()
    {
        var view = new[] { new Zeile("A", "a", 1), new Zeile("D", "d", 1), new Zeile("D", "d", 1), new Zeile("N", null, 1) };
        var sdata = new[] { new Zeile("B", "b", 1), new Zeile("D", "d", 1), new Zeile("N", "", 1) };

        var e = SdataVergleich.Vergleiche(view, sdata, z => z.Code, Felder);

        e.AbweichendeSchluessel.Should().Be(4);
        e.Beispiele.Should().Equal(
            new SdataAbweichung("A", "(fehlt in SData)", "1", null),
            new SdataAbweichung("B", "(fehlt in View)", null, "1"),
            new SdataAbweichung("D", "(Anzahl Zeilen)", "2", "1"),
            new SdataAbweichung("N", "Text", null, ""));
    }

    [Fact]
    public void Vergleich_HoechstensZwanzigBeispiele_AnzahlVollstaendig()
    {
        var view = Enumerable.Range(1, 30).Select(i => new Zeile($"K{i:00}", "v", i)).ToList();
        var sdata = Enumerable.Range(1, 30).Select(i => new Zeile($"K{i:00}", "s", i + 1)).ToList();

        var e = SdataVergleich.Vergleiche(view, sdata, z => z.Code, Felder);

        e.AbweichendeSchluessel.Should().Be(30);
        e.Beispiele.Should().HaveCount(SdataVergleich.MaxBeispiele);
        e.Beispiele[0].Should().Be(new SdataAbweichung("K01", "Text", "v", "s"));
        e.Beispiele[1].Should().Be(new SdataAbweichung("K01", "Menge", "1", "2"));
    }

    // ----- SData-Ziel -----

    [Fact]
    public void Ziel_Vollstaendig_DefaultApplication()
    {
        var z = SdataLeseZielLeser.Bauen("https://sagetest01:5493", null, "ake_TEST2026;1", "u", null, false);
        z.Application.Should().Be("ol");
        z.Password.Should().BeEmpty();
        z.ZertifikatPruefen.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "ds", "u", "SData:BaseUrl")]
    [InlineData("https://u:p@host", "ds", "u", "ohne Zugangsdaten")]
    [InlineData("https://host", " ", "u", "SData:Dataset")]
    [InlineData("https://host", "ds", "", "SageLagerbuchung:Username")]
    public void Ziel_Unvollstaendig_Wirft(string? baseUrl, string? dataset, string? user, string erwartet)
    {
        var act = () => SdataLeseZielLeser.Bauen(baseUrl, "ol", dataset, user, "p", true);
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{erwartet}*");
    }

    // ----- DI (fallstricke §10, wie ADR 0013) -----

    [Fact]
    public void DI_SdataReadClient_IstAufloesbar_MitSeitenTimeout()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSageSdataReadClient();
        using var sp = services.BuildServiceProvider();

        var client = sp.GetRequiredService<ISageSdataReadClient>();

        client.Should().BeOfType<SageSdataReadClient>();
        var http = (HttpClient)typeof(SageSdataReadClient).GetField("_http", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(client)!;
        http.Timeout.Should().Be(SageSdataReadClient.SeitenTimeout);
    }

    [Fact]
    public void SyncLogServices_KenntSageQuellenvergleich_MitDeutschemNamen()
    {
        SyncLogServices.All.Should().Contain(SyncLogServices.SageQuellenvergleich);
        SyncLogServices.DisplayName(SyncLogServices.SageQuellenvergleich).Should().NotBe(SyncLogServices.SageQuellenvergleich);
    }
}
