namespace IDEALAKEWMSService.Services;

public interface IFaZusatzinfoSyncService
{
    /// <summary>
    /// <paramref name="autoDoneMaxPerRun"/>: Sicherheits-Cap fuer das Auto-Erledigt
    /// (Spec §10.2) — mehr Kandidaten je Lauf → kein Erledigt-Write + Warn + Fehlermail.
    /// <paramref name="autoErledigtEnabled"/> (Teil 7 AK 11, Stage 2): Auto-Erledigt-Schalter.
    /// Zusaetzlich greift die datengetriebene Sperre (Stage 1): bei WA-Mehrfachtreffer (hierarchisch:
    /// HauptFA + Sub-FAs teilen die OrderNumber) wird Fold 2 uebersprungen, unabhaengig vom Schalter.
    /// Werte kommen als Parameter vom SyncWorker (DB-Read via ServiceSettings), damit der Service
    /// ohne DB-Settings unit-testbar bleibt.
    /// </summary>
    Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, bool autoErledigtEnabled = true, CancellationToken ct = default);
}
