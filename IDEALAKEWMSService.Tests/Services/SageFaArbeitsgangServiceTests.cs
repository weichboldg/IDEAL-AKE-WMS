using System.Net;
using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

/// <summary>v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen E1a): Sende-Lauf + Client NUR mit Fake-HTTP — kein echter POST.</summary>
public class SageFaArbeitsgangServiceTests
{
    private static readonly SageBookingEndpoint Endpoint = new("https://sage.invalid", "ol", "CommonWawiServices", "IDEAL;1", "u", "p");
    private static readonly SageFaAgWiederholung Wdh = new(3, 15);

    private sealed class FakeClient : ISageFaArbeitsgangClient
    {
        public List<SageFaArbeitsgangRequest> Gesendet { get; } = new();
        public Func<SageFaArbeitsgangRequest, SageFaArbeitsgangSendResult> Antwort { get; set; } = _ => new(true, false, "{}", null);
        public Task<SageFaArbeitsgangSendResult> SendAsync(SageFaArbeitsgangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
        {
            Gesendet.Add(request);
            return Task.FromResult(Antwort(request));
        }
    }

    private sealed class FakeLookup : ISageFaStandLookup
    {
        public Dictionary<int, SageFaArbeitsgaenge.FaStand> Stand { get; } = new();
        public bool Wirft { get; set; }
        public int? WirftAbAufruf { get; set; }
        public int Aufrufe { get; private set; }
        public Task<IReadOnlyDictionary<int, SageFaArbeitsgaenge.FaStand>> LesenAsync(IReadOnlyCollection<int> belIds, CancellationToken ct)
        {
            Aufrufe++;
            if (Wirft || Aufrufe >= WirftAbAufruf) throw new IdealAkeWms.Services.SageSdata.SdataLeseException("PpsFaBelegeInfo", 503, "Sage nicht erreichbar");
            return Task.FromResult<IReadOnlyDictionary<int, SageFaArbeitsgaenge.FaStand>>(Stand);
        }
    }

    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly FakeClient _client = new();
    private readonly FakeLookup _lookup = new();
    private readonly FakeSyncLogger _log = new();
    private readonly Mock<ISyncErrorNotifier> _mail = new();

    // Standard: Sage nimmt an und die AG steht beim Nachlesen am FA (QA F3)
    public SageFaArbeitsgangServiceTests() => _client.Antwort = Anlegen;

    private SageFaArbeitsgangSendResult Anlegen(SageFaArbeitsgangRequest r)
    {
        ((HashSet<string>)_lookup.Stand[r.BelId].AgNummern).Add(r.SageArbeitsgangnummer);
        return new(true, false, "{}", null);
    }

    private SageFaArbeitsgangService Svc() =>
        new(_db, _client, _lookup, _mail.Object, NullLogger<SageFaArbeitsgangService>.Instance, _log);

    private SageFaArbeitsgangAnlage Zeile(int belId, string ag, int pos, SageFaArbeitsgangStatus status = SageFaArbeitsgangStatus.Offen,
        int versuche = 0, string? fehler = null, DateTime? geaendert = null)
    {
        var a = new SageFaArbeitsgangAnlage
        {
            BelId = belId, HauptFa = belId, Kuerzel = "K" + ag, SageArbeitsgangnummer = ag, Reihenfolge = pos, Status = status, Versuche = versuche,
            Fehler = fehler, CreatedAt = DateTime.Now.AddHours(-2), CreatedBy = "t", CreatedByWindows = "t", ModifiedAt = geaendert,
        };
        _db.SageFaArbeitsgangAnlagen.Add(a);
        _db.SaveChanges();
        return a;
    }

    private static SageFaArbeitsgaenge.FaStand Offen(params string[] ags) => new(true, false, false, 4m, new HashSet<string>(ags));

    [Fact]
    public async Task OhneArbeit_KeinProtokoll_KeinSageAufruf()
    {
        (await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh)).Should().Be(0);
        _log.Runs.Should().BeEmpty();
        _lookup.Aufrufe.Should().Be(0);
    }

    [Fact]
    public async Task Senden_InReihenfolge_MitFertigungsmenge_Bearbeiter_StatusUndCounts()
    {
        Zeile(10, "320", 6);
        Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();

        (await Svc().RunOnceAsync(Endpoint, " BEA ", 10, Wdh)).Should().Be(2);

        _client.Gesendet.Select(r => (r.BelId, r.SageArbeitsgangnummer, r.Menge, r.Bearbeiter)).Should().Equal((10, "180", 4m, "BEA"), (10, "320", 4m, "BEA"));
        _db.SageFaArbeitsgangAnlagen.Should().OnlyContain(a => a.Status == SageFaArbeitsgangStatus.Angelegt && a.Versuche == 1 && a.SageAm != null
            && a.ModifiedBy == SageFaArbeitsgaenge.ServiceActor && a.ModifiedByWindows == SageFaArbeitsgaenge.ServiceActor);
        var run = _log.Runs.Single();
        run.ServiceName.Should().Be(SyncLogServices.SageFaArbeitsgaenge);
        run.FinalCounts.Should().BeEquivalentTo(new Dictionary<string, int> { ["Angelegt"] = 2, ["Übersprungen"] = 0, ["Fehler"] = 0 });
        _mail.Verify(m => m.NotifyAsync(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgSchonInSage_KeinPost_InSageVorhanden()
    {
        Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen("180");
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        _client.Gesendet.Should().BeEmpty();
        _db.SageFaArbeitsgangAnlagen.Single().Status.Should().Be(SageFaArbeitsgangStatus.InSageVorhanden);
        _log.Runs.Single().FinalCounts!["Übersprungen"].Should().Be(1);
    }

    [Theory]
    [InlineData("fehlt")]
    [InlineData("beendet")]
    [InlineData("begonnen")]
    public async Task GesperrterFa_KeinPost_Fehler_OhneVersuch(string fall)
    {
        Zeile(10, "180", 1);
        if (fall == "beendet") _lookup.Stand[10] = new(true, true, false, 4m, new HashSet<string>());
        if (fall == "begonnen") _lookup.Stand[10] = new(true, false, true, 4m, new HashSet<string>());

        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);

        _client.Gesendet.Should().BeEmpty();
        var a = _db.SageFaArbeitsgangAnlagen.Single();
        a.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
        a.Versuche.Should().Be(0);   // keine automatische Wiederholung
        a.Fehler.Should().Contain("nicht gesendet");
    }

    [Fact]
    public async Task BearbeiterLeer_NichtsGesendet_KeinSageLesen_Fehler_Mail()
    {
        Zeile(10, "180", 1);
        await Svc().RunOnceAsync(Endpoint, "  ", 10, Wdh);
        _client.Gesendet.Should().BeEmpty();
        _lookup.Aufrufe.Should().Be(0);
        _db.SageFaArbeitsgangAnlagen.Single().Fehler.Should().Be(SageFaArbeitsgaenge.BearbeiterFehlt);
        _mail.Verify(m => m.NotifyAsync("Sage-FA-Arbeitsgänge", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SageNichtLesbar_NichtsGesendet_VersuchGezaehlt()
    {
        Zeile(10, "180", 1);
        _lookup.Wirft = true;
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        _client.Gesendet.Should().BeEmpty();
        var a = _db.SageFaArbeitsgangAnlagen.Single();
        (a.Status, a.Versuche).Should().Be((SageFaArbeitsgangStatus.Fehler, 1));
        a.Fehler.Should().StartWith("Sage-Prüfung vor dem Senden fehlgeschlagen");
    }

    [Fact]
    public async Task SageFehler_Fehler_UndAutomatischWiederholt_NurNachAbstand_UndBisMax()
    {
        var a = Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();
        _client.Antwort = _ => new(false, false, "{\"ErrorText\":\"ValidateMitarbeiter\"}", "HTTP 500 Internal Server Error: ValidateMitarbeiter");
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        a.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
        a.Fehler.Should().Contain("ValidateMitarbeiter");

        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);          // Abstand noch nicht um
        _client.Gesendet.Should().HaveCount(1);

        a.ModifiedAt = DateTime.Now.AddMinutes(-20);
        _db.SaveChanges();
        _client.Antwort = Anlegen;
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        _client.Gesendet.Should().HaveCount(2);
        a.Status.Should().Be(SageFaArbeitsgangStatus.Angelegt);

        var b = Zeile(11, "180", 1, SageFaArbeitsgangStatus.Fehler, versuche: 4, fehler: "x", geaendert: DateTime.Now.AddHours(-1));
        _lookup.Stand[11] = Offen();
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);          // Versuche 4 > Max 3
        _client.Gesendet.Should().HaveCount(2);
        b.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
    }

    [Fact]
    public async Task Unklar_UndHaengend_NieAutomatischWiederholt()
    {
        var a = Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();
        _client.Antwort = _ => new(false, true, null, "Timeout");
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        a.Fehler.Should().StartWith(SageFaArbeitsgaenge.UnklarText);

        var h = Zeile(11, "180", 1, SageFaArbeitsgangStatus.Gesendet, versuche: 1, geaendert: DateTime.Now.AddHours(-1));
        a.ModifiedAt = DateTime.Now.AddHours(-1);
        _db.SaveChanges();
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        h.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
        h.Fehler.Should().Be(SageFaArbeitsgaenge.UnklarText);

        h.ModifiedAt = DateTime.Now.AddHours(-1);
        _db.SaveChanges();
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        _client.Gesendet.Should().HaveCount(1);                      // weder a noch h erneut gesendet
    }

    [Fact]
    public async Task Nachlesen_AgGefunden_Angelegt_NachlesenPerGet()
    {
        Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();
        (await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh)).Should().Be(1);
        _lookup.Aufrufe.Should().Be(2);                              // vor dem Senden + Nachlesen
        _db.SageFaArbeitsgangAnlagen.Single().Status.Should().Be(SageFaArbeitsgangStatus.Angelegt);
    }

    [Fact]
    public async Task Nachlesen_KeinFund_TrotzErfolgsantwort_FehlerMitText()
    {
        var a = Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();
        _client.Antwort = _ => new(true, false, "{\"Handle\":10}", null);   // 2xx ohne ErrorText, AG aber nicht angelegt
        (await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh)).Should().Be(0);
        a.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
        a.Fehler.Should().Contain("steht beim Nachlesen nicht am FA").And.NotStartWith(SageFaArbeitsgaenge.UnklarText);
        _log.Runs.Single().FinalCounts!["Fehler"].Should().Be(1);
        _mail.Verify(m => m.NotifyAsync("Sage-FA-Arbeitsgänge", It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nachlesen_Lesefehler_Unklar_NieAutomatischWiederholt()
    {
        var a = Zeile(10, "180", 1);
        _lookup.Stand[10] = Offen();
        _lookup.WirftAbAufruf = 2;                                   // Pruefung vor dem Senden ok, Nachlesen wirft
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        a.Status.Should().Be(SageFaArbeitsgangStatus.Fehler);
        a.Fehler.Should().StartWith(SageFaArbeitsgaenge.UnklarText).And.Contain("Nachlesen nach dem Senden fehlgeschlagen");

        _lookup.WirftAbAufruf = null;
        a.ModifiedAt = DateTime.Now.AddHours(-1);
        _db.SaveChanges();
        await Svc().RunOnceAsync(Endpoint, "BEA", 10, Wdh);
        _client.Gesendet.Should().HaveCount(1);
    }

    [Fact]
    public void DI_LoestAuf_LogNameRegistriert_SyncLoggerLetzterParameter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(new Mock<ISyncLogger>().Object);
        services.AddSingleton(new Mock<ISyncErrorNotifier>().Object);
        services.AddScoped(_ => TestDbContextFactory.Create());
        services.AddHttpClient<ISageFaArbeitsgangClient, SageFaArbeitsgangClient>();
        services.AddScoped<IdealAkeWms.Services.SageSdata.ISageSdataReadClient>(_ => new Mock<IdealAkeWms.Services.SageSdata.ISageSdataReadClient>().Object);
        services.AddScoped<ISageFaStandLookup, SdataFaStandLookup>();
        services.AddScoped<SageFaArbeitsgangService>();
        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var s = sp.CreateScope();
        s.ServiceProvider.GetRequiredService<SageFaArbeitsgangService>().Should().NotBeNull();
        SyncLogServices.All.Should().Contain(SyncLogServices.SageFaArbeitsgaenge);
        SyncLogServices.DisplayNames[SyncLogServices.SageFaArbeitsgaenge].Should().Be("Sage-FA-Arbeitsgänge");
        typeof(SageFaArbeitsgangService).GetConstructors().Single().GetParameters().Last().ParameterType.Should().Be(typeof(ISyncLogger));
    }

    // ------------------------------------------------------------------ Client mit Fake-HTTP ------------------------------------------

    private sealed class FakeHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Antwort { get; set; } = _ => new(HttpStatusCode.OK);
        public List<(HttpMethod Methode, string Url, string Body)> Anfragen { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Anfragen.Add((request.Method, request.RequestUri!.ToString(), request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return Antwort(request);
        }
    }

    private static (SageFaArbeitsgangClient, FakeHandler) Client(Func<HttpRequestMessage, HttpResponseMessage> antwort)
    {
        var h = new FakeHandler { Antwort = antwort };
        return (new SageFaArbeitsgangClient(new HttpClient(h), NullLogger<SageFaArbeitsgangClient>.Instance), h);
    }

    [Fact]
    public async Task Client_PostAnFertigungsauftragService_PayloadServiceOption4_RessourceTyp1()
    {
        var (c, h) = Client(_ => new(HttpStatusCode.OK) { Content = new StringContent("{\"Handle\":1059463,\"ResultNumber\":200,\"ErrorText\":\"\"}") });
        var r = await c.SendAsync(new SageFaArbeitsgangRequest(1059463, "180", 2.5m, "BEA"), Endpoint);

        r.Success.Should().BeTrue();
        var (methode, url, body) = h.Anfragen.Single();
        methode.Should().Be(HttpMethod.Post);
        url.Should().Be("https://sage.invalid/sdata/ol/CommonWawiServices/IDEAL;1/$service/FertigungsauftragService");
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("ServiceOption").GetInt32().Should().Be(4);
        var pos = doc.RootElement.GetProperty(SageFaArbeitsgangClient.FeldPositionen)[0];
        pos.GetProperty("RessourceTyp").GetInt32().Should().Be(1);
        pos.GetProperty("RessourceNummer").GetString().Should().Be("180");
        pos.GetProperty("Menge").GetDecimal().Should().Be(2.5m);
        pos.GetProperty("Bearbeiter").GetString().Should().Be("BEA");
        body.Should().NotContain("\"p\"");   // kein Kennwort im Body
    }

    [Fact]
    public async Task Client_Http500_MitErrorText_IstFehler_NichtUnklar()
    {
        var (c, _) = Client(_ => new(HttpStatusCode.InternalServerError)
            { Content = new StringContent("<response><ResultNumber>500</ResultNumber><ErrorText>ValidateMitarbeiter</ErrorText></response>") });
        var r = await c.SendAsync(new SageFaArbeitsgangRequest(1, "180", 1m, "X"), Endpoint);
        (r.Success, r.Unklar).Should().Be((false, false));
        r.Error.Should().Contain("HTTP 500").And.Contain("ValidateMitarbeiter");
    }

    [Fact]
    public async Task Client_Timeout_IstUnklar()
    {
        var (c, _) = Client(_ => throw new TaskCanceledException("Timeout"));
        var r = await c.SendAsync(new SageFaArbeitsgangRequest(1, "180", 1m, "X"), Endpoint);
        (r.Success, r.Unklar).Should().Be((false, true));
    }

    [Fact]
    public void Auswerten_OhneErrorText_VorlaeufigErfolg_MitErrorText_Fehler_XmlUndJson()
    {
        // QA F3: ohne ErrorText nur vorlaeufig erfolgreich — angelegt entscheidet erst das Nachlesen im Service
        SageFaArbeitsgangClient.Auswerten("{\"ResultNumber\":200}").Should().Match<SageFaArbeitsgangSendResult>(r => r.Success && !r.Unklar);
        SageFaArbeitsgangClient.Auswerten("<a><response><Handle>5</Handle><ErrorText></ErrorText></response></a>").Success.Should().BeTrue();
        SageFaArbeitsgangClient.Auswerten("{\"response\":{\"Handle\":5,\"ErrorText\":\"kaputt\"}}")
            .Should().Match<SageFaArbeitsgangSendResult>(r => !r.Success && !r.Unklar && r.Error == "kaputt");
        SageFaArbeitsgangClient.Auswerten("kein json").Should().Match<SageFaArbeitsgangSendResult>(r => r.Success && !r.Unklar);
    }
}
