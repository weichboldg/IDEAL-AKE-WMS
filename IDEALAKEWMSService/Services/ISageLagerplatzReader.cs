namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able Werte spiegeln Sage-Realitaet wider.
/// <paramref name="PlatzId"/> = KHKLagerplaetze.PlatzID (numerische Sage-Lagerplatz-Id,
/// von der SData-Lagerbuchung als Herkunft-/ZielLagerplatzId erwartet).</summary>
public record SageLagerplatzDto(string? Lagerkennung, string? Kurzbezeichnung, string? Platzbezeichnung, int? PlatzId = null);

public interface ISageLagerplatzReader
{
    Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default);
}
