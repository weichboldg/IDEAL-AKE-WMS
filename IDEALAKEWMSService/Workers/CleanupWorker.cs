using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Workers;

/// <summary>
/// Erweiterbarer Cleanup-Abschnitt. Laeuft taeglich (24h-Takt); jeder Cleaner ist
/// per ServiceSetting konfiguriert, DryRun-bewusst und resilient (ein Fehler killt
/// den Loop nicht). Weitere Cleaner: eigenen gegateten Block + Service + Setting ergaenzen.
/// </summary>
public class CleanupWorker : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    private readonly ILogger<CleanupWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanupWorker(ILogger<CleanupWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupWorker gestartet. Version {Version} ({Date}).",
            IDEALAKEWMSService.AppVersion.Version, IDEALAKEWMSService.AppVersion.Date);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dryRun = await ServiceSettings.GetBoolSafeAsync(_configuration, "WorkerSettings:SyncDryRun", false, stoppingToken);
                await RunActivityLogCleanupAsync(dryRun, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im CleanupWorker.");
                await NotifyErrorAsync("CleanupWorker (unerwartet)", ex, stoppingToken);
            }

            _logger.LogDebug("CleanupWorker: Naechster Durchlauf in {Hours}h.", RunInterval.TotalHours);
            await Task.Delay(RunInterval, stoppingToken);
        }

        _logger.LogInformation("CleanupWorker gestoppt.");
    }

    private async Task RunActivityLogCleanupAsync(bool dryRun, CancellationToken ct)
    {
        var retentionDays = await ServiceSettings.GetIntSafeAsync(
            _configuration, "Cleanup:AktivitaetsprotokollAufbewahrungTage", 180, ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IActivityLogCleanupService>();
            var result = await svc.RunAsync(retentionDays, dryRun, ct);
            if (result.Skipped)
                _logger.LogDebug("Aktivitaetsprotokoll-Bereinigung uebersprungen: {Reason}", result.SkipReason);
            else
                _logger.LogInformation("Aktivitaetsprotokoll-Bereinigung fertig: {Deleted} geloescht (DryRun={DryRun}).",
                    result.Deleted, dryRun);
        }
        // Cancellation (Service-Shutdown) ist kein Job-Fehler -> nicht als Fehler loggen/melden
        // (analog SyncWorker.RunResilientAsync). Die OCE propagiert dann sauber aus dem Worker.
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Aktivitaetsprotokoll-Bereinigung ist fehlgeschlagen.");
            await NotifyErrorAsync("Aktivitaetsprotokoll-Bereinigung", ex, ct);
        }
    }

    private async Task NotifyErrorAsync(string stepName, Exception ex, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ISyncErrorNotifier>();
            await notifier.NotifyAsync(stepName, ex, ct);
        }
        catch (Exception notifyEx)
        {
            _logger.LogError(notifyEx, "Fehlermail-Benachrichtigung fuer {Step} fehlgeschlagen.", stepName);
        }
    }
}
