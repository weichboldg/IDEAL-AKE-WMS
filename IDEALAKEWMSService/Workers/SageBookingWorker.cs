using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
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
/// 0. Reconciliation-Sweep (B4): verpasste Ein-/Ausbuchungen (und Umbuchungen, Schalter <c>SageLagerbuchungUmbuchungAktiv</c>) nachtraeglich einreihen.
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

    // Kurzes Rueckblickfenster fuer den Reconciliation-Sweep: faengt in erster Linie Enqueue-Fehler
    // (Crash zwischen Buchung und Queue-Insert). BEWUSSTER TRADEOFF: liegt eine Toggle-Aus-Phase
    // innerhalb dieses Fensters, werden die in ihr gebuchten Ein-/Ausbuchungen beim Wiedereinschalten
    // mit nachgesendet (das Fenster begrenzt den Umfang, schliesst diesen Fall aber nicht voellig aus).
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

        // Beim Start warnen, falls die TLS-Zertifikatspruefung deaktiviert ist (nur Testsysteme).
        var tlsRaw = await ServiceSettings.GetValueSafeAsync(_configuration, SageTlsPolicy.SettingKey, stoppingToken);
        if (!SageTlsPolicy.ShouldVerifyCertificate(tlsRaw))
            _logger.LogWarning(SageTlsPolicy.DisabledWarning);

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = await ServiceSettings.GetIntSafeAsync(
                _configuration, "Sync:SageLagerbuchungIntervalSeconds", 20, stoppingToken);
            if (intervalSeconds < 5) intervalSeconds = 5;   // Untergrenze gegen DB-Last

            var sageWeAn = false;   // v1.99.11: derselbe gelesene Wert gated die Wareneingang-CSV (Lesefehler = aus)
            try
            {
                if (await ServiceSettings.GetBoolSafeAsync(_configuration, "SageLagerbuchungAktiv", false, stoppingToken))
                    await ProcessOnceAsync(stoppingToken);
                // v1.99.4 (Wareneingang Teil 2, E4): eigener Schritt, NUR durch SageWareneingangBelegAktiv geschaltet (unabhaengig von
                // SageLagerbuchungAktiv); dieselbe EINE Instanz ist zustaendig (Idempotenz "Status vor dem Call").
                // QA v1.99.7 F-2: NICHT die Safe-Variante - ein DB-Lesefehler darf nicht als „aus“ gelten und Q-2 (Offen/Fehler -> Klären)
                // ausloesen; er landet im catch unten, der Takt wird wiederholt.
                sageWeAn = await ServiceSettings.GetBoolAsync(_configuration, SageWareneingang.ToggleKey, false, stoppingToken);
                if (sageWeAn)
                    await ProcessSageWareneingangAsync(stoppingToken);
                else
                    await SageWareneingangAusAsync(stoppingToken);   // v1.99.7 Q-2: Liegengebliebenes auf „Klären“, nie nachschicken
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

            // v1.99.11 (Wareneingang-CSV F3): eigener Schritt NACH dem Sage-WE-Schritt (im selben Takt uebergeben = sofort CSV), nur bei
            // eingeschaltetem Sage-Wareneingang. Eigenes try: ein Fehler hier haelt die Sage-Buchungen nicht auf und umgekehrt.
            try
            {
                if (sageWeAn) await ProcessWareneingangCsvAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler im Schritt Wareneingang-CSV.");
                await NotifyErrorAsync("SageBookingWorker (Wareneingang-CSV)", ex, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("SageBookingWorker gestoppt.");
    }

    private async Task ProcessSageWareneingangAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SageWareneingangService>();
        var batchSize = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungBatchSize", 50, ct);
        var stuckMinutes = await ServiceSettings.GetIntSafeAsync(_configuration, "Sync:SageLagerbuchungStuckMinutes", 10, ct);
        // v1.99.7 E5: Wiederholgrenzen (ADR 0020, Katalog ServiceSettingDefinitions)
        var wiederholung = new SageWeWiederholung(
            await ServiceSettings.GetIntSafeAsync(_configuration, SageWareneingang.MaxWiederholungenKey, 3, ct),
            await ServiceSettings.GetIntSafeAsync(_configuration, SageWareneingang.WiederholAbstandKey, 15, ct));
        // v1.99.9: System-Kuerzel der Kennung WE#[K#]<id>#; Lesefehler/ungueltig -> Takt abbrechen (nichts senden), nicht ohne Kuerzel senden.
        var kuerzel = SageSystemKuerzel.Pruefen(await ServiceSettings.GetValueAsync(_configuration, SageSystemKuerzel.Key, ct));
        var (endpoint, zugang) = await BuildEndpointAsync(ct);
        if (zugang.Fehler is { } fehler)
        {
            // v1.99.14 F6: nicht entschluesselbar = nichts senden, kein Rueckfall. Einmal je Zustandswechsel protokollieren (Takt 20 s).
            if (_weZugangGemeldet != fehler)
            {
                await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageWareneingang, ct);
                await run.LogErrorAsync(fehler, ct: ct);
                await run.FinishFailedAsync(fehler, ct: ct);
                _weZugangGemeldet = fehler;
            }
            return;
        }
        _weZugangGemeldet = null;
        await service.RunOnceAsync(endpoint, stuckMinutes, batchSize, wiederholung, kuerzel, ct, zugang.Warnung);
    }

    private string? _weZugangGemeldet;

    private async Task ProcessWareneingangCsvAsync(CancellationToken ct)
    {
        // Lesefehler = aus bzw. leer = nichts schreiben (harmlos, anders als Q-2)
        if (!await ServiceSettings.GetBoolSafeAsync(_configuration, WareneingangCsv.AktivKey, false, ct)) return;
        var pfad = await ServiceSettings.GetValueSafeAsync(_configuration, WareneingangCsv.PfadKey, ct);
        if (string.IsNullOrWhiteSpace(pfad)) return;   // Hinweis in Liste, /ServiceSettings und Manifest-Pruefung
        var wiederholung = new WareneingangCsvWiederholung(
            await ServiceSettings.GetIntSafeAsync(_configuration, WareneingangCsv.MaxWiederholungenKey, 3, ct),
            await ServiceSettings.GetIntSafeAsync(_configuration, WareneingangCsv.WiederholAbstandKey, 15, ct));
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<WareneingangCsvService>().RunOnceAsync(pfad, wiederholung, ct);
    }

    private async Task SageWareneingangAusAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<SageWareneingangService>().AusgeschaltetAsync(ct);
    }

    /// <summary>EINE Endpunkt-Erzeugung fuer Lagerbuchung und Sage-Wareneingang (v1.99.14). Zugangsdaten ueber
    /// <see cref="IDEALAKEWMSService.Services.Sdata.SdataLeseZielLeser.ZugangAsync"/> (DB-first, Kennwort verschluesselt, F6).
    /// Application/ServiceContract: Fallback auf die Katalog-Defaults, falls der DB-Read scheitert (null).</summary>
    private async Task<(SageBookingEndpoint Endpoint, IdealAkeWms.Services.SageSdata.SdataZugang Zugang)> BuildEndpointAsync(CancellationToken ct)
    {
        var zugang = await IDEALAKEWMSService.Services.Sdata.SdataLeseZielLeser.ZugangAsync(_configuration, ct);
        return (new SageBookingEndpoint(
            BaseUrl: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:BaseUrl", ct) ?? string.Empty,
            Application: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:Application", ct) ?? "ol",
            ServiceContract: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:ServiceContract", ct) ?? "CommonWawiServices",
            Dataset: await ServiceSettings.GetValueSafeAsync(_configuration, "SData:Dataset", ct) ?? string.Empty,
            Username: zugang.Benutzer,
            Password: zugang.Kennwort), zugang);
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
        // v1.73.0 E7: Umbuchungen nur mit eigenem Schalter (Default aus = Nacherfassung wie bisher nur Ein-/Ausbuchung).
        var umbuchungAktiv = await ServiceSettings.GetBoolSafeAsync(_configuration, SageBookingEnqueueDecision.UmbuchungToggleKey, false, ct);
        // v1.99.9: System-Kuerzel der Kennung SM#[K#]<id>#. Nicht die Safe-Variante: ein Lesefehler bzw. ein ungueltiger Wert bricht den Takt
        // ab (catch in ExecuteAsync), statt ohne Kuerzel zu senden.
        var kuerzel = SageSystemKuerzel.Pruefen(await ServiceSettings.GetValueAsync(_configuration, SageSystemKuerzel.Key, ct));

        var (endpoint, zugang) = await BuildEndpointAsync(ct);

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageLagerbuchung, ct);
        int gesendet = 0, bestaetigt = 0, fehler = 0, nacherfasst = 0, uebersprungen = 0;
        // v1.99.14 F6: Kennwort nicht entschluesselbar = in diesem Lauf nichts an Sage, kein Rueckfall; Rueckfall appsettings = Warnung.
        if (zugang.Fehler is { } zugangsFehler)
        {
            await run.LogErrorAsync(zugangsFehler, ct: ct);
            await run.FinishFailedAsync(zugangsFehler, ct: ct);
            return;
        }
        if (zugang.Warnung is { } zugangsWarnung)
            await run.LogWarningAsync(zugangsWarnung, ct: ct);

        try
        {
            // 0. Reconciliation-Sweep (B4)
            nacherfasst = await queue.EnqueueMissingAsync(DateTime.Now - ReconcileLookback, batchSize, umbuchungAktiv);
            if (nacherfasst > 0)
                await run.LogInfoAsync($"{nacherfasst} Buchung(en) ohne Queue-Eintrag nachtraeglich eingereiht.", ct: ct);

            // 1. Recovery haengender Gesendet-Eintraege (S3) via Sage-Lookup (B2)
            var stuck = await queue.GetStuckSentAsync(DateTime.Now.AddMinutes(-stuckMinutes), batchSize);
            foreach (var item in stuck)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    if (await lookup.ExistsAsync(SageBookingCorrelation.Kennung(item, kuerzel), ct))
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
                    payload = SageLagerbuchungPayloadBuilder.Build(item.StockMovement, SageBookingCorrelation.Kennung(item, kuerzel));
                }
                catch (SageBookingPayloadException pex)
                {
                    // AK9: ungueltige Buchung sauber als Fehler markieren statt zu senden/abzustuerzen.
                    await queue.MarkFailedAsync(item.Id, pex.Message, null);
                    fehler++;
                    continue;
                }

                // B2: Wurde dieser Eintrag schon einmal gesendet (Requeue nach Timeout/Fehler)?
                // Dann VOR dem erneuten Senden in Sage pruefen — nie blind doppelt buchen.
                if (item.SentAt != null)
                {
                    try
                    {
                        if (await lookup.ExistsAsync(SageBookingCorrelation.Kennung(item, kuerzel), ct))
                        {
                            await queue.MarkConfirmedAsync(item.Id,
                                "Requeue: Buchung in Sage bereits vorhanden (Memo-Lookup).");
                            bestaetigt++;
                            continue;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Lookup unsicher -> NICHT senden (Doppelbuchungs-Schutz geht vor).
                        await queue.MarkFailedAsync(item.Id,
                            $"Sage-Lookup vor Requeue fehlgeschlagen, kein erneutes Senden: {ex.Message}", null);
                        fehler++;
                        continue;
                    }
                }

                // AK10: Status VOR dem HTTP-Call auf Gesendet (Timeout -> kein zweiter Auto-Versuch).
                await queue.MarkSentAsync(item.Id, SageBookingCorrelation.Kennung(item, kuerzel));
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

            // Quelle des Memo-Lookups (Sync:Quelle:LagerbuchungLookup, v1.73.0 E7), nur wenn in diesem Lauf gesucht wurde.
            if (lookup is IDEALAKEWMSService.Services.Sdata.SageQuelleWeiche { GenutzteQuelle: { } quelle })
                await run.LogInfoAsync($"Quelle Lookup: {quelle}", ct: ct);

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
