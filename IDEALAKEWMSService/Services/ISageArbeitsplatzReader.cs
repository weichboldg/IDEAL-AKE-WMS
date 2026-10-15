namespace IDEALAKEWMSService.Services;

/// <summary>DTO aus Sage `KHKPpsArbeitsplaetze` (Baustein a). Werte null-bar (Sage-Realitaet).
/// <paramref name="Arbeitsplatznummer"/> ist der Verknuepfungsschluessel — bewusst als **Zeichenkette**
/// gelesen (nie int.Parse, `0000` ≠ `0`); der Reader liest den Rohwert typunabhaengig als String.</summary>
public record SageArbeitsplatzDto(string? Arbeitsplatznummer, string? Bezeichnung1, string? Arbeitsschritt);

/// <summary>Ergebnis des Arbeitsplatz-Reads. <paramref name="SourceExists"/> = false: Quelle fehlt oder ist fuer
/// den Login unsichtbar — der Sync ueberspringt dann mit Warnung (Muster <c>SageZusatzinfoReadResult</c>).</summary>
public record SageArbeitsplatzReadResult(bool SourceExists, List<SageArbeitsplatzDto> Rows);

public interface ISageArbeitsplatzReader
{
    /// <summary>Aktive Arbeitsplaetze (`Aktiv = -1`) des angegebenen Mandanten aus der Quelle
    /// <paramref name="quotedSource"/> (bereits per <c>FaHierarchySql.ValidateAndQuote</c> geprueft; Tabelle
    /// KHKPpsArbeitsplaetze oder View vw_IDEAL_AKE_WMS_Arbeitsgaenge). Filterung auf gefuelltes Kuerzel und
    /// Ausschlussliste macht der <c>ProductionWorkplaceSyncService</c>, nicht der Reader.</summary>
    Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default);
}
