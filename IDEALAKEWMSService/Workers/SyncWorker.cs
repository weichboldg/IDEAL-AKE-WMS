using IDEALAKEWMSService.Common;
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
        _logger.LogInformation("SyncWorker gestartet. Version {Version} ({Date}).",
            IDEALAKEWMSService.AppVersion.Version, IDEALAKEWMSService.AppVersion.Date);

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

                // OSEON Artikelkategorie-Sync (muss nach Artikel-Import laufen)
                if (_configuration.GetValue<bool>("Sync:OseonArticleCategoryEnabled", false))
                {
                    var oseonSync = scope.ServiceProvider.GetRequiredService<IOseonSyncService>();

                    _logger.LogInformation("OSEON-Artikelkategorie-Sync startet...");
                    var catResult = await oseonSync.SyncArticleCategoriesToWmsAsync(dryRun, stoppingToken);
                    _logger.LogInformation(
                        "OSEON-Artikelkategorie-Sync: {Inserted} neu, {Updated} aktualisiert, {Errors} Fehler.{Details}",
                        catResult.Inserted, catResult.Updated, catResult.Errors,
                        catResult.ErrorDetails != null ? $" Details: {catResult.ErrorDetails}" : "");
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

                // --- Bedarfsmeldungen E-Mail-Versand ---
                if (_configuration.GetValue<bool>("Sync:PartRequisitionEmailEnabled", false))
                {
                    try
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IPartRequisitionEmailService>();
                        var sentCount = await emailService.SendPendingEmailsAsync(dryRun, stoppingToken);
                        if (sentCount > 0)
                            _logger.LogInformation("Bedarfsmeldungen: {Count} E-Mails versendet.", sentCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Versand der Bedarfsmeldungs-E-Mails.");
                    }
                }
                // ---------------------------------------------------------------
                // BOM-Cache-Sync
                // ---------------------------------------------------------------
                var bomCacheEnabled = await ServiceSettings.GetBoolAsync(_configuration, "Sync:BomCacheEnabled", false, stoppingToken);
                if (bomCacheEnabled)
                {
                    try
                    {
                        _logger.LogInformation("Starte BOM-Cache-Sync");
                        using var bomScope = _scopeFactory.CreateScope();
                        var bomCacheSvc = bomScope.ServiceProvider.GetRequiredService<IBomCacheSyncService>();
                        var bomResult = await bomCacheSvc.SyncBomCacheAsync(dryRun: dryRun, ct: stoppingToken);
                        _logger.LogInformation("BOM-Cache-Sync fertig: Ins={Ins}, Upd={Upd}, Err={Err}",
                            bomResult.Inserted, bomResult.Updated, bomResult.Errors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "BOM-Cache-Sync ist fehlgeschlagen");
                    }
                }
                else
                {
                    _logger.LogDebug("BOM-Cache-Sync deaktiviert (Sync:BomCacheEnabled=false in ServiceSettings)");
                }

                // ---------------------------------------------------------------
                // Coating-Detection-Sync (Lackierteil-Erkennung)
                // ---------------------------------------------------------------
                var coatingEnabled = await ServiceSettings.GetBoolAsync(_configuration, "Sync:CoatingDetectionEnabled", false, stoppingToken);
                if (coatingEnabled)
                {
                    try
                    {
                        _logger.LogInformation("Starte Lackierteil-Erkennung");
                        using var coatScope = _scopeFactory.CreateScope();
                        var coatSvc = coatScope.ServiceProvider.GetRequiredService<ICoatingDetectionService>();
                        var coatResult = await coatSvc.DetectAndUpdateCoatingFlagsAsync(
                            dryRun: dryRun, specificOrderIds: null, ct: stoppingToken);
                        // Inserted=mit Lackierteilen, Updated=ohne Lackierteile
                        _logger.LogInformation("Lackierteil-Erkennung fertig: MitLack={WithCoat}, OhneLack={WithoutCoat}, Err={Err}",
                            coatResult.Inserted, coatResult.Updated, coatResult.Errors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lackierteil-Erkennung ist fehlgeschlagen");
                    }
                }
                else
                {
                    _logger.LogDebug("Lackierteil-Erkennung deaktiviert (Sync:CoatingDetectionEnabled=false in ServiceSettings)");
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
