using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Workers;

public class SyncWorker : BackgroundService
{
    private readonly ILogger<SyncWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public SyncWorker(ILogger<SyncWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncWorker gestartet.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = _configuration.GetValue<int>("WorkerSettings:SyncIntervalMinutes", 15);
            var dryRun = _configuration.GetValue<bool>("WorkerSettings:SyncDryRun", false);

            if (dryRun)
                _logger.LogInformation("SyncWorker läuft im DryRun-Modus — keine Änderungen werden geschrieben.");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sageImport = scope.ServiceProvider.GetRequiredService<ISageImportService>();

                // Produktionsaufträge sync
                if (_configuration.GetValue<bool>("Sync:ProductionOrdersEnabled", true))
                {
                    _logger.LogInformation("Produktionsaufträge-Sync startet...");
                    var waResult = await sageImport.SyncProductionOrdersAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "Produktionsaufträge-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        waResult.Inserted, waResult.Updated, waResult.Errors,
                        waResult.ErrorDetails != null ? $" Details: {waResult.ErrorDetails}" : "");
                }

                // Artikel sync
                if (_configuration.GetValue<bool>("Sync:ArticlesEnabled", true))
                {
                    _logger.LogInformation("Artikel-Sync startet...");
                    var articleResult = await sageImport.SyncArticlesAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "Artikel-Sync: {Inserted} neu, {Errors} Fehler.{Details}",
                        articleResult.Inserted, articleResult.Errors,
                        articleResult.ErrorDetails != null ? $" Details: {articleResult.ErrorDetails}" : "");
                }

                // OSEON Tracking sync + Werkbank-Sync
                if (_configuration.GetValue<bool>("Sync:OseonTrackingEnabled", false))
                {
                    var oseonSync = scope.ServiceProvider.GetRequiredService<IOseonSyncService>();

                    _logger.LogInformation("OSEON-Tracking-Sync startet...");
                    var oseonResult = await oseonSync.SyncOseonProductionOrdersAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "OSEON-Tracking-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        oseonResult.Inserted, oseonResult.Updated, oseonResult.Errors,
                        oseonResult.ErrorDetails != null ? $" Details: {oseonResult.ErrorDetails}" : "");

                    // Werkbank von OSEON-Aufträgen auf Sage-Aufträge übertragen
                    _logger.LogInformation("Werkbank-Sync (OSEON → Produktionsaufträge) startet...");
                    var wpResult = await oseonSync.SyncWorkplacesToProductionOrdersAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "Werkbank-Sync: {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        wpResult.Updated, wpResult.Errors,
                        wpResult.ErrorDetails != null ? $" Details: {wpResult.ErrorDetails}" : "");
                }
                // enaio DMS-Sync
                if (_configuration.GetValue<bool>("Sync:EnaioDmsEnabled", false))
                {
                    var enaioDmsSync = scope.ServiceProvider.GetRequiredService<IEnaioDmsSyncService>();

                    _logger.LogInformation("enaio DMS-Sync startet...");
                    var enaioDmsResult = await enaioDmsSync.SyncDocumentsAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "enaio DMS-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        enaioDmsResult.Inserted, enaioDmsResult.Updated, enaioDmsResult.Errors,
                        enaioDmsResult.ErrorDetails != null ? $" Details: {enaioDmsResult.ErrorDetails}" : "");
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im SyncWorker.");
            }

            _logger.LogDebug("SyncWorker: Nächster Durchlauf in {IntervalMinutes} Minuten.", intervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("SyncWorker gestoppt.");
    }
}
