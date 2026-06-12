using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Workers;

public class SyncWorker : BackgroundService
{
    private readonly ILogger<SyncWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    // Independent run-cadence tracking — BdeAutoPause hat eigenes Intervall, HolidaySync läuft täglich.
    private DateTime? _lastAutoPauseRun;
    private DateTime? _lastHolidaySyncRun;
    private DateTime? _lastLagerbestandRun;

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

                // --- Lagerbestellungen E-Mail-Versand ---
                if (_configuration.GetValue<bool>("Sync:WarehouseRequisitionEmailEnabled", false))
                {
                    try
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IWarehouseRequisitionEmailService>();
                        var result = await emailService.SendPendingEmailsAsync(dryRun, stoppingToken);
                        if (result.SubmitsSent > 0 || result.CancellationsSent > 0)
                            _logger.LogInformation("Lagerbestellungen: {Submits} Submit + {Cancels} Storno-Mails versendet.", result.SubmitsSent, result.CancellationsSent);
                        foreach (var e in result.Errors) _logger.LogWarning("Lagerbestellung-Mail-Fehler: {Err}", e);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Versand der Lagerbestellungs-E-Mails.");
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
                // FA-Arbeitsgang-Erkennung (aus BOM-Cache) — laeuft direkt NACH BomCache
                // ---------------------------------------------------------------
                var faDetectionEnabled = await ServiceSettings.GetBoolAsync(_configuration, "Sync:FaWorkStepDetectionEnabled", false, stoppingToken);
                if (faDetectionEnabled)
                {
                    try
                    {
                        _logger.LogInformation("Starte FA-Arbeitsgang-Erkennung");
                        using var faScope = _scopeFactory.CreateScope();
                        var faSvc = faScope.ServiceProvider.GetRequiredService<IFaWorkStepDetectionService>();
                        var faResult = await faSvc.DetectAsync(dryRun: dryRun, ct: stoppingToken);
                        _logger.LogInformation("FA-Arbeitsgang-Erkennung fertig: Neu={Ins}, Err={Err}",
                            faResult.Inserted, faResult.Errors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "FA-Arbeitsgang-Erkennung ist fehlgeschlagen");
                    }
                }
                else
                {
                    _logger.LogDebug("FA-Arbeitsgang-Erkennung deaktiviert (Sync:FaWorkStepDetectionEnabled=false in ServiceSettings)");
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

                // ---------------------------------------------------------------
                // BDE Auto-Pause — eigenes Intervall via Sync:BdeAutoPauseIntervalMinutes (default 60)
                // ---------------------------------------------------------------
                if (await ShouldRunAutoPauseAsync(stoppingToken))
                {
                    try
                    {
                        _logger.LogInformation("Starte BDE-AutoPause");
                        using var autoPauseScope = _scopeFactory.CreateScope();
                        var autoPause = autoPauseScope.ServiceProvider.GetRequiredService<IBdeAutoPauseService>();
                        var apResult = await autoPause.RunAsync(stoppingToken);
                        _logger.LogInformation("BDE-AutoPause fertig: Checked={Checked}, Paused={Paused}, Errors={Err}",
                            apResult.CheckedCount, apResult.PausedCount, apResult.Errors.Count);
                        _lastAutoPauseRun = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "BDE-AutoPause ist fehlgeschlagen");
                    }
                }

                // ---------------------------------------------------------------
                // Holiday-Sync — täglich (24h), skip wenn Sync:FeiertagSyncEnabled=false
                // ---------------------------------------------------------------
                if (await ShouldRunHolidaySyncAsync(stoppingToken))
                {
                    try
                    {
                        _logger.LogInformation("Starte Holiday-Sync");
                        using var holidayScope = _scopeFactory.CreateScope();
                        var holidaySync = holidayScope.ServiceProvider.GetRequiredService<IHolidaySyncService>();
                        var hResult = await holidaySync.RunAsync(stoppingToken);
                        _logger.LogInformation("Holiday-Sync fertig: Fetched={F}, Inserted={I}, Errors={Err}",
                            hResult.FetchedCount, hResult.InsertedCount, hResult.Errors.Count);
                        _lastHolidaySyncRun = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Holiday-Sync ist fehlgeschlagen");
                    }
                }

                // ---------------------------------------------------------------
                // Lagerplatz-Sync (Sage Stammdaten)
                // ---------------------------------------------------------------
                if (_configuration.GetValue<bool>("Sync:LagerplaetzeEnabled", false))
                {
                    try
                    {
                        _logger.LogInformation("Lagerplatz-Sync startet...");
                        using var lpScope = _scopeFactory.CreateScope();
                        var lpSync = lpScope.ServiceProvider.GetRequiredService<ILagerplatzSyncService>();
                        var lpResult = await lpSync.RunAsync(stoppingToken);
                        _logger.LogInformation(
                            "Lagerplatz-Sync: {Inserted} neu, {Updated} aktualisiert, {Conflicts} Konflikte, {Deactivated} deaktiviert, {Skipped} uebersprungen, {Errors} Fehler.",
                            lpResult.Inserted, lpResult.Updated, lpResult.Conflicts, lpResult.Deactivated, lpResult.Skipped, lpResult.Errors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lagerplatz-Sync ist fehlgeschlagen.");
                    }
                }

                // ---------------------------------------------------------------
                // Lagerbestand-Sync (Sage Bestand-Korrektur)
                // ---------------------------------------------------------------
                if (ShouldRunLagerbestand())
                {
                    try
                    {
                        _logger.LogInformation("Lagerbestand-Sync startet (DryRun={DryRun})...", dryRun);
                        using var lbScope = _scopeFactory.CreateScope();
                        var lbSync = lbScope.ServiceProvider.GetRequiredService<ILagerbestandSyncService>();
                        var lbResult = await lbSync.RunAsync(dryRun, stoppingToken);
                        _logger.LogInformation(
                            "Lagerbestand-Sync: {Tuples} Tupel, {Plus} Plus, {Minus} Minus, {NoChange} ohne Aenderung, {Skipped} uebersprungen, {Errors} Fehler.",
                            lbResult.Tuples, lbResult.CorrectionsPlus, lbResult.CorrectionsMinus,
                            lbResult.NoChange, lbResult.Skipped, lbResult.Errors);
                        _lastLagerbestandRun = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lagerbestand-Sync ist fehlgeschlagen.");
                    }
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

    private async Task<bool> ShouldRunAutoPauseAsync(CancellationToken ct)
    {
        var intervalMinutes = await ServiceSettings.GetIntAsync(_configuration, "Sync:BdeAutoPauseIntervalMinutes", 60, ct);
        if (intervalMinutes <= 0) return false;
        if (_lastAutoPauseRun == null) return true;
        return DateTime.Now - _lastAutoPauseRun.Value >= TimeSpan.FromMinutes(intervalMinutes);
    }

    private Task<bool> ShouldRunHolidaySyncAsync(CancellationToken ct)
    {
        // Read via IConfiguration (same source HolidaySyncOptions binds to in
        // Program.cs) so the worker gate and the service-internal gate cannot
        // disagree across the two configuration sources.
        var enabled = _configuration.GetValue<bool>("Sync:FeiertagSyncEnabled", false);
        if (!enabled) return Task.FromResult(false);
        if (_lastHolidaySyncRun == null) return Task.FromResult(true);
        return Task.FromResult(DateTime.Now - _lastHolidaySyncRun.Value >= TimeSpan.FromHours(24));
    }

    private bool ShouldRunLagerbestand()
    {
        if (!_configuration.GetValue<bool>("Sync:LagerbestandEnabled", false))
            return false;

        var overrideMinutes = _configuration.GetValue<int>("Sync:LagerbestandIntervalMinutes", 0);
        if (overrideMinutes <= 0)
            return true;   // nutzt Worker-Standard-Intervall (15 Min)

        if (_lastLagerbestandRun == null) return true;
        return DateTime.Now - _lastLagerbestandRun.Value >= TimeSpan.FromMinutes(overrideMinutes);
    }
}
