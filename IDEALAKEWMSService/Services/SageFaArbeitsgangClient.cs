using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>Eine AG-Position fuer FertigungsauftragService ServiceOption 4 (je AG ein Aufruf, damit das Ergebnis je AG eindeutig ist).</summary>
public sealed record SageFaArbeitsgangRequest(int BelId, string SageArbeitsgangnummer, decimal Menge, string Bearbeiter);

/// <summary>Ergebnis eines Sende-Versuchs. <see cref="Unklar"/> = Zeitueberschreitung/Antwort ohne Handle: nie automatisch wiederholen.</summary>
public sealed record SageFaArbeitsgangSendResult(bool Success, bool Unklar, string? ResponseRaw, string? Error);

/// <summary>v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen E1a): POST {BaseUrl}/sdata/{App}/{Contract}/{Dataset}/$service/FertigungsauftragService
/// mit ServiceOption 4 (AddOrChangePositionArbeitsgang). Nur Unit-Tests mit Fake-HTTP; der echte Schreibtest ist E1b (Freigabe Gerald).</summary>
public interface ISageFaArbeitsgangClient
{
    Task<SageFaArbeitsgangSendResult> SendAsync(SageFaArbeitsgangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default);
}

/// <summary>Kopie von <see cref="SageWareneingangClient"/> mit anderer Ressource, Payload und Auswertung.</summary>
public class SageFaArbeitsgangClient : ISageFaArbeitsgangClient
{
    public const string Resource = "$service/FertigungsauftragService";

    // ---- Payload-Feldnamen/-Werte. NICHT am IDEAL-Ziel belegt (Spec P-1..P-4) — vor E1b per $template/$schema pruefen. ----
    // Belegt nur aus $schema IDEAL Prod (Ideen-Notiz 2026-10-30, 15:44): ServiceOption 4 = AddOrChangePositionArbeitsgang,
    // FaBelegArbeitsgangPosition = Handle, PositionHandle, RessourceTyp, RessourceNummer, Menge, Bearbeiter, Userfelder.
    public const string FeldServiceOption = "ServiceOption";
    public const int ServiceOptionAgAnlegen = 4;                                   // kein 5 (Einlasten), Freigabe-Antwort 6
    public const string FeldFaHandle = "Handle";                                   // TODO(P-1): FA-Handle am Kopf = BelID?
    public const string FeldPositionen = "FaBelegArbeitsgangPositionen";           // TODO(P-1): Name der Positionsliste im Request
    public const string FeldPosHandle = "Handle";                                  // TODO(P-1): FA-Handle auch je Position?
    public const string FeldPositionHandle = "PositionHandle";
    public const int PositionHandleNeu = 0;                                        // TODO(P-1): 0 = neue Position?
    public const string FeldRessourceTyp = "RessourceTyp";
    public const int RessourceTypArbeitsgang = 1;                                  // TODO(P-2): DefaultRessourceTyp 1 (Freigabe-Antwort 1)
    public const string FeldRessourceNummer = "RessourceNummer";                   // = Sage-Arbeitsgangnummer, kein Arbeitsplatz (Freigabe-Antwort 1)
    public const string FeldMenge = "Menge";                                       // Fertigungsmenge (Sollmenge), Freigabe-Antwort 11
    public const string FeldBearbeiter = "Bearbeiter";                             // TODO(P-4): ValidateMitarbeiter-gueltiger Wert
    // TODO(P-3): WMS-Kennung im Positions-Userfeld erst, wenn ein beschreibbares Feld belegt ist.

    private readonly HttpClient _http;
    private readonly ILogger<SageFaArbeitsgangClient> _logger;

    public SageFaArbeitsgangClient(HttpClient http, ILogger<SageFaArbeitsgangClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public static string BuildServiceUrl(SageBookingEndpoint e) =>
        SdataUrlBuilder.BuildResourceUrl(e.BaseUrl, e.Application, e.ServiceContract, e.Dataset, Resource);

    public static string Serialize(SageFaArbeitsgangRequest r) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        [FeldServiceOption] = ServiceOptionAgAnlegen,
        [FeldFaHandle] = r.BelId,
        [FeldPositionen] = new[]
        {
            new Dictionary<string, object>
            {
                [FeldPosHandle] = r.BelId,
                [FeldPositionHandle] = PositionHandleNeu,
                [FeldRessourceTyp] = RessourceTypArbeitsgang,
                [FeldRessourceNummer] = r.SageArbeitsgangnummer,
                [FeldMenge] = r.Menge,
                [FeldBearbeiter] = r.Bearbeiter,
            },
        },
    });

    public async Task<SageFaArbeitsgangSendResult> SendAsync(SageFaArbeitsgangRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl) || string.IsNullOrWhiteSpace(endpoint.Application)
            || string.IsNullOrWhiteSpace(endpoint.ServiceContract) || string.IsNullOrWhiteSpace(endpoint.Dataset))
            return new(false, false, null, "SData-Konfiguration unvollstaendig (SData:BaseUrl / SData:Application / SData:ServiceContract / SData:Dataset).");

        var url = BuildServiceUrl(endpoint);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url);
            var json = Serialize(request);
            _logger.LogInformation("Sage-FA-AG POST {Url} FA {BelId} AG {Ag}: {Json}", url, request.BelId, request.SageArbeitsgangnummer, json); // ohne Zugangsdaten
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{endpoint.Username}:{endpoint.Password}")));

            using var response = await _http.SendAsync(message, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Sage-FA-AG Antwort {Status} FA {BelId} AG {Ag}: {Body}", (int)response.StatusCode, request.BelId, request.SageArbeitsgangnummer, Cap(body));
            if (!response.IsSuccessStatusCode)
            {
                // fachlicher Fehler (400/500 mit ErrorText/ResultText, Befund §11/§14): eindeutig nicht angelegt
                var text = Feld(body, "ErrorText") ?? Feld(body, "ResultText");
                return new(false, false, Cap(body), $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}" + (text is null ? "" : $": {text}"));
            }
            return Auswerten(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sende-Fehler an Sage-FertigungsauftragService ({Url}).", url);
            // Zeitueberschreitung/Verbindungsabbruch: Sage kann angelegt haben -> unklar
            return new(false, true, null, ex.Message);
        }
    }

    /// <summary>HTTP 2xx: Erfolg nur ohne ErrorText UND mit einem Handle in der Antwort (TODO(P-1): Antwortform belegen); sonst unklar.</summary>
    public static SageFaArbeitsgangSendResult Auswerten(string? body)
    {
        var fehler = Feld(body, "ErrorText");
        if (!string.IsNullOrWhiteSpace(fehler)) return new(false, false, Cap(body), fehler);
        return string.IsNullOrWhiteSpace(Feld(body, "Handle"))
            ? new(false, true, Cap(body), "Antwort von Sage ohne Handle.")
            : new(true, false, Cap(body), null);
    }

    /// <summary>Erstes Vorkommen eines Feldes in XML (SData-Atom, LocalName) oder JSON (beliebige Tiefe); null wenn nicht lesbar/leer.</summary>
    internal static string? Feld(string? body, string name)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            if (body.TrimStart().StartsWith('<'))
                return Leer(XDocument.Parse(body).Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value);
            using var doc = JsonDocument.Parse(body);
            return Leer(Suchen(doc.RootElement, name));
        }
        catch (Exception e) when (e is System.Xml.XmlException or JsonException) { return null; }
    }

    private static string? Suchen(JsonElement e, string name)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
                if (p.NameEquals(name) && p.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText();
            foreach (var p in e.EnumerateObject())
                if (Suchen(p.Value, name) is { } t) return t;
        }
        else if (e.ValueKind == JsonValueKind.Array)
            foreach (var x in e.EnumerateArray())
                if (Suchen(x, name) is { } t) return t;
        return null;
    }

    private static string? Leer(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static string? Cap(string? v) => v is null ? null : (v.Length <= 4000 ? v : v[..4000]);
}
