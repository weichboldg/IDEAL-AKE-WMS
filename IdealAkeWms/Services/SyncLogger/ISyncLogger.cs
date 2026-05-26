namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Einheitlicher Einstiegspunkt fuer das Sync-Audit-Logging.
/// Jeder Sync-Run startet mit <see cref="BeginRunAsync"/> und endet mit
/// <see cref="ISyncRun.FinishSuccessAsync"/> bzw. <see cref="ISyncRun.FinishFailedAsync"/>.
/// </summary>
public interface ISyncLogger
{
    /// <summary>
    /// Startet einen neuen Run, schreibt einen Start-Eintrag (Info) und liefert das Run-Handle zurueck.
    /// </summary>
    /// <param name="serviceName">Konstante aus <see cref="SyncLogServices"/>.</param>
    Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default);
}
