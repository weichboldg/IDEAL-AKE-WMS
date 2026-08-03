using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class SageLagerbuchungClient : ISageLagerbuchungClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SageLagerbuchungClient> _logger;

    // Endpunkt-URL wird pro Request absolut gebaut (BaseUrl ist DB-first, nicht beim DI-Setup bekannt).
    private const string ServicePath = "$service/LagerbuchungService";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public SageLagerbuchungClient(HttpClient http, ILogger<SageLagerbuchungClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<SageBookingSendResult> SendAsync(
        SageLagerbuchungRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl) || string.IsNullOrWhiteSpace(endpoint.Dataset))
            return new SageBookingSendResult(false, null,
                "SData-Konfiguration unvollstaendig (SData:BaseUrl / SData:Dataset fehlt).");

        var url = $"{endpoint.BaseUrl.TrimEnd('/')}/{endpoint.Dataset.Trim('/')}/{ServicePath}";

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
