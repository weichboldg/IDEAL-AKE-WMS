using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services;

namespace IDEALAKEWMSService.Workers;

/// <summary>
/// Eigener, kurzgetakteter BackgroundService fuer die ausgehenden Sage-Lagerbuchungen
/// ("fast live"). Bewusst getrennt vom minutenskalierten <see cref="SyncWorker"/> — ein
/// Sage-Ausfall bei den Buchungen darf die anderen Sync-Bloecke nicht verlangsamen (und umgekehrt).
///
/// VORAUSSETZUNG: genau EINE Service-Instanz (der Idempotenz-Baustein "Status vor dem Call auf
/// Gesendet" schuetzt nur bei einer einzigen laufenden Queue-Verarbeitung).
///
/// Ablauf je Tick (nur wenn ServiceSetting <c>SageLagerbuchungAktiv</c> = true):
/// 0. Reconciliation-Sweep (B4): verpasste Ein-/Ausbuchungen nachtraeglich einreihen.
/// 1. Recovery haengender <c>Gesendet</c>-Eintraege (S3) ueber den Sage-Memo-Lookup (B2) — nie blind neu senden.
/// 2. Offene Eintraege senden: Status VOR dem HTTP-Call auf Gesendet (AK10), dann bestaetigen/fehlern.
/// 3. Bei zu vielen Fehlern je Lauf eine Fehlermail (Cap).
/// </summary>
public class SageBookingWorker : BackgroundService
{
    private readonly ILogger<SageBookingWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISyncLogger _syncLogger;

    // Kurzes Rueckblickfenster fuer den Reconciliation-Sweep: faengt Enqueue-Fehler (Crash zwischen
    // Buchung und Queue-Insert), holt aber NICHT Buchungen aus einer Toggle-Aus-Phase nach.
    private static readonly TimeSpan ReconcileLookback = TimeSpan.FromMinutes(15);

    public SageBookingWorker(
        ILogger<SageBookingWorker> logger,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ISyncLogger syncLogger)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _syncLogger = syncLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SageBookingWorker gestartet. Version {Version} ({Date}).",
            IDEALAKEWMSService.AppVersion.Version, IDEALAKEWMSService.AppVersion.Date);

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = await ServiceSettings.GetIntSafeAsync(
                _configuration, "Sync:SageLagerbuchungIntervalSeconds", 20, stoppingToken);
            if (intervalSeconds < 5) intervalSeconds = 5;   // Untergrenze gegen DB-Last

            try
            {
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "SageLagerbuchungAktiv", false, stoppingToken))
                    await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im SageBookingWorker.");
                await NotifyErrorAsync("SageBookingWorker (unerwartet)", ex, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("SageBookingWorker gestoppt.");
    }

    private async Task ProcessOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<ISageBookingQueueRepository>();
        var client = scope.ServiceProvider.GetRequiredService<ISageLagerbuchungClient>();
        var lookup = scope.ServiceProvider.GetRequiredService<ISageBuchungLookupReader>();

        var batchSize = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungBatchSize", 50, ct);
        var maxRetries = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungMaxRetries", 5, ct);
        var maxErrors = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungMaxErrorsPerRun", 50, ct);
        var stuckMinutes = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungStuckMinutes", 10, ct);

        var endpoint = new SageBookingEndpoint(
            BaseUrl: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:BaseUrl", ct) ?? string.Empty,
            Dataset: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:Dataset", ct) ?? string.Empty,
            Username: _configuration["SageLagerbuchung:Username"] ?? string.Empty,
            Password: _configuration["SageLagerbuchung:Password"] ?? string.Empty);

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageLagerbuchung, ct);
        int gesendet = 0, bestaetigt = 0, fehler = 0, nacherfasst = 0, uebersprungen = 0;

        try
        {
            // 0. Reconciliation-Sweep (B4)
            nacherfasst = await queue.EnqueueMissingAsync(DateTime.Now - ReconcileLookback, batchSize);
            if (nacherfasst > 0)
                await run.LogInfoAsync($"{nacherfasst} Buchung(en) ohne Queue-Eintrag nachtraeglich eingereiht.", ct: ct);

            // 1. Recovery haengender Gesendet-Eintraege (S3) via Sage-Lookup (B2)
            var stuck = await queue.GetStuckSentAsync(DateTime.Now.AddMinutes(-stuckMinutes), batchSize);
            foreach (var item in stuck)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    if (await lookup.ExistsAsync(item.StockMovementId, ct))
                    {
                        await queue.MarkConfirmedAsync(item.Id, "Recovery: Buchung in Sage bereits vorhanden (Memo-Lookup).");
                        bestaetigt++;
                    }
                    else
                    {
                        // Nicht in Sage -> darf erneut gesendet werden (zurueck auf Offen).
                        await queue.RequeueAsync(item.Id, SageBookingQueueRepository.ServiceActor);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Lookup unsicher -> Eintrag NICHT anfassen (nie blind neu senden).
                    uebersprungen++;
                    await run.LogWarningAsync(
                        $"Sage-Lookup fuer haengenden Eintrag #{item.Id} fehlgeschlagen: {ex.Message}", ct: ct);
                }
            }

            // 2. Offene Eintraege senden
            var open = await queue.GetOpenBatchAsync(batchSize);
            foreach (var item in open)
            {
                if (ct.IsCancellationRequested) break;

                if (item.AttemptCount >= maxRetries)
                {
                    await queue.MarkFailedAsync(item.Id,
                        $"Maximale Sende-Versuche ({maxRetries}) erreicht — manuelles Requeue noetig.", null);
                    fehler++;
                    continue;
                }

                SageLagerbuchungRequest payload;
                try
                {
                    payload = SageLagerbuchungPayloadBuilder.Build(item.StockMovement);
                }
                catch (SageBookingPayloadException pex)
                {
                    // AK9: ungueltige Buchung sauber als Fehler markieren statt zu senden/abzustuerzen.
                    await queue.MarkFailedAsync(item.Id, pex.Message, null);
                    fehler++;
                    continue;
                }

                // AK10: Status VOR dem HTTP-Call auf Gesendet (Timeout -> kein zweiter Auto-Versuch).
                await queue.MarkSentAsync(item.Id);
                gesendet++;

                var result = await client.SendAsync(payload, endpoint, ct);
                if (result.Success)
                {
                    await queue.MarkConfirmedAsync(item.Id, result.ResponseRaw);
                    bestaetigt++;
                }
                else
                {
                    await queue.MarkFailedAsync(item.Id, result.Error ?? "Unbekannter Sende-Fehler", result.ResponseRaw);
                    fehler++;
                }
            }

            // 3. Fehler-Cap -> Fehlermail
            if (fehler >= maxErrors && maxErrors > 0)
            {
                await NotifyErrorAsync("Sage-Lagerbuchung",
                    new Exception($"{fehler} fehlgeschlagene Sage-Buchungen in einem Lauf (Cap {maxErrors})."), ct);
            }

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gesendet"] = gesendet,
                ["bestaetigt"] = bestaetigt,
                ["fehler"] = fehler,
                ["nacherfasst"] = nacherfasst,
                ["uebersprungen"] = uebersprungen,
            }, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["gesendet"] = gesendet,
                ["bestaetigt"] = bestaetigt,
                ["fehler"] = fehler,
                ["nacherfasst"] = nacherfasst,
                ["uebersprungen"] = uebersprungen,
            }, ct: ct);
            throw;
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
