namespace IDEALAKEWMSService.Services;

public interface IFaZusatzinfoSyncService
{
    /// <summary>
    /// <paramref name="autoDoneMaxPerRun"/>: Sicherheits-Cap fuer das Auto-Erledigt
    /// (Spec §10.2) — mehr Kandidaten je Lauf → kein Erledigt-Write + Warn + Fehlermail.
    /// Wert kommt als Parameter vom SyncWorker (DB-Read via ServiceSettings),
    /// damit der Service ohne DB-Settings unit-testbar bleibt.
    /// </summary>
    Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default);
}
