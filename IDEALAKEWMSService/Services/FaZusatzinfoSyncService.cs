using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// FA-Zusatzinfos aus Sage (v1.26.0, Spec §4 + §10): liest die View ueber den Reader-Seam
/// und upsertet den 1:1-Satelliten <see cref="ProductionOrderExtraInfo"/> per EF
/// (shared ApplicationDbContext — kompletter Entscheidungs- UND Schreibpfad InMemory-
/// testbar). Mehrfach-treffer-faehig (IDEAL-Linie: mehrere ProductionOrders je
/// WA-Nummer → Upsert je Id). Kein Loeschen: verschwindet ein WA aus der View,
/// bleibt der letzte Stand stehen.
/// Fold 2 (Spec §10): Sage-Status verpackt/abgeholt setzt offene FAs
/// (!IsDone &amp;&amp; !IsCancelled &amp;&amp; !PickingStatus.IsDonePicking) automatisch
/// auf Komm-Erledigt — einweg, zweiphasig mit Sicherheits-Cap
/// <c>autoDoneMaxPerRun</c> (mehr Kandidaten → kein Write + Warn + Fehlermail).
/// DryRun = voller Plan inkl. echter Would-be-Counts; JEDER Write (Upsert UND
/// Auto-Erledigt) liegt hinter einem if(!dryRun)-Guard VOR der Mutation getrackter
/// Entities — der Service laeuft im zyklusweiten Scope, ein spaeterer SaveChanges
/// wuerde mutierte Entities sonst mitflushen.
/// </summary>
public class FaZusatzinfoSyncService : IFaZusatzinfoSyncService
{
    private const string SyncUser = "FaZusatzinfoSync";
    private const int MaxDetailLinesPerRun = 100;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageZusatzinfoReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaZusatzinfoSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaZusatzinfoSyncService(
        ApplicationDbContext ctx,
        ISageZusatzinfoReader reader,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaZusatzinfoSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> SyncAsync(bool dryRun, int autoDoneMaxPerRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaZusatzinfo, ct);
        int read = 0, inserted = 0, updated = 0, skipped = 0, erledigtGesetzt = 0, erledigtKandidaten = 0;

        try
        {
            var readResult = await _reader.ReadAsync(ct);

            // View-fehlt-Guard (Spec §2): Warn-Zeile + regulaeres Lauf-Ende — bewusst
            // KEIN Fehlerpfad (kein throw, keine Fehlermail alle 15 min).
            if (!readResult.ViewExists)
            {
                await run.LogWarningAsync(
                    "View dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync uebersprungen.", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["gelesen"] = 0,
                    ["neu"] = 0,
                    ["aktualisiert"] = 0,
                    ["uebersprungen"] = 0,
                    ["erledigt-gesetzt"] = 0,
                }, messageSuffix: dryRun ? "View nicht vorhanden [DryRun]" : "View nicht vorhanden", ct: ct);
                return new SyncResult(0, 0, 0, "View nicht vorhanden.");
            }

            var rows = readResult.Rows;
            read = rows.Count;

            // Einmaliger Set-Read der FA-Zuordnung (kein Zeile-fuer-Zeile-Roundtrip):
            // OrderNumber -> List<ProductionOrder> inkl. ExtraInfo-Satellit.
            // Fold 2: + PickingStatus (ein LEFT JOIN mehr, kein zweiter Roundtrip).
            var waNumbers = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.WaNummer))
                .Select(r => r.WaNummer!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var orders = await _ctx.ProductionOrders
                .Include(o => o.ExtraInfo)
                .Include(o => o.PickingStatus)
                .Where(o => waNumbers.Contains(o.OrderNumber))
                .ToListAsync(ct);

            var ordersByNumber = orders
                .GroupBy(o => o.OrderNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Fold 2 (Spec §10.2): Auto-Erledigt-Kandidaten NUR sammeln (zweiphasig) —
            // Writes laufen nach der Schleife hinter dem Cap. Dictionary-Keys = Order-Ids
            // (dedupliziert hypothetische View-Duplikate -> kein DryRun-Doppelcount).
            var autoDoneCandidates = new Dictionary<int, (ProductionOrder Order, string SageStatus)>();

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();

                var wa = row.WaNummer?.Trim();
                if (string.IsNullOrWhiteSpace(wa) || !ordersByNumber.TryGetValue(wa, out var matches))
                {
                    // Kein FA-Treffer: alte/erledigte WAs sind normal — Count, keine Warn-Zeile je WA.
                    skipped++;
                    continue;
                }

                foreach (var order in matches)
                {
                    var info = order.ExtraInfo;
                    if (info == null)
                    {
                        if (!dryRun)
                        {
                            _ctx.ProductionOrderExtraInfos.Add(new ProductionOrderExtraInfo
                            {
                                ProductionOrderId = order.Id,
                                Kaeltemittel = row.Kaeltemittel,
                                Ventil = row.Ventil,
                                AusfuehrungEZ = row.AusfuehrungEZ,
                                Maschine = row.Maschine,
                                SageStatus = row.Status,
                                CreatedAt = DateTime.Now,
                                CreatedBy = SyncUser,
                                CreatedByWindows = SyncUser,
                            });
                        }
                        inserted++;
                    }
                    else if (info.Kaeltemittel != row.Kaeltemittel
                          || info.Ventil != row.Ventil
                          || info.AusfuehrungEZ != row.AusfuehrungEZ
                          || info.Maschine != row.Maschine
                          || info.SageStatus != row.Status)
                    {
                        if (!dryRun)
                        {
                            info.Kaeltemittel = row.Kaeltemittel;
                            info.Ventil = row.Ventil;
                            info.AusfuehrungEZ = row.AusfuehrungEZ;
                            info.Maschine = row.Maschine;
                            info.SageStatus = row.Status;
                            info.ModifiedAt = DateTime.Now;
                            info.ModifiedBy = SyncUser;
                            info.ModifiedByWindows = SyncUser;
                        }
                        updated++;
                    }
                    // unveraendert -> kein Write, kein Count (haelt ModifiedAt aussagekraeftig)

                    // Fold 2 (Spec §10.1): Done-Check je gematchter FA-Zeile — UNABHAENGIG
                    // vom Feld-Diff (kein continue in der Kette -> greift auch im
                    // unveraendert-Zweig). Statusquelle ist row.Status (frischer View-Wert),
                    // NICHT info.SageStatus. Tripel: Sage-IsDone wird NIE beschrieben,
                    // stornierte FAs uebersprungen (sonst kaeme eine reaktivierte FA mit
                    // klebendem IsDonePicking dauerhaft versteckt zurueck).
                    if (FaZusatzinfoStatus.IstVerpacktOderAbgeholt(row.Status)
                        && !order.IsDone
                        && !order.IsCancelled
                        && order.PickingStatus?.IsDonePicking != true
                        && !autoDoneCandidates.ContainsKey(order.Id))
                    {
                        autoDoneCandidates[order.Id] = (order, row.Status!.Trim());
                    }
                }
            }

            // Fold 2 (Spec §10.2/§10.3): Write-Phase mit Sicherheits-Cap. Ein View-Defekt
            // (Status-Spalte flaechendeckend "abgeholt") wuerde sonst in EINEM Lauf alle
            // offenen FAs schliessen — still, einweg, ohne Bulk-Reopen.
            if (autoDoneCandidates.Count > autoDoneMaxPerRun)
            {
                erledigtKandidaten = autoDoneCandidates.Count;
                var warnMessage =
                    $"Auto-Erledigt uebersprungen: {autoDoneCandidates.Count} Kandidaten > Cap {autoDoneMaxPerRun} — moeglicher View-Defekt";
                await run.LogWarningAsync(warnMessage, ct: ct);
                await _errorNotifier.NotifyAsync("FA-Zusatzinfo Auto-Erledigt",
                    new InvalidOperationException(warnMessage), ct);
                // KEIN throw — der Upsert-Teil des Laufs bleibt gueltig und wird gespeichert.
            }
            else
            {
                var detailLines = 0;
                foreach (var (order, sageStatus) in autoDoneCandidates.Values)
                {
                    // DryRun-Guard VOR der Mutation (Spec §10.1): im DryRun wird KEINE
                    // getrackte Entity mutiert; Counts + Detailzeilen laufen ausserhalb.
                    if (!dryRun)
                    {
                        if (order.PickingStatus == null)
                        {
                            // Altbestand-Randfall: PickingStatus-Zeile fehlt -> eager anlegen
                            // (Muster ProductionOrderPickingStatusRepository.SetFieldAsync).
                            _ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
                            {
                                ProductionOrderId = order.Id,
                                IsDonePicking = true,
                                CreatedAt = DateTime.Now,
                                CreatedBy = SyncUser,
                                CreatedByWindows = SyncUser,
                            });
                        }
                        else
                        {
                            order.PickingStatus.IsDonePicking = true;
                            order.PickingStatus.ModifiedAt = DateTime.Now;
                            order.PickingStatus.ModifiedBy = SyncUser;
                            order.PickingStatus.ModifiedByWindows = SyncUser;
                        }
                    }

                    erledigtGesetzt++;

                    // Detailzeilen gecappt (Muster Hauptlagerplatz-Warnzeilen) —
                    // der Count zaehlt weiterhin ALLE.
                    if (detailLines < MaxDetailLinesPerRun)
                    {
                        detailLines++;
                        await run.LogInfoAsync(
                            $"FA {order.OrderNumber} auf erledigt gesetzt (Sage-Status: {sageStatus})",
                            reference: order.OrderNumber, ct: ct);
                    }
                }
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            _logger.LogInformation(
                "FA-Zusatzinfo-Sync abgeschlossen: {Read} gelesen, {Inserted} neu, {Updated} aktualisiert, {Skipped} uebersprungen, {Done} erledigt gesetzt{DryRun}",
                read, inserted, updated, skipped, erledigtGesetzt, dryRun ? " [DryRun]" : "");

            var counts = new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
                ["erledigt-gesetzt"] = erledigtGesetzt,
            };
            if (erledigtKandidaten > 0)
                counts["erledigt-kandidaten"] = erledigtKandidaten;

            await run.FinishSuccessAsync(counts, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(inserted, updated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim FA-Zusatzinfo-Sync.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["gelesen"] = read,
                ["neu"] = inserted,
                ["aktualisiert"] = updated,
                ["uebersprungen"] = skipped,
                ["erledigt-gesetzt"] = erledigtGesetzt,
            }, ct: ct);
            throw;
        }
    }
}
