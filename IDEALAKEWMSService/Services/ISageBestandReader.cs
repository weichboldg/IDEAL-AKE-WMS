namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able weil LEFT JOIN + Aggregation ggf. NULL liefern kann.</summary>
public record SageBestandDto(string? Artikelnummer, string? Lagerplatz, decimal? Bestand);

public interface ISageBestandReader
{
    Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default);
}
