namespace IdealAkeWms.Services.SyncLogger;

/// <summary>
/// Handle fuer einen laufenden Sync-Run. Pflicht: genau einer der beiden Finish-Aufrufe
/// pro Run. Ein zweiter Finish-Aufruf wird idempotent ignoriert (kein Throw, kein
/// zusaetzlicher Eintrag). Wenn weder Finish noch DisposeAsync den Run sauber schliessen,
/// schreibt DisposeAsync einen Warning-Eintrag "Run wurde unerwartet beendet".
/// </summary>
public interface ISyncRun : IAsyncDisposable
{
    Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default);

    /// <summary>
    /// Sauberer Abschluss. <paramref name="counts"/>-Eintraege werden als
    /// <c>"key=value, key=value"</c> in die End-Message gerendert (siehe Spec §5.3).
    /// </summary>
    Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                            string? messageSuffix = null,
                            CancellationToken ct = default);

    /// <summary>
    /// Fehler-Abschluss. <paramref name="errorMessage"/> wird in die End-Message geschrieben,
    /// <paramref name="counts"/> mit demselben Formatter wie bei Success angefuegt.
    /// </summary>
    Task FinishFailedAsync(string errorMessage,
                           IReadOnlyDictionary<string, int>? counts = null,
                           CancellationToken ct = default);
}
