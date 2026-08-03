using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class SageLagerbuchungClient : ISageLagerbuchungClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SageLagerbuchungClient> _logger;

    /// <summary>Feste SData-Wurzel im Pfad (nach dem Host, vor der Application).</summary>
    public const string SdataRoot = "sdata";

    /// <summary>Feste Ziel-Resource. Beginnt mit '$' — MUSS literal bleiben (kein %24).</summary>
    public const string ServiceResource = "$service/LagerbuchungService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public SageLagerbuchungClient(HttpClient http, ILogger<SageLagerbuchungClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Baut die absolute SData-Ziel-URL aus den Endpunkt-Segmenten. REIN + testbar.
    /// <para>
    /// <b>Kodierung (wichtig):</b> KEIN <see cref="Uri.EscapeDataString"/> auf Dataset oder Resource.
    /// Das Semikolon im Dataset (z. B. <c>ake_TEST2026;1</c>) und das <c>$</c> der Resource sind laut
    /// RFC 3986 sub-delims und in Pfadsegmenten gueltig — sie muessen <b>literal</b> bleiben
    /// (<c>;</c> nicht <c>%3B</c>, <c>$</c> nicht <c>%24</c>). Der SData-Feed liefert die href-Werte
    /// ebenfalls unkodiert; das ist die Referenz. Es wird nur bewusst per String-Interpolation
    /// zusammengesetzt (die .NET-<see cref="Uri"/>-Pipeline laesst diese sub-delims im Pfad unangetastet).
    /// </para>
    /// </summary>
    public static string BuildServiceUrl(SageBookingEndpoint endpoint)
    {
        var baseUrl = endpoint.BaseUrl.TrimEnd('/');
        // Toleranz: manche Admins tragen die BaseUrl inkl. der SData-Wurzel "/sdata" ein
        // (z. B. https://host:5493/sdata). Dann NICHT verdoppeln — sonst entstuende ".../sdata/sdata/...".
        if (baseUrl.EndsWith("/" + SdataRoot, StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^(SdataRoot.Length + 1)].TrimEnd('/');
        var app = endpoint.Application.Trim('/');
        var contract = endpoint.ServiceContract.Trim('/');
        var dataset = endpoint.Dataset.Trim('/');
        return $"{baseUrl}/{SdataRoot}/{app}/{contract}/{dataset}/{ServiceResource}";
    }

    public async Task<SageBookingSendResult> SendAsync(
        SageLagerbuchungRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl) || string.IsNullOrWhiteSpace(endpoint.Application)
            || string.IsNullOrWhiteSpace(endpoint.ServiceContract) || string.IsNullOrWhiteSpace(endpoint.Dataset))
            return new SageBookingSendResult(false, null,
                "SData-Konfiguration unvollstaendig (SData:BaseUrl / SData:Application / SData:ServiceContract / SData:Dataset).");

        var url = BuildServiceUrl(endpoint);

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{endpoint.Username}:{endpoint.Password}"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var response = await _http.SendAsync(message, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return new SageBookingSendResult(true, Cap(body), null);

            // Fehlerantwort samt Body ins Log (der eigentliche Sage-Fehler steht im Body, nicht im Status).
            _logger.LogWarning("Sage-LagerbuchungService antwortete {Status} {Reason} auf {Url}. Antwort: {Body}",
                (int)response.StatusCode, response.ReasonPhrase, url, Cap(body));

            return new SageBookingSendResult(false, Cap(body),
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sende-Fehler an Sage-LagerbuchungService ({Url}).", url);
            return new SageBookingSendResult(false, null, ex.Message);
        }
    }

    private static string? Cap(string? value)
        => value is null ? null : (value.Length <= 4000 ? value : value.Substring(0, 4000));
}
