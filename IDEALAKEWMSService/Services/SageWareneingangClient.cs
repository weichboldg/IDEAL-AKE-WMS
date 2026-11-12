using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>Ergebnis eines Sende-Versuchs des Sage-Wareneingangsbelegs.</summary>
public sealed record SageWareneingangSendResult(bool Success, string? Belegnummer, string? ResponseRaw, string? Error);

/// <summary>v1.99.4 (Wareneingang Teil 2, E4): POST {BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/$service/StandardBelegService,
/// Basic-Auth wie die Lagerbuchung. Manual-UAT (Schreibtest), nicht InMemory-testbar.</summary>
public interface ISageWareneingangClient
{
    Task<SageWareneingangSendResult> SendAsync(SageWareneingangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default);
}

/// <summary>Kopie von <see cref="SageLagerbuchungClient"/> mit anderer Ressource und Auswertung der Beleg-Response
/// (BelegnummerFormatiert, ErrorText; Brain sage-sdata-commonwawiservices "Beleg-Response").</summary>
public class SageWareneingangClient : ISageWareneingangClient
{
    public const string Resource = "$service/StandardBelegService";
    private readonly HttpClient _http;
    private readonly ILogger<SageWareneingangClient> _logger;

    public SageWareneingangClient(HttpClient http, ILogger<SageWareneingangClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public static string BuildServiceUrl(SageBookingEndpoint e) =>
        SdataUrlBuilder.BuildResourceUrl(e.BaseUrl, e.Application, e.ServiceContract, e.Dataset, Resource);

    public static string Serialize(SageWareneingangRequest request) => JsonSerializer.Serialize(request);

    public async Task<SageWareneingangSendResult> SendAsync(SageWareneingangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl) || string.IsNullOrWhiteSpace(endpoint.Application)
            || string.IsNullOrWhiteSpace(endpoint.ServiceContract) || string.IsNullOrWhiteSpace(endpoint.Dataset))
            return new(false, null, null, "SData-Konfiguration unvollstaendig (SData:BaseUrl / SData:Application / SData:ServiceContract / SData:Dataset).");

        var url = BuildServiceUrl(endpoint);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url);
            var json = Serialize(request);
            _logger.LogInformation("Sage-Wareneingang POST {Url} Memo {Memo}: {Json}", url, request.Memo, json);   // ohne Zugangsdaten
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{endpoint.Username}:{endpoint.Password}")));

            using var response = await _http.SendAsync(message, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Sage-Wareneingang Antwort {Status} Memo {Memo}: {Body}", (int)response.StatusCode, request.Memo, Cap(body));
            if (!response.IsSuccessStatusCode)
                return new(false, null, Cap(body), $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            return Auswerten(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sende-Fehler an Sage-StandardBelegService ({Url}).", url);
            return new(false, null, null, ex.Message);
        }
    }

    /// <summary>HTTP 2xx: Erfolg nur ohne ErrorText UND mit BelegnummerFormatiert (QA 2026-11-12: ohne Belegnummer ist das Ergebnis unklar -> Fehler
    /// "bitte in Sage prüfen", nie still Uebergeben). Sage antwortet ohne format=json
    /// als SData-Atom-XML (Schreibtest 2026-11-12: &lt;entry&gt;…&lt;sdata:payload&gt;…&lt;BelegnummerFormatiert&gt;, ResultNumber 200 = Ok) - beide
    /// Formate werden gelesen; der erste ErrorText/BelegnummerFormatiert im Dokument ist der des Belegkopfs.</summary>
    public static SageWareneingangSendResult Auswerten(string? body)
    {
        string? beleg = null, fehler = null;
        if (body != null && body.TrimStart().StartsWith('<'))
        {
            try
            {
                var doc = XDocument.Parse(body);
                beleg = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "BelegnummerFormatiert")?.Value;
                fehler = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "ErrorText")?.Value;
            }
            catch (System.Xml.XmlException) { return new(false, null, Cap(body), "Antwort von Sage nicht lesbar - bitte in Sage prüfen."); }
            return Ergebnis(beleg, fehler, body);
        }
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("BelegnummerFormatiert", out var b) && b.ValueKind == JsonValueKind.String) beleg = b.GetString();
                if (doc.RootElement.TryGetProperty("ErrorText", out var f) && f.ValueKind == JsonValueKind.String) fehler = f.GetString();
            }
        }
        catch (JsonException) { return new(false, null, Cap(body), "Antwort von Sage nicht lesbar - bitte in Sage prüfen."); }
        return Ergebnis(beleg, fehler, body);
    }

    /// <summary>EINE Regel fuer XML und JSON.</summary>
    private static SageWareneingangSendResult Ergebnis(string? beleg, string? fehler, string? body) =>
        !string.IsNullOrWhiteSpace(fehler) ? new(false, beleg, Cap(body), fehler)
        : string.IsNullOrWhiteSpace(beleg) ? new(false, null, Cap(body), "Antwort von Sage ohne Belegnummer - Ergebnis unklar, bitte in Sage prüfen.")
        : new(true, beleg, Cap(body), null);

    private static string? Cap(string? v) => v is null ? null : (v.Length <= 4000 ? v : v[..4000]);
}
