using System.Net;
using System.Net.Security;
using System.Text;
using FluentAssertions;
using IdealAkeWms.Services.SageSdata;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services.SageSdata;

/// <summary>Erster HTTP-Client-Test im Repo: Stub-Handler statt Netz; geprueft wird die tatsaechlich gesendete Anfrage.</summary>
public class SageSdataReadClientTests
{
    private sealed class Stub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _antwort;
        public List<HttpRequestMessage> Anfragen { get; } = new();
        public Stub(Func<HttpRequestMessage, HttpResponseMessage> antwort) => _antwort = antwort;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Anfragen.Add(request);
            return Task.FromResult(_antwort(request));
        }
    }

    private static readonly SdataLeseZiel Ziel = new("https://sagetest01.ake.at:5493/", "ol", "ake_TEST2026;1", "sdatauser", "geheim!Pw", true);

    private static (SageSdataReadClient Client, Stub Stub) Erzeuge(HttpStatusCode status, string body) =>
        Erzeuge(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

    private static (SageSdataReadClient Client, Stub Stub) Erzeuge(Func<HttpRequestMessage, HttpResponseMessage> antwort)
    {
        var stub = new Stub(antwort);
        return (new SageSdataReadClient(new HttpClient(stub) { Timeout = TimeSpan.FromSeconds(20) }, NullLogger<SageSdataReadClient>.Instance), stub);
    }

    [Fact]
    public async Task Struktur_GesendeteUrl_MethodeUndAuth()
    {
        var (client, stub) = Erzeuge(HttpStatusCode.OK, SageSdataFixtures.Feed(SageSdataFixtures.Struktur835));

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "StrukturID eq 835", "BelID,StrukturID", "BelID", CancellationToken.None);

        var req = stub.Anfragen.Single();
        req.Method.Should().Be(HttpMethod.Get);
        req.RequestUri!.AbsoluteUri.Should().Be(
            "https://sagetest01.ake.at:5493/sdata/ol/RealTimeData/ake_TEST2026;1/dtsFaBelegsuche.Sage.PPS?where=StrukturID%20eq%20835&select=BelID,StrukturID&orderBy=BelID&format=json");
        req.Headers.Authorization!.Scheme.Should().Be("Basic");
        Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers.Authorization.Parameter!)).Should().Be("sdatauser:geheim!Pw");
        req.Options.TryGetValue(SageSdataReadClient.ZertifikatPruefenOption, out var pruefen).Should().BeTrue();
        pruefen.Should().BeTrue();

        e.Zeilen.Should().HaveCount(7);
        e.Call.Status.Should().Be(200);
        e.Call.Zeilen.Should().Be(7);
        e.Call.Url.Should().NotContain("sdatauser").And.NotContain("geheim");
    }

    [Fact]
    public async Task StringWhere_ApostrophUndSemikolonBleibenLiteral()
    {
        var (client, stub) = Erzeuge(HttpStatusCode.OK, "{\"$resources\":[]}");

        await client.GetAsync<SageFaPosition>(Ziel, SageSdataQuellen.Teileverwendungsnachweis, "KopfArtikelnummer eq '86003871' or BelID eq 836", "BelID", null, CancellationToken.None);

        var uri = stub.Anfragen.Single().RequestUri!.AbsoluteUri;
        uri.Should().Contain("/ake_TEST2026;1/").And.Contain("where=KopfArtikelnummer%20eq%20'86003871'%20or%20BelID%20eq%20836");
        uri.Should().NotContain("%27").And.NotContain("%3B");
    }

    [Fact]
    public async Task Positionen_Fixture_DezimalUndFremdfelder()
    {
        var (client, _) = Erzeuge(HttpStatusCode.OK, SageSdataFixtures.Feed(SageSdataFixtures.Positionen, "belid_836")
            .Replace("\"BelID\": 836", "\"BelID\": 836, \"$url\": \"https://x\", \"$updated\": \"2026-10-30\""));

        var e = await client.GetAsync<SageFaPosition>(Ziel, SageSdataQuellen.Teileverwendungsnachweis, "BelID eq 836", "BelID", null, CancellationToken.None);

        e.Zeilen.Single().Menge.Should().Be(0.6m);
        e.Zeilen.Single().Ressourcenummer.Should().Be("00722001");
    }

    [Fact]
    public async Task Http403_LeereListe_StatusUndBody_OhneException()
    {
        var (client, _) = Erzeuge(HttpStatusCode.Forbidden, "kein Zugriff");

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1", "BelID", null, CancellationToken.None);

        e.Zeilen.Should().BeEmpty();
        e.Call.Status.Should().Be(403);
        e.Call.Erfolgreich.Should().BeFalse();
        e.Call.Verbindungsfehler.Should().BeFalse();
        e.Call.Rohdaten.Should().Be("kein Zugriff");
    }

    [Fact]
    public async Task Verbindungsfehler_Status0_MeldungOhneZugangsdaten()
    {
        var (client, _) = Erzeuge(_ => throw new HttpRequestException("Der angegebene Host ist unbekannt. (nirgends:5493)"));

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1", "BelID", null, CancellationToken.None);

        e.Call.Status.Should().Be(0);
        e.Call.Verbindungsfehler.Should().BeTrue();
        e.Call.Fehler.Should().Contain("Host").And.NotContain("geheim");
    }

    [Fact]
    public async Task Timeout_Status0_Zeitueberschreitung()
    {
        var (client, _) = Erzeuge(_ => throw new TaskCanceledException("timeout"));

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1", "BelID", null, CancellationToken.None);

        e.Call.Status.Should().Be(0);
        e.Call.Fehler.Should().Contain("Zeitüberschreitung nach 20 s");
    }

    [Fact]
    public async Task UnlesbaresJson_KeineException()
    {
        var (client, _) = Erzeuge(HttpStatusCode.OK, "<html>Login</html>");

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1", "BelID", null, CancellationToken.None);

        e.Zeilen.Should().BeEmpty();
        e.Call.Fehler.Should().StartWith("Antwort nicht lesbar");
        e.Call.Erfolgreich.Should().BeFalse("ein unlesbarer 200-Body darf nicht als 'nicht gefunden' erscheinen (QA Q-1)");
        e.Call.Verbindungsfehler.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task LeeresWhere_ArgumentException_OhneRequest(string where)
    {
        var (client, stub) = Erzeuge(HttpStatusCode.OK, "{}");

        var act = () => client.GetAsync<SageFaPosition>(Ziel, SageSdataQuellen.Teileverwendungsnachweis, where, "BelID", null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        stub.Anfragen.Should().BeEmpty();
    }

    [Fact]
    public async Task Rohdaten_AufViertausendZeichenGekuerzt()
    {
        var (client, _) = Erzeuge(HttpStatusCode.InternalServerError, new string('x', 5000));

        var e = await client.GetAsync<SageFaKopf>(Ziel, SageSdataQuellen.FaBelegsuche, "BelID eq 1", "BelID", null, CancellationToken.None);

        e.Call.Rohdaten!.Length.Should().Be(4000);
    }

    [Fact]
    public void ZertifikatsCallback_LiestNurDieOptionDerAnfrage_DefaultPruefen()
    {
        var ohne = new HttpRequestMessage();
        var aus = new HttpRequestMessage();
        aus.Options.Set(SageSdataReadClient.ZertifikatPruefenOption, false);
        var an = new HttpRequestMessage();
        an.Options.Set(SageSdataReadClient.ZertifikatPruefenOption, true);

        SageSdataReadClient.ZertifikatAkzeptieren(ohne, SslPolicyErrors.None).Should().BeTrue();
        SageSdataReadClient.ZertifikatAkzeptieren(ohne, SslPolicyErrors.RemoteCertificateChainErrors).Should().BeFalse("fehlende Option = pruefen");
        SageSdataReadClient.ZertifikatAkzeptieren(null, SslPolicyErrors.RemoteCertificateChainErrors).Should().BeFalse();
        SageSdataReadClient.ZertifikatAkzeptieren(an, SslPolicyErrors.RemoteCertificateChainErrors).Should().BeFalse();
        SageSdataReadClient.ZertifikatAkzeptieren(aus, SslPolicyErrors.RemoteCertificateChainErrors).Should().BeTrue();
    }

    [Fact]
    public void LeseZiel_ToString_OhneZugangsdaten()
        => Ziel.ToString().Should().NotContain("geheim").And.NotContain("sdatauser");
}
