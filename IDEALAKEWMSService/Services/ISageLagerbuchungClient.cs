namespace IDEALAKEWMSService.Services;

/// <summary>
/// Laufzeit-Endpunkt (DB-first SData-Segmente + appsettings-Credentials). Die Ziel-URL wird daraus
/// zusammengesetzt: <c>{BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/$service/LagerbuchungService</c>
/// (z. B. <c>https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServices/ake_TEST2026;1/$service/LagerbuchungService</c>).
/// </summary>
public sealed record SageBookingEndpoint(
    string BaseUrl,
    string Application,
    string ServiceContract,
    string Dataset,
    string Username,
    string Password);

/// <summary>Ergebnis eines Sende-Versuchs — Erfolg plus rohe Antwort, oder Fehlertext.</summary>
public sealed record SageBookingSendResult(bool Success, string? ResponseRaw, string? Error);

/// <summary>
/// Sendet eine einzelne Lagerbuchung an die Sage-SData-API
/// (<c>POST {BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/$service/LagerbuchungService</c>,
/// Basic-Auth; URL via <c>SageLagerbuchungClient.BuildServiceUrl</c>). Manual-UAT — der HTTP-/SData-Pfad
/// ist nicht InMemory-testbar.
/// </summary>
public interface ISageLagerbuchungClient
{
    Task<SageBookingSendResult> SendAsync(SageLagerbuchungRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default);
}
