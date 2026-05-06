namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able strings spiegeln Sage-Realitaet wider.</summary>
public record SageLagerplatzDto(string? Lagerkennung, string? Kurzbezeichnung, string? Platzbezeichnung);

public interface ISageLagerplatzReader
{
    Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default);
}
