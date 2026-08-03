namespace IDEALAKEWMSService.Services;

/// <summary>Laufzeit-Endpunkt (DB-first BaseUrl/Dataset + appsettings-Credentials).</summary>
public sealed record SageBookingEndpoint(string BaseUrl, string Dataset, string Username, string Password);

/// <summary>Ergebnis eines Sende-Versuchs — Erfolg plus rohe Antwort, oder Fehlertext.</summary>
public sealed record SageBookingSendResult(bool Success, string? ResponseRaw, string? Error);

/// <summary>
/// Sendet eine einzelne Lagerbuchung an die Sage-SData-API
/// (<c>POST {BaseUrl}/{Dataset}/$service/LagerbuchungService</c>, Basic-Auth). Manual-UAT — der
/// HTTP-/SData-Pfad ist nicht InMemory-testbar.
/// </summary>
public interface ISageLagerbuchungClient
{
    Task<SageBookingSendResult> SendAsync(SageLagerbuchungRequest request, SageBookingEndpoint endpoint, CancellationToken ct = default);
}
