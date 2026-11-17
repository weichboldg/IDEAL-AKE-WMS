using System.Text;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services;

/// <summary>Grenzen der automatischen Wiederholung nach einem Schreibfehler (wie <see cref="WareneingangCsvWiederholung"/>).</summary>
public sealed record BfCsvWiederholung(int MaxWiederholungen, int AbstandMinuten);

/// <summary>
/// v1.99.35 (Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv F4): je freigegebenem BF-Auftrag mit CSV-Status offen (bzw. faelliger
/// Wiederholung) eine Datei <c>&lt;BF-Nummer&gt;.csv</c> im WiCAM-Pool, Muster <see cref="WareneingangCsvService"/>: <c>.tmp</c> schreiben, ohne
/// Ueberschreiben umbenennen; Namenskollision = Fehler ohne automatische Wiederholung. Windows-1252 ohne stilles Ersetzen. Das Gate
/// (Schalter, Pfad) prueft der <c>SageBookingWorker</c>; hier nur „Pfad leer = nichts“. Kein Sage-Zugriff.
/// </summary>
public class BfCsvService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<BfCsvService> _logger;
    private readonly ISyncLogger _syncLogger;

    public BfCsvService(ApplicationDbContext ctx, ILogger<BfCsvService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task RunOnceAsync(string? pfad, BfCsvKopf kopf, BfCsvWiederholung wiederholung, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pfad)) return;
        pfad = pfad.Trim();
        var jetzt0 = DateTime.Now;
        var abstand = jetzt0.AddMinutes(-Math.Max(1, wiederholung.AbstandMinuten));
        var max = Math.Max(0, wiederholung.MaxWiederholungen);
        var faellig = await _ctx.BfAuftraege.Include(a => a.Positionen)
            .Where(a => a.Status == BfAuftragStatus.Freigegeben && a.Nummer != null
                        && (a.CsvStatus == BfCsvStatus.Offen
                            || (a.CsvStatus == BfCsvStatus.Fehler && a.CsvVersuche >= 1 && a.CsvVersuche <= max
                                && (a.ModifiedAt ?? a.CreatedAt) < abstand && a.CsvFehler != BfCsv.KollisionText)))
            .OrderBy(a => a.Id).Take(WareneingangCsv.MaxDateienJeTakt).ToListAsync(ct);
        if (faellig.Count == 0) return;   // kein Protokoll-Eintrag ohne Arbeit (ADR 0010)

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BfAuftragCsv, ct);
        int geschrieben = 0, fehler = 0;
        try
        {
            foreach (var a in faellig)
            {
                if (ct.IsCancellationRequested) break;
                // ponytail: gelingt das Umbenennen, scheitert aber danach SaveChanges, bleibt der Auftrag offen; der naechste Takt meldet dann
                // „Datei existiert bereits“ (keine doppelte Datei). Abhilfe erst bei Bedarf.
                var (text, datei) = Schreiben(pfad, a, kopf);
                var now = DateTime.Now;
                if (text == null)
                {
                    a.CsvStatus = BfCsvStatus.Geschrieben;
                    a.CsvFehler = null;
                    a.CsvDatei = Kappen(datei, 400);
                    a.CsvGeschriebenAm = now;
                    geschrieben++;
                    await run.LogInfoAsync($"{a.Nummer}: {Path.GetFileName(datei)} geschrieben ({a.Positionen.Count(p => p.Uebergeben)} Position(en)).", reference: a.Nummer, ct: ct);
                }
                else
                {
                    a.CsvStatus = BfCsvStatus.Fehler;
                    a.CsvFehler = Kappen(text, 1000);
                    a.CsvVersuche++;
                    fehler++;
                    await run.LogWarningAsync(Kappen($"BF-CSV {a.Nummer}: {text}", 900)!, reference: a.Nummer, ct: ct);   // SyncLogs.Message nvarchar(1000)
                }
                a.ModifiedAt = now; a.ModifiedBy = BfCsv.ServiceActor; a.ModifiedByWindows = BfCsv.ServiceActor;
                await _ctx.SaveChangesAsync(ct);
            }
            await run.FinishSuccessAsync(new Dictionary<string, int> { ["geschrieben"] = geschrieben, ["fehler"] = fehler }, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "BF-CSV-Lauf fehlgeschlagen.");
            await run.LogErrorAsync(Kappen(ex.Message, 900)!, ct: ct);
            await run.FinishFailedAsync(Kappen(ex.Message, 900)!, counts: new Dictionary<string, int> { ["geschrieben"] = geschrieben, ["fehler"] = fehler }, ct: ct);
            throw;
        }
    }

    /// <summary>(null, Pfad) bei Erfolg, sonst (Fehlertext, null). Inhalt vor dem Schreiben kodiert: ein nicht kodierbares Zeichen
    /// (Altdaten nach der Freigabe) wird gemeldet und legt keine Datei an.</summary>
    private (string? Fehler, string? Datei) Schreiben(string pfad, BfAuftrag a, BfCsvKopf kopf)
    {
        var ziel = Path.Combine(pfad, a.Nummer + ".csv");
        var tmp = Path.Combine(pfad, a.Nummer + ".tmp");
        try
        {
            var bytes = BfCsv.Kodierung().GetBytes(BfCsv.Inhalt(a, kopf));
            if (File.Exists(ziel)) return (BfCsv.KollisionText, null);
            File.WriteAllBytes(tmp, bytes);
            File.Move(tmp, ziel, overwrite: false);
            return (null, ziel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BF-CSV {Nummer} nicht geschrieben ({Pfad}).", a.Nummer, pfad);
            try { if (File.Exists(tmp)) File.Delete(tmp); }
            catch (Exception tex) { _logger.LogWarning(tex, "BF-CSV: {Tmp} nicht geloescht.", tmp); }   // best effort
            return (ex is EncoderFallbackException ? $"Zeichen nicht in Windows-1252: {ex.Message}" : ex.Message, null);
        }
    }

    private static string? Kappen(string? v, int max) => string.IsNullOrEmpty(v) ? v : (v.Length <= max ? v : v[..max]);
}
