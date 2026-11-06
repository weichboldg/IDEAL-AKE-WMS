using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Tests.Services.SageSdata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class SageSdataFaControllerTests
{
    /// <summary>Fake-Client: liefert je (Quelle, where) eine vorbereitete Antwort und protokolliert jeden Aufruf.</summary>
    private sealed class FakeClient : ISageSdataReadClient
    {
        public Func<string, string, object?> Antwort { get; set; } = (_, _) => null;
        public List<(string Quelle, string Where, SdataLeseZiel Ziel)> Aufrufe { get; } = new();

        public Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct)
        {
            Aufrufe.Add((quelle, where, ziel));
            var url = SageSdataReadClient.BuildUrl(ziel, quelle, where, select, orderBy);
            return Task.FromResult(Antwort(quelle, where) switch
            {
                int status => new SdataErgebnis<T>(Array.Empty<T>(), new SdataCall(quelle, url, status, 1, 0, status == 0 ? "Host unbekannt" : $"HTTP {status}", null)),
                IEnumerable<T> zeilen => new SdataErgebnis<T>(zeilen.ToList(), new SdataCall(quelle, url, 200, 1, zeilen.Count(), null, null)),
                _ => new SdataErgebnis<T>(Array.Empty<T>(), new SdataCall(quelle, url, 200, 1, 0, null, null)),
            });
        }

        // FA-Ansicht nutzt nur RealTimeData (GetAsync); die Paging-Methode (v1.73.0) darf hier nie gerufen werden.
        public Task<SdataSeiten<T>> QueryAllAsync<T>(SdataLeseZiel ziel, string vertrag, string ressource, string? where, string? select,
            string orderBy, CancellationToken ct, int count = SageSdataReadClient.Seitengroesse) => throw new NotSupportedException();
    }

    private readonly FakeClient _client = new();
    private readonly Mock<IAppSettingRepository> _app = new();
    private readonly Mock<IServiceSettingRepository> _svc = new();
    private readonly Dictionary<string, string?> _config = new()
    {
        ["SageLagerbuchung:Username"] = "sdatauser",
        ["SageLagerbuchung:Password"] = "geheim",
    };

    public SageSdataFaControllerTests()
    {
        _app.Setup(a => a.GetValueAsync(AppSettingKeys.SageSdataFaAnsichtAktiv)).ReturnsAsync("true");
        _svc.Setup(s => s.GetValueAsync("SData:BaseUrl")).ReturnsAsync("https://sagetest01.ake.at:5493");
        _svc.Setup(s => s.GetValueAsync("SData:Dataset")).ReturnsAsync("ake_TEST2026;1");
    }

    private SageSdataFaController Controller()
    {
        var c = new SageSdataFaController(_client, _app.Object, _svc.Object, new ConfigurationBuilder().AddInMemoryCollection(_config).Build());
        var http = new DefaultHttpContext();
        c.ControllerContext = new ControllerContext { HttpContext = http };
        c.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        return c;
    }

    private async Task<SageSdataFaViewModel> Vm(int? belId) =>
        (SageSdataFaViewModel)((ViewResult)await Controller().Index(belId, CancellationToken.None)).Model!;

    /// <summary>Sage-Test-Struktur 835 aus den Fixtures, Rueckmelde-Quellen leer.</summary>
    private void Struktur835(Func<string, string, object?>? ueberschreiben = null)
    {
        var fas = SageSdataFixtures.Fas835();
        var pos = SageSdataFixtures.Positionen835Und836();
        _client.Antwort = (q, w) => ueberschreiben?.Invoke(q, w) ?? (q, w) switch
        {
            (SageSdataQuellen.FaBelegsuche, var x) when x.StartsWith("BelID eq ") => fas.Where(f => w == $"BelID eq {f.BelID}").ToList(),
            (SageSdataQuellen.FaBelegsuche, _) => fas,
            (SageSdataQuellen.Teileverwendungsnachweis, _) => pos,
            _ => new List<SageRueckmeldung>(),
        };
    }

    [Fact]
    public async Task GateAus_RedirectHomeMitWarnung_KeinAufruf()
    {
        _app.Setup(a => a.GetValueAsync(AppSettingKeys.SageSdataFaAnsichtAktiv)).ReturnsAsync((string?)null);
        var c = Controller();

        var r = await c.Index(835, CancellationToken.None);

        r.Should().BeOfType<RedirectToActionResult>().Which.ControllerName.Should().Be("Home");
        c.TempData["WarningMessage"].Should().Be("Die Sage-Live-Ansicht der FA-Struktur ist deaktiviert.");
        _client.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task NichtKonfiguriert_NenntKeys_KeinAufruf()
    {
        _svc.Setup(s => s.GetValueAsync("SData:BaseUrl")).ReturnsAsync("");
        _config["SageLagerbuchung:Username"] = null;

        var vm = await Vm(835);

        vm.NichtKonfiguriert.Should().BeEquivalentTo("SData:BaseUrl", "SageLagerbuchung:Username");
        _client.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task BaseUrlMitZugangsdaten_NichtKonfiguriert_KeinAufruf()
    {
        _svc.Setup(s => s.GetValueAsync("SData:BaseUrl")).ReturnsAsync("https://user:pw@sagetest01.ake.at:5493");

        var vm = await Vm(835);

        vm.NichtKonfiguriert.Should().ContainSingle().Which.Should().Contain("enthält Zugangsdaten").And.NotContain("pw@");
        _client.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task OhneBelId_NurFormular_KeinAufruf()
    {
        (await Vm(null)).Struktur.Should().BeNull();
        _client.Aufrufe.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UngueltigeBelId_Validierung_KeinAufruf(int belId)
    {
        var c = Controller();
        await c.Index(belId, CancellationToken.None);

        c.ModelState.IsValid.Should().BeFalse();
        _client.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task TextEingabe_Validierung_KeinAufruf()
    {
        var c = Controller();
        c.ModelState.AddModelError("belId", "The value 'abc' is not valid.");
        await c.Index(null, CancellationToken.None);

        c.ModelState["belId"]!.Errors.Single().ErrorMessage.Should().Contain("ganze Zahl");
        _client.Aufrufe.Should().BeEmpty();
    }

    [Fact]
    public async Task Struktur835_FuenfAufrufe_OrJeQuelle_TlsSchalterImZiel()
    {
        Struktur835();
        _svc.Setup(s => s.GetValueAsync(SageTlsPolicy.SettingKey)).ReturnsAsync("false");

        var vm = await Vm(835);

        _client.Aufrufe.Select(a => a.Quelle).Should().Equal(
            SageSdataQuellen.FaBelegsuche, SageSdataQuellen.FaBelegsuche, SageSdataQuellen.Teileverwendungsnachweis,
            SageSdataQuellen.RueckKorrektur, SageSdataQuellen.Fertigmeldungen);
        _client.Aufrufe[2].Where.Should().Be("BelID eq 835 or BelID eq 836 or BelID eq 837 or BelID eq 838 or BelID eq 839 or BelID eq 840 or BelID eq 841");
        _client.Aufrufe.Should().OnlyContain(a => a.Ziel.ZertifikatPruefen == false && a.Ziel.Application == "ol");
        vm.ZertifikatPruefungAus.Should().BeTrue();
        vm.Hinweis.Should().BeNull();
        vm.Fehler.Should().BeEmpty();
        vm.Struktur!.Knoten.Should().HaveCount(7);
        vm.Struktur.Meldungen.Should().BeEmpty();
        vm.Aufrufe.Should().HaveCount(5).And.OnlyContain(a => a.Erfolgreich);
        vm.Aufrufe.Should().OnlyContain(a => !a.Url!.Contains("geheim") && !a.Url.Contains("sdatauser"));
    }

    [Fact]
    public async Task SubFaEingabe_HinweisUndGanzeStruktur()
    {
        Struktur835();

        var vm = await Vm(836);

        vm.Hinweis.Should().Be("BelID 836 gehört zur Struktur 835 – angezeigt wird die ganze Struktur.");
        _client.Aufrufe[1].Where.Should().Be("StrukturID eq 835");
        vm.Struktur!.HauptFa!.BelID.Should().Be(835);
    }

    [Fact]
    public async Task UnbekannteBelId_NichtGefunden_EinAufruf()
    {
        Struktur835();

        var vm = await Vm(999999999);

        vm.Fehler.Should().ContainSingle().Which.Should().Be("BelID 999999999 in Sage nicht gefunden.");
        vm.Aufrufe.Should().ContainSingle();
    }

    [Fact]
    public async Task StrukturIdNullOderKleiner_Meldung_KeineFolgeaufrufe()
    {
        _client.Antwort = (_, _) => new List<SageFaKopf> { new(5, 0, null, null, null, null, null, null, null, null, null, null, null, null, null) };

        var vm = await Vm(5);

        vm.Fehler.Should().ContainSingle().Which.Should().Contain("ohne gültige StrukturID");
        _client.Aufrufe.Should().ContainSingle();
    }

    [Fact]
    public async Task VerbindungsfehlerBeiPositionen_RestUebersprungen()
    {
        Struktur835((q, _) => q == SageSdataQuellen.Teileverwendungsnachweis ? 0 : null);

        var vm = await Vm(835);

        _client.Aufrufe.Should().HaveCount(3, "nach dem Verbindungsfehler geht nichts mehr raus");
        vm.Aufrufe.Should().HaveCount(5);
        vm.Aufrufe.TakeLast(2).Should().OnlyContain(a => a.Uebersprungen);
        vm.Fehler.Should().ContainSingle().Which.Should().StartWith("Sage nicht erreichbar");
        vm.PositionenNichtGeladen.Should().HaveCount(7);
        vm.Struktur.Should().NotBeNull("die Struktur selbst ist geladen");
    }

    [Fact]
    public async Task VerbindungsfehlerBeiSuche_AlleWeiterenUebersprungen()
    {
        _client.Antwort = (_, _) => 0;

        var vm = await Vm(835);

        _client.Aufrufe.Should().ContainSingle();
        vm.Aufrufe.Should().HaveCount(5);
        vm.Aufrufe.Skip(1).Should().OnlyContain(a => a.Uebersprungen);
        vm.Struktur.Should().BeNull();
    }

    [Fact]
    public async Task Http403BeiRueckmeldungen_BrichtNichtAb()
    {
        Struktur835((q, _) => q == SageSdataQuellen.RueckKorrektur ? 403 : null);

        var vm = await Vm(835);

        _client.Aufrufe.Should().HaveCount(5);
        vm.Fehler.Should().ContainSingle().Which.Should().Contain("Rückmeldungen: HTTP 403");
        vm.RueckmeldungenNichtGeladen[835].Should().Contain("Rückmeldungen nicht geladen (HTTP 403)");
        vm.PositionenNichtGeladen.Should().BeEmpty();
        vm.Struktur!.Knoten.Should().HaveCount(7);
    }

    [Fact]
    public async Task MehrAlsFuenfzigFas_JeQuelleZweiBloecke()
    {
        var fas = Enumerable.Range(1000, 60)
            .Select(id => new SageFaKopf(id, 1000, $"A{id}", null, 1, 1, 0, -1, 0, 0, 0, id == 1000 ? "VK" : "FA", null, null, null)).ToList();
        _client.Antwort = (q, w) => q == SageSdataQuellen.FaBelegsuche
            ? (w == "BelID eq 1000" ? fas.Take(1).ToList() : fas)
            : null;

        var vm = await Vm(1000);

        _client.Aufrufe.Should().HaveCount(2 + 3 * 2);
        _client.Aufrufe.Where(a => a.Quelle == SageSdataQuellen.Teileverwendungsnachweis).Select(a => a.Where.Split(" or ").Length).Should().Equal(50, 10);
        vm.Struktur!.HauptFa!.VerursacherTyp.Should().Be("VK");
    }
}
