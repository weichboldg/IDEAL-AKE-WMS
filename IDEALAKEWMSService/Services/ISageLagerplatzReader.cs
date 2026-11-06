namespace IDEALAKEWMSService.Services;

/// <summary>DTO from SAGE — null-able Werte spiegeln Sage-Realitaet wider.
/// <paramref name="PlatzId"/> = KHKLagerplaetze.PlatzID (numerische Sage-Lagerplatz-Id,
/// von der SData-Lagerbuchung als Herkunft-/ZielLagerplatzId erwartet).
/// Ab <paramref name="Lagerort"/> (Spec 2026-11-06-sage-sdata-stammdaten-sync, E1/F1): Felder, die nur der SData-Reader liefert.
/// <c>null</c> = "Quelle liefert das Feld nicht" — der Abgleich schreibt es dann nicht (der View-Reader laesst sie alle null).</summary>
public record SageLagerplatzDto(string? Lagerkennung, string? Kurzbezeichnung, string? Platzbezeichnung, int? PlatzId = null,
    SageLagerortDto? Lagerort = null, bool? IstGesperrt = null, bool? IstGesperrtInventur = null,
    decimal? Breite = null, decimal? Hoehe = null, decimal? Laenge = null, decimal? Volumen = null, decimal? Tragkraft = null);

/// <summary>Sage-Lagerort (<c>LagerorteInfo</c>) eines Platzes; nur aktive Lagerorte kommen in die Menge.</summary>
public record SageLagerortDto(string Lagerkennung, string? Bezeichnung, string? Lagertechnik, bool IstDispoLager);

public interface ISageLagerplatzReader
{
    Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default);
}
