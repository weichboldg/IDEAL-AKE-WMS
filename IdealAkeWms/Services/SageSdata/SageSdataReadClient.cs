using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IdealAkeWms.Services.SageSdata;

/// <summary>Reiner Lese-Client (nur GET) fuer den SData-Vertrag <c>RealTimeData</c>.</summary>
public interface ISageSdataReadClient
{
    /// <summary>
    /// <c>GET {Basis}/sdata/{App}/RealTimeData/{Dataset}/{quelle}?where=…&amp;select=…[&amp;orderBy=…]&amp;format=json</c>.
    /// Wirft nie bei HTTP- oder Verbindungsfehlern (leere Zeilen + Diagnose), nur bei leerem <paramref name="where"/>
    /// (<see cref="ArgumentException"/>) und bei Abbruch durch <paramref name="ct"/>.
    /// </summary>
    Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct);
}

/// <summary>
/// Spec 2026-10-30-sage-sdata-fa-ansicht (v1.71.0). Muster wie der Lagerbuchungs-Client des Dienstes
/// (IDEALAKEWMSService/Services/SageLagerbuchungClient.cs: Basic-Auth, kein Throw bei HTTP-Fehler, Body-Cap 4000).
/// <para>
/// <b>Kodierung:</b> <c>where</c> wird NICHT mit <see cref="Uri.EscapeDataString"/> kodiert (aus <c>'</c> wuerde <c>%27</c>);
/// die <see cref="Uri"/>-Pipeline macht Leerzeichen zu <c>%20</c> und laesst <c>'</c> und <c>;</c> literal — so wie die im
/// Browser erprobte URL. <b>TLS:</b> Der Schalter <c>SageLagerbuchungSslZertifikatPruefen</c> reist je Anfrage in
/// <see cref="HttpRequestMessage.Options"/> (<see cref="ZertifikatPruefenOption"/>); der Zertifikats-Callback in
/// <c>Program.cs</c> liest nur diese Option — kein DB-Zugriff im Handshake (Freigabe-Nachtrag 4).
/// </para>
/// </summary>
public sealed class SageSdataReadClient : ISageSdataReadClient
{
    public const string ServiceContract = "RealTimeData";
    public const int MaxRohdaten = 4000;

    /// <summary>Fehlt die Option, wird geprueft (fail-safe).</summary>
    public static readonly HttpRequestOptionsKey<bool> ZertifikatPruefenOption = new("SageSdata.ZertifikatPruefen");

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<SageSdataReadClient> _logger;

    public SageSdataReadClient(HttpClient http, ILogger<SageSdataReadClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>Zertifikatspruefung fuer eine Anfrage: ohne Fehler immer ok, sonst nur, wenn die Anfrage sie abschaltet.</summary>
    public static bool ZertifikatAkzeptieren(HttpRequestMessage? request, System.Net.Security.SslPolicyErrors errors) =>
        errors == System.Net.Security.SslPolicyErrors.None
        || (request is not null && request.Options.TryGetValue(ZertifikatPruefenOption, out var pruefen) && !pruefen);

    public static string BuildUrl(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy)
    {
        var query = $"{quelle}?where={where}&select={select}" + (orderBy is null ? "" : $"&orderBy={orderBy}") + "&format=json";
        return SdataUrlBuilder.BuildResourceUrl(ziel.BaseUrl, ziel.Application, ServiceContract, ziel.Dataset, query);
    }

    public async Task<SdataErgebnis<T>> GetAsync<T>(SdataLeseZiel ziel, string quelle, string where, string select, string? orderBy, CancellationToken ct)
    {
        // Code-Sperre: ohne where liefert z. B. dtsTeileverwendungsnachweis 237.122 Zeilen (count wird ignoriert).
        if (string.IsNullOrWhiteSpace(where))
            throw new ArgumentException("SData-Lesezugriff ohne where ist gesperrt.", nameof(where));

        var url = BuildUrl(ziel, quelle, where, select, orderBy);
        var uhr = Stopwatch.StartNew();
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Options.Set(ZertifikatPruefenOption, ziel.ZertifikatPruefen);
            // format=json allein reicht nicht: ohne Accept-Header liefert Sage Atom-XML (UAT 2026-10-30).
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ziel.Username}:{ziel.Password}")));

            using var response = await _http.SendAsync(message, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sage-SData antwortete {Status} auf {Url}.", status, url);
                return Leer<T>(new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, 0,
                    $"HTTP {status} {response.ReasonPhrase}", Cap(body)));
            }

            try
            {
                var zeilen = JsonSerializer.Deserialize<SdataFeed<T>>(body, JsonOptions)?.Resources ?? new List<T>();
                return new SdataErgebnis<T>(zeilen, new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, zeilen.Count, null, Cap(body)));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Sage-SData-Antwort von {Url} nicht lesbar.", url);
                return Leer<T>(new SdataCall(quelle, url, status, uhr.ElapsedMilliseconds, 0, $"Antwort nicht lesbar: {ex.Message}", Cap(body)));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Status 0 = Verbindungsfehler (nicht erreichbar, Timeout, TLS). Meldungen nennen Host/Grund, nie Zugangsdaten
            // (die stehen nur im Authorization-Header).
            _logger.LogWarning(ex, "Sage-SData nicht erreichbar ({Url}).", url);
            var text = ex is TaskCanceledException
                ? $"Zeitüberschreitung nach {_http.Timeout.TotalSeconds:0} s"
                : ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
            return Leer<T>(new SdataCall(quelle, url, 0, uhr.ElapsedMilliseconds, 0, text, null));
        }
    }

    private static SdataErgebnis<T> Leer<T>(SdataCall call) => new(Array.Empty<T>(), call);

    private static string? Cap(string? value) =>
        value is null ? null : (value.Length <= MaxRohdaten ? value : value[..MaxRohdaten]);
}
