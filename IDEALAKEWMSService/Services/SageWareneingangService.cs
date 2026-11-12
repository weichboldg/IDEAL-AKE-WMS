using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E4, Variante B): ein Sende-Lauf der Sage-Wareneingangsbelege.
/// Offen-Receipts je SageWeVorgang = EIN ELL-Beleg. Status VOR dem POST auf Gesendet (+1 Versuch), danach Uebergeben bzw. Fehler.
/// KEIN automatisches Wiederholen: Fehler bleibt Fehler bis „Erneut senden“; hängendes Gesendet -> Fehler „Ergebnis unklar“, nicht neu gesendet.
/// Auto-Retry mit Sage-Prüfung (Variante A, E5) erst mit belegtem P-W9. Voraussetzung: genau EINE Dienst-Instanz (wie SageBookingWorker).
/// </summary>
public class SageWareneingangService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ISageWareneingangClient _client;
    private readonly ILogger<SageWareneingangService> _logger;
    private readonly ISyncLogger _syncLogger;

    public SageWareneingangService(ApplicationDbContext ctx, ISageWareneingangClient client, ILogger<SageWareneingangService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _client = client;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task RunOnceAsync(SageBookingEndpoint endpoint, int stuckMinutes, int batchSize, CancellationToken ct = default)
    {
        var grenze = DateTime.Now.AddMinutes(-Math.Max(1, stuckMinutes));
        var haengend = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Gesendet && (r.ModifiedAt ?? r.CreatedAt) < grenze).ToListAsync(ct);
        var vorgaenge = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Offen && r.SageWeVorgang != null)
            .Select(r => r.SageWeVorgang!.Value).Distinct().Take(Math.Max(1, batchSize)).ToListAsync(ct);
        if (haengend.Count == 0 && vorgaenge.Count == 0) return;   // kein Protokoll-Eintrag je Takt ohne Arbeit

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageWareneingang, ct);
        int gesendet = 0, uebergeben = 0, fehler = 0, unklar = 0;
        try
        {
            foreach (var g in haengend.GroupBy(r => r.SageWeVorgang))
            {
                var text = SageWareneingang.UnklarText(g.Min(r => r.Id));
                foreach (var r in g) Setze(r, SageWareneingangStatus.Fehler, fehlerText: text);
                unklar++;
                await run.LogWarningAsync(text, ct: ct);
            }
            if (haengend.Count > 0) await _ctx.SaveChangesAsync(ct);

            foreach (var vorgang in vorgaenge)
            {
                if (ct.IsCancellationRequested) break;
                var gruppe = await _ctx.PurchaseOrderReceipts
                    .Include(r => r.StockMovement).ThenInclude(m => m.Article)
                    .Include(r => r.StockMovement).ThenInclude(m => m.StorageLocation)
                    .Where(r => r.SageWeVorgang == vorgang && r.SageWeStatus == SageWareneingangStatus.Offen)
                    .OrderBy(r => r.Id).ToListAsync(ct);
                if (gruppe.Count == 0) continue;

                SageWareneingangRequest payload;
                try { payload = SageWareneingangPayloadBuilder.Build(gruppe); }
                catch (SageWareneingangPayloadException pex)
                {
                    foreach (var r in gruppe) Setze(r, SageWareneingangStatus.Fehler, fehlerText: pex.Message);
                    await _ctx.SaveChangesAsync(ct);
                    fehler++;
                    continue;
                }

                // Status VOR dem HTTP-Call (Timeout/Absturz -> „hängend“ statt blind doppelt senden)
                foreach (var r in gruppe) { Setze(r, SageWareneingangStatus.Gesendet); r.SageWeVersuche++; }
                await _ctx.SaveChangesAsync(ct);
                gesendet++;

                var result = await _client.SendAsync(payload, endpoint, ct);
                var jetzt = DateTime.Now;
                if (result.Success)
                {
                    foreach (var r in gruppe) { Setze(r, SageWareneingangStatus.Uebergeben); r.SageWeAm = jetzt; r.SageWeBelegnummer = Kappen(result.Belegnummer, 30); }
                    uebergeben++;
                }
                else
                {
                    var text = Kappen(string.Join(" | ", new[] { result.Error, result.ResponseRaw }.Where(s => !string.IsNullOrWhiteSpace(s))), 1000)
                               ?? "Unbekannter Sende-Fehler";
                    foreach (var r in gruppe) Setze(r, SageWareneingangStatus.Fehler, fehlerText: text);
                    fehler++;
                    // SyncLogs.Message ist nvarchar(1000) - Schreibtest 2026-11-12: laengerer Text liess den Protokoll-Eintrag scheitern
                    await run.LogWarningAsync(Kappen($"Sage-Wareneingang {payload.Memo} fehlgeschlagen: {text}", 900)!, ct: ct);
                }
                await _ctx.SaveChangesAsync(ct);
            }

            await run.FinishSuccessAsync(Counts(gesendet, uebergeben, fehler, unklar), ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Wareneingang-Lauf fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: Counts(gesendet, uebergeben, fehler, unklar), ct: ct);
            throw;
        }
    }

    private static Dictionary<string, int> Counts(int gesendet, int uebergeben, int fehler, int unklar) => new()
    {
        ["gesendet"] = gesendet, ["uebergeben"] = uebergeben, ["fehler"] = fehler, ["unklar"] = unklar,
    };

    private static void Setze(PurchaseOrderReceipt r, SageWareneingangStatus status, string? fehlerText = null)
    {
        r.SageWeStatus = status;
        if (status == SageWareneingangStatus.Fehler) r.SageWeFehler = Kappen(fehlerText, 1000);
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = SageWareneingang.ServiceActor;
        r.ModifiedByWindows = SageWareneingang.ServiceActor;
    }

    private static string? Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);
}
