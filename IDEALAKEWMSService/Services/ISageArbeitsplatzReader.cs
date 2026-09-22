namespace IDEALAKEWMSService.Services;

/// <summary>DTO aus Sage `KHKPpsArbeitsplaetze` (Baustein a). Werte null-bar (Sage-Realitaet).
/// <paramref name="Arbeitsplatznummer"/> ist der Verknuepfungsschluessel — bewusst als **Zeichenkette**
/// gelesen (nie int.Parse, `0000` ≠ `0`); der Reader liest den Rohwert typunabhaengig als String.</summary>
public record SageArbeitsplatzDto(string? Arbeitsplatznummer, string? Bezeichnung1, string? Arbeitsschritt);

public interface ISageArbeitsplatzReader
{
    /// <summary>Aktive Arbeitsplaetze (`Aktiv = -1`) des angegebenen Mandanten. Filterung auf gefuelltes
    /// Kuerzel und Ausschlussliste macht der <c>ProductionWorkplaceSyncService</c>, nicht der Reader.</summary>
    Task<List<SageArbeitsplatzDto>> GetActiveAsync(int mandant, CancellationToken ct = default);
}
