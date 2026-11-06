using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IdealAkeWms.Services.SageSdata;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services.SageSdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync, E0 (AK 5, AK 6): Paging-Lesemethode <see cref="SageSdataReadClient.QueryAllAsync{T}"/>
/// gegen einen gefaelschten HttpMessageHandler — geprueft werden die tatsaechlich gesendeten Anfragen.
/// </summary>
public class SageSdataPagingTests
{
    public sealed record Platz(int PlatzID, string? Kurzbezeichnung);

    private sealed class Stub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _antwort;
        public List<HttpRequestMessage> Anfragen { get; } = new();
        public Stub(Func<HttpRequestMessage, int, HttpResponseMessage> antwort) => _antwort = antwort;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Anfragen.Add(request);
            return Task.FromResult(_antwort(request, Anfragen.Count));
        }
    }

    private static readonly SdataLeseZiel Ziel = new("https://sagetest01.ake.at:5493", "ol", "ake_TEST2026;1", "u", "p", true);

    private static (SageSdataReadClient Client, Stub Stub) Erzeuge(Func<HttpRequestMessage, int, HttpResponseMessage> antwort)
    {
        var stub = new Stub(antwort);
        return (new SageSdataReadClient(new HttpClient(stub), NullLogger<SageSdataReadClient>.Instance) { WiederholungsPause = TimeSpan.Zero }, stub);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string Feed(int? total, IEnumerable<int> ids) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["$totalResults"] = total,
            ["$itemsPerPage"] = 2,
            ["$resources"] = ids.Select(i => new { PlatzID = i, Kurzbezeichnung = $"P{i}" }),
        });

    private static int StartIndex(HttpRequestMessage r) =>
        int.Parse(r.RequestUri!.Query.Split('&').Single(p => p.StartsWith("startIndex=")).Split('=')[1]);

    private static Task<SdataSeiten<Platz>> Lies(SageSdataReadClient c, string ressource = "LagerplaetzeInfo", string? where = null, string orderBy = "PlatzID") =>
        c.QueryAllAsync<Platz>(Ziel, SageSdataReadClient.VertragResources, ressource, where, "PlatzID,Kurzbezeichnung", orderBy, CancellationToken.None, count: 2);

    [Fact]
    public async Task Paging_DreiSeiten_BisTotalResults()
    {
        var (client, stub) = Erzeuge((r, _) => Json(HttpStatusCode.OK, Feed(6, Enumerable.Range(StartIndex(r) + 1, 2))));

        var e = await Lies(client);

        e.Zeilen.Select(z => z.PlatzID).Should().Equal(1, 2, 3, 4, 5, 6);
        e.Seiten.Should().Be(3);
        e.Gesamt.Should().Be(6);
        stub.Anfragen.Select(StartIndex).Should().Equal(0, 2, 4);
        var erste = stub.Anfragen[0];
        erste.RequestUri!.AbsoluteUri.Should().Be(
            "https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/LagerplaetzeInfo?format=json&orderBy=PlatzID&count=2&startIndex=0&select=PlatzID,Kurzbezeichnung");
        erste.Headers.Accept.Select(a => a.MediaType).Should().Equal("application/json");
        erste.Headers.Authorization!.Scheme.Should().Be("Basic");
        e.Zusammenfassung.Should().Contain("LagerplaetzeInfo").And.Contain("3 Seite(n)").And.Contain("6 Zeile(n)");
    }

    [Fact]
    public async Task Paging_OhneTotalResults_EndetBeiKurzerSeite()
    {
        var (client, stub) = Erzeuge((r, n) => Json(HttpStatusCode.OK, Feed(null, n == 1 ? new[] { 1, 2 } : new[] { 3 })));

        var e = await Lies(client);

        e.Zeilen.Should().HaveCount(3);
        stub.Anfragen.Should().HaveCount(2);
    }

    [Fact]
    public async Task Paging_LeereSeiteVorTotalResults_Wirft()
    {
        var (client, _) = Erzeuge((_, n) => Json(HttpStatusCode.OK, Feed(5, n == 1 ? new[] { 1, 2 } : Array.Empty<int>())));

        var act = () => Lies(client);

        (await act.Should().ThrowAsync<SdataLeseException>()).Which.Message.Should().Contain("2 von 5");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task OrderBy_Pflicht(string orderBy)
    {
        var (client, stub) = Erzeuge((_, _) => Json(HttpStatusCode.OK, Feed(0, Array.Empty<int>())));

        var act = () => Lies(client, orderBy: orderBy);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("orderBy");
        stub.Anfragen.Should().BeEmpty();
    }

    [Theory]
    [InlineData("VerkaufPositionenAllInfo")]
    [InlineData("einkaufpositionenallinfo")]
    public async Task Sperrliste_OhneWhere_VorDemAufrufAbgewiesen(string ressource)
    {
        var (client, stub) = Erzeuge((_, _) => Json(HttpStatusCode.OK, Feed(0, Array.Empty<int>())));

        await ((Func<Task>)(() => Lies(client, ressource))).Should().ThrowAsync<ArgumentException>().WithParameterName("where");
        stub.Anfragen.Should().BeEmpty();

        await Lies(client, ressource, where: "BelID eq 1");
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task OhneWhere_UeberGrenze_AbbruchNachErsterSeite()
    {
        var (client, stub) = Erzeuge((r, _) => Json(HttpStatusCode.OK, Feed(SageSdataReadClient.MaxZeilenOhneWhere + 1, new[] { 1, 2 })));

        var act = () => Lies(client);

        (await act.Should().ThrowAsync<SdataLeseException>()).Which.Message.Should().Contain("ohne where");
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task MitWhere_UeberGrenze_LiestWeiter()
    {
        var (client, _) = Erzeuge((r, n) => Json(HttpStatusCode.OK, Feed(n == 1 ? SageSdataReadClient.MaxZeilenOhneWhere + 1 : 3, n == 1 ? new[] { 1, 2 } : new[] { 3 })));

        var e = await Lies(client, where: "Aktiv eq -1");

        e.Zeilen.Should().HaveCount(3);
    }

    [Fact]
    public async Task Http500_MitDiagnoses_WirftMitSqlText_OhneWiederholung()
    {
        const string body = "{\"$diagnoses\":[{\"severity\":\"error\",\"message\":\"Ungültiger Spaltenname 'Foo'. SELECT ... FROM KHKLagerplaetze\"}]}";
        var (client, stub) = Erzeuge((_, _) => Json(HttpStatusCode.InternalServerError, body));

        var act = () => Lies(client);

        var ex = (await act.Should().ThrowAsync<SdataLeseException>()).Which;
        ex.Status.Should().Be(500);
        ex.Ressource.Should().Be("LagerplaetzeInfo");
        ex.Diagnose.Should().Contain("Ungültiger Spaltenname 'Foo'").And.Contain("KHKLagerplaetze");
        ex.Message.Should().Contain("HTTP 500").And.Contain("LagerplaetzeInfo");
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task Http401_Wirft_OhneZugangsdatenInMeldung()
    {
        var (client, stub) = Erzeuge((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("<html>Login</html>") });

        var act = () => Lies(client);

        var ex = (await act.Should().ThrowAsync<SdataLeseException>()).Which;
        ex.Status.Should().Be(401);
        ex.Message.Should().NotContain("u:p");
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task Html_StattJson_MitStatus200_Wirft()
    {
        var (client, _) = Erzeuge((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>Proxy</html>", Encoding.UTF8, "text/html") });

        var act = () => Lies(client);

        (await act.Should().ThrowAsync<SdataLeseException>()).Which.Message.Should().Contain("nicht lesbar");
    }

    [Fact]
    public async Task Json_OhneResources_Wirft()
    {
        var (client, _) = Erzeuge((_, _) => Json(HttpStatusCode.OK, "{\"$totalResults\":0}"));

        await ((Func<Task>)(() => Lies(client))).Should().ThrowAsync<SdataLeseException>();
    }

    [Fact]
    public async Task Http503_ZweiMalWiederholt_DannErfolg()
    {
        var (client, stub) = Erzeuge((_, n) => n <= 2 ? Json(HttpStatusCode.ServiceUnavailable, "") : Json(HttpStatusCode.OK, Feed(1, new[] { 1 })));

        var e = await Lies(client);

        e.Zeilen.Should().ContainSingle();
        stub.Anfragen.Should().HaveCount(3);
    }

    [Fact]
    public async Task Http503_Dauerhaft_WirftNachDreiVersuchen()
    {
        var (client, stub) = Erzeuge((_, _) => Json(HttpStatusCode.ServiceUnavailable, ""));

        (await ((Func<Task>)(() => Lies(client))).Should().ThrowAsync<SdataLeseException>()).Which.Status.Should().Be(503);
        stub.Anfragen.Should().HaveCount(1 + SageSdataReadClient.MaxWiederholungen);
    }

    [Fact]
    public async Task Timeout_ZweiMalWiederholt_DannWirft()
    {
        var (client, stub) = Erzeuge((_, _) => throw new TaskCanceledException("Timeout"));

        (await ((Func<Task>)(() => Lies(client))).Should().ThrowAsync<SdataLeseException>()).Which.Message.Should().Contain("Zeitüberschreitung");
        stub.Anfragen.Should().HaveCount(3);
    }

    [Fact]
    public async Task Verbindungsfehler_Wirft_OhneWiederholung()
    {
        var (client, stub) = Erzeuge((_, _) => throw new HttpRequestException("Host nicht erreichbar"));

        (await ((Func<Task>)(() => Lies(client))).Should().ThrowAsync<SdataLeseException>()).Which.Status.Should().Be(0);
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task Abbruch_DurchAufrufer_WirdNichtWiederholt()
    {
        using var cts = new CancellationTokenSource();
        var (client, stub) = Erzeuge((_, _) => { cts.Cancel(); throw new TaskCanceledException(); });

        var act = () => client.QueryAllAsync<Platz>(Ziel, SageSdataReadClient.VertragResources, "LagerplaetzeInfo", null, null, "PlatzID", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        stub.Anfragen.Should().ContainSingle();
    }

    [Fact]
    public async Task Where_RautePlusUndProzent_WerdenKodiert_ApostrophBleibtLiteral()
    {
        var (client, stub) = Erzeuge((_, _) => Json(HttpStatusCode.OK, Feed(0, Array.Empty<int>())));

        await client.QueryAllAsync<Platz>(Ziel, SageSdataReadClient.VertragResources, "LagerplatzbuchungenInfo",
            "Memo like 'IdealAkeWms Lagerbuchung SM#5#%' and Menge eq 1+1", "Memo", "BuchungsID", CancellationToken.None);

        var uri = stub.Anfragen.Single().RequestUri!;
        uri.Fragment.Should().BeEmpty();
        uri.AbsoluteUri.Should().Contain("where=Memo%20like%20'IdealAkeWms%20Lagerbuchung%20SM%235%23%25'%20and%20Menge%20eq%201%2B1");
    }

    public sealed record Typen(bool Aktiv, bool? Gesperrt, bool Wahr, decimal? Menge, decimal Preis);

    [Fact]
    public async Task SageBoolean_UndDezimal_Robust()
    {
        const string body = "{\"$totalResults\":\"2\",\"$resources\":["
            + "{\"Aktiv\":-1,\"Gesperrt\":0,\"Wahr\":true,\"Menge\":\"12.5\",\"Preis\":3.75},"
            + "{\"Aktiv\":\"0\",\"Gesperrt\":null,\"Wahr\":\"-1\",\"Menge\":null,\"Preis\":\"0\"}]}";
        var (client, _) = Erzeuge((_, _) => Json(HttpStatusCode.OK, body));

        var e = await client.QueryAllAsync<Typen>(Ziel, SageSdataReadClient.VertragResources, "Artikel", null, null, "Artikelnummer", CancellationToken.None);

        e.Gesamt.Should().Be(2);
        e.Zeilen.Should().Equal(new Typen(true, false, true, 12.5m, 3.75m), new Typen(false, null, true, null, 0m));
    }

    [Fact]
    public async Task UnbekannterBoolWert_Wirft()
    {
        var (client, _) = Erzeuge((_, _) => Json(HttpStatusCode.OK, "{\"$resources\":[{\"Aktiv\":\"ja\",\"Wahr\":1,\"Preis\":1}]}"));

        await ((Func<Task>)(() => client.QueryAllAsync<Typen>(Ziel, SageSdataReadClient.VertragResources, "Artikel", null, null, "x", CancellationToken.None)))
            .Should().ThrowAsync<SdataLeseException>();
    }

    [Fact]
    public void Specific_Vertrag_ImPfad()
    {
        SageSdataReadClient.BuildQueryUrl(Ziel, SageSdataReadClient.VertragResourcesSpecific, "PpsRessourcenListenInfo", null, null, "Position", 500, 0)
            .Should().Be("https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServicesResourcesSpecific/ake_TEST2026;1/PpsRessourcenListenInfo?format=json&orderBy=Position&count=500&startIndex=0");
    }
}
