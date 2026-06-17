namespace IDEALAKEWMSService.Services;

/// <summary>
/// Automatische FA-zu-Arbeitsgang-Erkennung aus dem BOM-Cache (v1.22.0).
/// Eigener idempotenter Sync-Schritt NACH dem BomCache-Sync.
/// </summary>
public interface IFaWorkStepDetectionService
{
    /// <summary>
    /// Matcht aktive <c>WorkSteps</c> (Suchbegriffe) gegen die Bezeichnungen der
    /// gecachten BOM-Items und legt fuer offene FAs fehlende <c>FaWorkStep</c>-Zeilen
    /// an (Source=Sync). Nur-hinzufuegen-Semantik: vorhandene Zeilen — auch manuell
    /// entfernte (IsRemoved) — werden nie veraendert.
    /// </summary>
    Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default);
}
