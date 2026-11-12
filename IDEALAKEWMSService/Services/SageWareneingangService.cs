using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E4, Variante B): ein Sende-Lauf der Sage-Wareneingangsbelege.
/// Offen-Receipts je SageWeVorgang = EIN ELL-Beleg. Status VOR dem POST auf Gesendet (+1 Versuch), danach Uebergeben bzw. Fehler.
/// v1.99.7 E5 (Variante A, P-W9 belegt): ein Fehler wird automatisch wiederholt (Grenze und Abstand als Einstellung). Vor JEDEM erneuten Senden
/// (automatisch oder nach „Erneut senden“) sucht der Dienst in Sage nach der Memo-Kennung: genau ein Treffer = Uebergeben ohne POST, mehrere =
/// Fehler. Nur beim automatischen Wiederholen und 0 Memo-Treffern zusätzlich das Sicherungsnetz (Bestellung + Datum + Lieferschein): Treffer =
/// Klaeren (kein Senden). Scheitert die Suche, wird nicht gesendet. Voraussetzung: genau EINE Dienst-Instanz (wie SageBookingWorker).
/// </summary>
public sealed record SageWeWiederholung(int MaxWiederholungen, int AbstandMinuten);

public class SageWareneingangService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ISageWareneingangClient _client;
    private readonly ISageWareneingangLookup _lookup;
    private readonly ILogger<SageWareneingangService> _logger;
    private readonly ISyncLogger _syncLogger;

    public SageWareneingangService(ApplicationDbContext ctx, ISageWareneingangClient client, ISageWareneingangLookup lookup,
        ILogger<SageWareneingangService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _client = client;
        _lookup = lookup;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <summary>Q-2: bei ausgeschaltetem <see cref="SageWareneingang.ToggleKey"/> gehen liegengebliebene Offen/Fehler auf Klaeren - beim
    /// Wiedereinschalten wird nichts automatisch nachgeschickt (nur „Erneut senden“). Liefert die Anzahl; Protokoll nur bei Arbeit.</summary>
    public async Task<int> AusgeschaltetAsync(CancellationToken ct = default)
    {
        var liegen = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Offen || r.SageWeStatus == SageWareneingangStatus.Fehler).ToListAsync(ct);
        if (liegen.Count == 0) return 0;
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageWareneingang, ct);
        foreach (var r in liegen)
            Setze(r, SageWareneingangStatus.Klaeren, string.IsNullOrWhiteSpace(r.SageWeFehler) ? SageWareneingang.AusText : $"{SageWareneingang.AusText} Vorher: {r.SageWeFehler}");
        await _ctx.SaveChangesAsync(ct);
        await run.LogWarningAsync($"Sage-Wareneingang ist aus: {liegen.Count} Position(en) auf „Klären“ gesetzt, nicht gesendet.", ct: ct);
        await run.FinishSuccessAsync(new Dictionary<string, int> { ["klaeren"] = liegen.Count }, ct: ct);
        return liegen.Count;
    }

    public async Task RunOnceAsync(SageBookingEndpoint endpoint, int stuckMinutes, int batchSize, SageWeWiederholung wiederholung, CancellationToken ct = default)
    {
        var jetzt0 = DateTime.Now;
        var grenze = jetzt0.AddMinutes(-Math.Max(1, stuckMinutes));
        var haengend = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Gesendet && (r.ModifiedAt ?? r.CreatedAt) < grenze).ToListAsync(ct);
        var vorgaenge = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Offen && r.SageWeVorgang != null)
            .Select(r => r.SageWeVorgang!.Value).Distinct().Take(Math.Max(1, batchSize)).ToListAsync(ct);
        // E5: automatisch wiederholen nur nach einem echten Sende-Versuch (Versuche >= 1; ein Payload-Fehler ohne POST bringt nichts) und
        // hoechstens MaxWiederholungen-mal (Versuche zaehlt auch die Wiederholung „mehrere Treffer“, damit sie nicht endlos sucht).
        var abstand = jetzt0.AddMinutes(-Math.Max(1, wiederholung.AbstandMinuten));
        var max = Math.Max(0, wiederholung.MaxWiederholungen);
        var wiederholen = await _ctx.PurchaseOrderReceipts
            .Where(r => r.SageWeStatus == SageWareneingangStatus.Fehler && r.SageWeVorgang != null && r.SageWeVersuche >= 1
                        && r.SageWeVersuche <= max && (r.ModifiedAt ?? r.CreatedAt) < abstand)
            .Select(r => r.SageWeVorgang!.Value).Distinct().Take(Math.Max(1, batchSize)).ToListAsync(ct);
        if (haengend.Count == 0 && vorgaenge.Count == 0 && wiederholen.Count == 0) return;   // kein Protokoll-Eintrag je Takt ohne Arbeit

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.SageWareneingang, ct);
        int gesendet = 0, uebergeben = 0, fehler = 0, unklar = 0, gefunden = 0, klaeren = 0;
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

            foreach (var (vorgang, auto) in vorgaenge.Select(v => (v, false)).Concat(wiederholen.Except(vorgaenge).Select(v => (v, true))))
            {
                if (ct.IsCancellationRequested) break;
                var status = auto ? SageWareneingangStatus.Fehler : SageWareneingangStatus.Offen;
                var gruppe = await _ctx.PurchaseOrderReceipts
                    .Include(r => r.StockMovement).ThenInclude(m => m.Article)
                    .Include(r => r.StockMovement).ThenInclude(m => m.StorageLocation)
                    .Where(r => r.SageWeVorgang == vorgang && r.SageWeStatus == status)
                    .OrderBy(r => r.Id).ToListAsync(ct);
                if (gruppe.Count == 0) continue;

                // E5: vor jedem ERNEUTEN Senden in Sage nachsehen (Erstversand: Versuche = 0, keine Suche)
                if (auto || gruppe.Any(r => r.SageWeVersuche > 0))
                {
                    var memo = SageWareneingang.Memo(gruppe[0].Id);
                    var p = await PruefenAsync(gruppe, auto, ct);
                    // QA v1.99.7 F-1: Waehrend der Sage-Suche (Sekunden) kann die Web-Seite die Gruppe geaendert haben („Als uebergeben markieren“,
                    // „Erneut senden“). Stand neu lesen - sonst ueberschreibt das Speichern den Handgriff bzw. sendet trotz Uebergeben.
                    // ponytail: Restfenster im Millisekundenbereich bis zum Speichern; schliessen nur mit rowversion (Migration).
                    var ids = gruppe.Select(r => r.Id).ToList();
                    if (await _ctx.PurchaseOrderReceipts.AsNoTracking().CountAsync(r => ids.Contains(r.Id) && r.SageWeStatus == status, ct) != gruppe.Count)
                    {
                        await run.LogWarningAsync($"Sage-Wareneingang {memo}: während der Sage-Prüfung von Hand geändert – übersprungen, nichts gesendet.", ct: ct);
                        continue;
                    }
                    if (p is not null)
                    {
                        foreach (var r in gruppe)
                        {
                            Setze(r, p.Neu, p.Text);
                            if (auto && p.Mehrfach) r.SageWeVersuche++;
                            if (p.Neu == SageWareneingangStatus.Uebergeben) { r.SageWeAm = DateTime.Now; r.SageWeBelegnummer = Kappen(p.Beleg, 30); }
                        }
                        await _ctx.SaveChangesAsync(ct);
                        if (p.Neu == SageWareneingangStatus.Uebergeben) { gefunden++; uebergeben++; }
                        else if (p.Neu == SageWareneingangStatus.Klaeren) klaeren++;
                        else fehler++;
                        await run.LogWarningAsync(Kappen(p.Neu == SageWareneingangStatus.Uebergeben
                            ? $"Sage-Wareneingang {memo}: Beleg {p.Beleg} in Sage gefunden, nicht erneut gesendet."
                            : $"Sage-Wareneingang {memo}: {p.Text}", 900)!, ct: ct);
                        continue;
                    }
                }

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

            await run.FinishSuccessAsync(Counts(gesendet, uebergeben, fehler, unklar, gefunden, klaeren), ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sage-Wareneingang-Lauf fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: Counts(gesendet, uebergeben, fehler, unklar, gefunden, klaeren), ct: ct);
            throw;
        }
    }

    private sealed record Pruefung(SageWareneingangStatus Neu, string? Text, string? Beleg, bool Mehrfach = false);

    /// <summary>E5: Sage-Suche vor dem erneuten Senden. null = senden.</summary>
    private async Task<Pruefung?> PruefenAsync(List<PurchaseOrderReceipt> gruppe, bool auto, CancellationToken ct)
    {
        var erste = gruppe[0];
        var memo = SageWareneingang.Memo(erste.Id);
        try
        {
            var treffer = await _lookup.NachMemoAsync(erste.Id, ct);
            if (treffer.Count == 1) return new(SageWareneingangStatus.Uebergeben, null, treffer[0].Anzeige);
            if (treffer.Count > 1)
                return new(SageWareneingangStatus.Fehler,
                    $"Mehrere Sage-Belege mit Memo {memo}: {string.Join(", ", treffer.Select(t => t.Anzeige))}. Nicht gesendet – bitte in Sage klären.", null, Mehrfach: true);
            if (!auto) return null;   // „Erneut senden“ von Hand: der Mensch hat geprueft, kein Sicherungsnetz
            var kandidaten = (await _lookup.KandidatenAsync(erste.BelId, erste.CreatedAt.Date, erste.Lieferschein, ct))
                .Where(t => !SdataWareneingangLookupReader.FremdeKennung(t.Memo, erste.Id)).ToList();
            return kandidaten.Count == 0 ? null
                : new(SageWareneingangStatus.Klaeren, SageWareneingang.KlaerenText(kandidaten.Select(t => t.Anzeige)), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Sage-Pruefung vor dem erneuten Senden fehlgeschlagen ({Memo}).", memo);
            return new(SageWareneingangStatus.Fehler,
                $"Sage-Prüfung vor dem erneuten Senden fehlgeschlagen ({ex.GetBaseException().Message}) – nicht gesendet, nächster Versuch später.", null);
        }
    }

    private static Dictionary<string, int> Counts(int gesendet, int uebergeben, int fehler, int unklar, int gefunden, int klaeren) => new()
    {
        ["gesendet"] = gesendet, ["uebergeben"] = uebergeben, ["fehler"] = fehler, ["unklar"] = unklar, ["in Sage gefunden"] = gefunden, ["klaeren"] = klaeren,
    };

    private static void Setze(PurchaseOrderReceipt r, SageWareneingangStatus status, string? fehlerText = null)
    {
        r.SageWeStatus = status;
        if (status is SageWareneingangStatus.Fehler or SageWareneingangStatus.Klaeren) r.SageWeFehler = Kappen(fehlerText, 1000);
        else if (status == SageWareneingangStatus.Uebergeben) r.SageWeFehler = null;
        r.ModifiedAt = DateTime.Now;
        r.ModifiedBy = SageWareneingang.ServiceActor;
        r.ModifiedByWindows = SageWareneingang.ServiceActor;
    }

    private static string? Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);
}
