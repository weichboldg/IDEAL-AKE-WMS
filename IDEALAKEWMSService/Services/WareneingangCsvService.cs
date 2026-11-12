using System.Text;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>v1.99.11: Grenzen der automatischen Wiederholung nach einem Schreibfehler (Freigabe-Antwort 1d).</summary>
public sealed record WareneingangCsvWiederholung(int MaxWiederholungen, int AbstandMinuten);

/// <summary>
/// v1.99.11 (Spec 2026-11-12-wareneingang-barcode-lieferschein-csv-export F2/F3): ein Lauf der Wareneingang-CSV im Takt des SageBookingWorker,
/// nach dem Sage-WE-Schritt. Je Sage-WE-Vorgang mit Barcode, dessen Receipts alle „an Sage übergeben“ sind, eine Ausgangszeile; je Zeile
/// <c>&lt;Barcode&gt;.tmp</c> schreiben und auf <c>&lt;Barcode&gt;.csv</c> umbenennen (nie ueberschreiben). Das Gate (Schalter, Sage-WE an, Pfad)
/// prueft der Worker; hier nur „Pfad leer = nichts“. Voraussetzung: genau EINE Dienst-Instanz (wie SageBookingWorker).
/// </summary>
public class WareneingangCsvService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<WareneingangCsvService> _logger;
    private readonly ISyncLogger _syncLogger;

    public WareneingangCsvService(ApplicationDbContext ctx, ILogger<WareneingangCsvService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task RunOnceAsync(string? pfad, WareneingangCsvWiederholung wiederholung, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pfad)) return;
        pfad = pfad.Trim();
        var jetzt0 = DateTime.Now;

        // 1. Ausgangszeilen fuer neu uebergebene Vorgaenge mit Barcode (auch nach „Als übergeben markieren“)
        var neu = (await _ctx.PurchaseOrderReceipts
                .Where(r => r.Barcode != null && r.SageWeVorgang != null && r.SageWeStatus == SageWareneingangStatus.Uebergeben
                            && !_ctx.WareneingangCsvExporte.Any(e => e.SageWeVorgang == r.SageWeVorgang)
                            && !_ctx.PurchaseOrderReceipts.Any(o => o.SageWeVorgang == r.SageWeVorgang && o.SageWeStatus != SageWareneingangStatus.Uebergeben))
                .Select(r => new { Vorgang = r.SageWeVorgang!.Value, Barcode = r.Barcode! }).ToListAsync(ct))
            .GroupBy(x => x.Vorgang).Select(g => g.First()).ToList();
        foreach (var n in neu)
            _ctx.WareneingangCsvExporte.Add(new WareneingangCsvExport
            {
                SageWeVorgang = n.Vorgang, Barcode = n.Barcode, Status = WareneingangCsvStatus.Offen,
                CreatedAt = jetzt0, CreatedBy = WareneingangCsv.ServiceActor, CreatedByWindows = WareneingangCsv.ServiceActor,
            });
        if (neu.Count > 0) await _ctx.SaveChangesAsync(ct);

        // 2. Offene plus faellige Wiederholungen (Kollision/ungueltig nie automatisch), hoechstens MaxDateienJeTakt (Freigabe-Antwort 1f)
        var abstand = jetzt0.AddMinutes(-Math.Max(1, wiederholung.AbstandMinuten));
        var max = Math.Max(0, wiederholung.MaxWiederholungen);
        var faellig = await _ctx.WareneingangCsvExporte
            .Where(e => e.Status == WareneingangCsvStatus.Offen
                        || (e.Status == WareneingangCsvStatus.Fehler && e.Versuche >= 1 && e.Versuche <= max
                            && (e.ModifiedAt ?? e.CreatedAt) < abstand && !WareneingangCsv.NieWiederholen.Contains(e.Fehler)))
            .OrderBy(e => e.Id).Take(WareneingangCsv.MaxDateienJeTakt).ToListAsync(ct);
        if (faellig.Count == 0) return;   // kein Protokoll-Eintrag je Takt ohne Arbeit (ADR 0010)

        var vorgaenge = faellig.Select(e => e.SageWeVorgang).ToList();
        var erste = (await _ctx.PurchaseOrderReceipts.AsNoTracking()
                .Where(r => r.SageWeVorgang != null && vorgaenge.Contains(r.SageWeVorgang.Value)).ToListAsync(ct))
            .GroupBy(r => r.SageWeVorgang!.Value).ToDictionary(g => g.Key, g => g.MinBy(r => r.Id)!);

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.WareneingangCsv, ct);
        int geschrieben = 0, fehler = 0;
        try
        {
            foreach (var e in faellig)
            {
                if (ct.IsCancellationRequested) break;
                var text = erste.TryGetValue(e.SageWeVorgang, out var r) ? Schreiben(pfad, e, r) : "Wareneingang zum Vorgang nicht gefunden – nicht geschrieben.";
                var now = DateTime.Now;
                if (text == null)
                {
                    e.Status = WareneingangCsvStatus.Geschrieben;
                    e.Fehler = null;
                    e.Datei = Kappen(Path.Combine(pfad, e.Barcode + ".csv"), 400);
                    e.GeschriebenAm = now;
                    geschrieben++;
                }
                else
                {
                    e.Status = WareneingangCsvStatus.Fehler;
                    e.Fehler = Kappen(text, 1000);
                    e.Versuche++;
                    fehler++;
                    // SyncLogs.Message ist nvarchar(1000) (fallstricke §31)
                    await run.LogWarningAsync(Kappen($"Wareneingang-CSV {e.Barcode}: {text}", 900)!, ct: ct);
                }
                e.ModifiedAt = now; e.ModifiedBy = WareneingangCsv.ServiceActor; e.ModifiedByWindows = WareneingangCsv.ServiceActor;
                await _ctx.SaveChangesAsync(ct);
            }
            await run.FinishSuccessAsync(new Dictionary<string, int> { ["geschrieben"] = geschrieben, ["fehler"] = fehler }, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Wareneingang-CSV-Lauf fehlgeschlagen.");
            await run.LogErrorAsync(Kappen(ex.Message, 900)!, ct: ct);
            await run.FinishFailedAsync(Kappen(ex.Message, 900)!, counts: new Dictionary<string, int> { ["geschrieben"] = geschrieben, ["fehler"] = fehler }, ct: ct);
            throw;
        }
    }

    /// <summary>Eine Datei atomar schreiben: <c>.tmp</c> (ueberschreibt nur die eigene Leiche desselben Barcodes), dann Umbenennen ohne
    /// Ueberschreiben auf derselben Freigabe. Liefert null bei Erfolg, sonst den Fehlertext.</summary>
    private string? Schreiben(string pfad, WareneingangCsvExport e, PurchaseOrderReceipt r)
    {
        if (!WareneingangCsv.IstGueltigerBarcode(e.Barcode)) return WareneingangCsv.UngueltigText;   // Altwert in der DB
        var ziel = Path.Combine(pfad, e.Barcode + ".csv");
        var tmp = Path.Combine(pfad, e.Barcode + ".tmp");
        try
        {
            if (File.Exists(ziel)) return WareneingangCsv.KollisionText;
            File.WriteAllText(tmp, CsvInhalt(r, e.Barcode), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            File.Move(tmp, ziel, overwrite: false);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wareneingang-CSV {Barcode} nicht geschrieben ({Pfad}).", e.Barcode, pfad);
            try { if (File.Exists(tmp)) File.Delete(tmp); }
            catch (Exception tex) { _logger.LogWarning(tex, "Wareneingang-CSV: {Tmp} nicht geloescht.", tmp); }   // best effort
            return ex.Message;
        }
    }

    /// <summary>F3a (Freigabe-Antworten 1c, 2, 3): Kopfzeile + eine Datenzeile, Trennzeichen <c>;</c>, CRLF, Datum <c>yyyy-MM-dd HH:mm</c>,
    /// Felder mit <c>;</c>, <c>"</c> oder Zeilenumbruch in Anfuehrungszeichen (inneres <c>"</c> verdoppelt), leere Werte leer.
    /// Das BOM setzt der Schreiber (UTF-8 mit BOM).</summary>
    public static string CsvInhalt(PurchaseOrderReceipt r, string barcode)
    {
        static string F(string? v) => string.IsNullOrEmpty(v) ? ""
            : v.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        static string D(DateTime? d) => d?.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        return "Barcode;Kreditornummer;Kreditorname;Bestellnummer;Lieferschein;SageBelegnummer;Erfasser;Datum;UebergebenAm\r\n"
            + string.Join(";", F(barcode), F(r.LieferantKto), F(r.LieferantName), F(r.Belegnummer), F(r.Lieferschein), F(r.SageWeBelegnummer),
                F(r.CreatedBy), D(r.CreatedAt), D(r.SageWeAm)) + "\r\n";
    }

    private static string? Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);
}
