namespace IDEALAKEWMSService.Services;

/// <summary>DTO aus Sage `KHKPpsArbeitsplaetze` (Baustein a). Werte null-bar (Sage-Realitaet).
/// <paramref name="Arbeitsplatznummer"/> ist der Verknuepfungsschluessel — bewusst als **Zeichenkette**
/// gelesen (nie int.Parse, `0000` ≠ `0`); der Reader liest den Rohwert typunabhaengig als String.
/// <paramref name="BdeTerminalId"/>, <paramref name="Kostenstelle"/>, <paramref name="Schichtplannummer"/> liefert nur der SData-Weg
/// (v1.73.0, E6a); null = Quelle liefert das Feld nicht → der Abgleich schreibt es nicht (F1).</summary>
public record SageArbeitsplatzDto(string? Arbeitsplatznummer, string? Bezeichnung1, string? Arbeitsschritt,
    string? BdeTerminalId = null, string? Kostenstelle = null, string? Schichtplannummer = null);

/// <summary>Ergebnis des Arbeitsplatz-Reads. <paramref name="SourceExists"/> = false: Quelle fehlt oder ist fuer
/// den Login unsichtbar — der Sync ueberspringt dann mit Warnung (Muster <c>SageZusatzinfoReadResult</c>).
/// <paramref name="ArbeitsschrittGeliefert"/> = false: die Quelle kennt das Kuerzel gar nicht (SData ohne USER_ArbeitsSchritt, P-A1) —
/// leere Kuerzel heissen dann „nicht gelesen“, nicht „in Sage leer“; der Arbeitsgang-Abgleich laeuft nicht (sonst waere jedes
/// Kuerzel „nicht in Sage“).</summary>
public record SageArbeitsplatzReadResult(bool SourceExists, List<SageArbeitsplatzDto> Rows, bool ArbeitsschrittGeliefert = true);

public interface ISageArbeitsplatzReader
{
    /// <summary>Aktive Arbeitsplaetze (`Aktiv = -1`) des angegebenen Mandanten aus der Quelle
    /// <paramref name="quotedSource"/> (bereits per <c>FaHierarchySql.ValidateAndQuote</c> geprueft; Tabelle
    /// KHKPpsArbeitsplaetze oder View vw_IDEAL_AKE_WMS_Arbeitsgaenge). Filterung auf gefuelltes Kuerzel und
    /// Ausschlussliste macht der <c>ProductionWorkplaceSyncService</c>, nicht der Reader.</summary>
    Task<SageArbeitsplatzReadResult> GetActiveAsync(string quotedSource, int mandant, CancellationToken ct = default);
}
