namespace IDEALAKEWMSService.Services;

/// <summary>
/// Arbeitsgang-Abgleich (v1.61.0, Spec 2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view): legt fehlende
/// Arbeitsgang-Kuerzel aus der Sage-View (<c>Sync:OperationConfigSyncViewName</c>, Spalten Arbeitsgangnummer, Bezeichnung, AG)
/// in <c>OperationConfigs</c> an (Quelle Sage, Offset 0) und kennzeichnet Eintraege der Quelle Sage/Manuell, die nicht in der View
/// stehen (<c>SageMissingSince</c>). Nie ueberschreiben, nie loeschen; einzige Aenderung an Bestandszeilen ausser der Kennzeichnung:
/// Quelle Manual -> Sage. Standortneutral, Schalter <c>Sync:OperationConfigSyncEnabled</c>.
/// </summary>
public interface IOperationConfigSyncService
{
    Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
