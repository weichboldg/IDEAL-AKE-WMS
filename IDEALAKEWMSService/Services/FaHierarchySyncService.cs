using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using IDEALAKEWMSService.Services.Sdata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Full-Refresh-Import der IDEAL-FA-Hierarchie aus den Sage-Views in die lokalen Cache-Tabellen.
/// Primaerweg (RCSI AN, Standard-Scope Teil 1): DELETE FROM beider Zieltabellen + Neubefuellung in
/// EINER Transaktion — kein Staging, kein sp_rename, kein GRANT ALTER.
///
/// Absicherung: View-Namen aus ServiceSettings, Whitelist + QUOTENAME (FaHierarchySql). Empty-Guard
/// auf den ungefilterten Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation. Fehlermail
/// (ISyncErrorNotifier) explizit bei ungueltigem View-Namen UND bei leerem View-Read, da
/// RunResilientAsync nur bei geworfener Exception mailt (ADR 0010, S-4).
///
/// Gelesen wird ueber <see cref="ISageFaStrukturReader"/> (v1.73.0 E4a): der bisherige SQL-Lese-Code steckt unveraendert in
/// <see cref="SageFaStrukturReader"/>, davor die Quellen-Weiche <c>Sync:Quelle:FaStruktur</c> (Default View).
/// </summary>
public class FaHierarchySyncService : IFaHierarchySyncService
{
    private const string SyncStep = "FaHierarchy-Sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageFaStrukturReader _reader;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<FaHierarchySyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaHierarchySyncService(
        ApplicationDbContext ctx,
        ISageFaStrukturReader reader,
        ISyncErrorNotifier errorNotifier,
        ILogger<FaHierarchySyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _reader = reader;
        _errorNotifier = errorNotifier;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<FaHierarchySyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaHierarchy, ct);

        try
        {
            // 1)–3) View-Namen, Existenzpruefung, Roh-Zeilenzahlen, Knoten + OrderInfos (SageFaStrukturReader bzw. Weiche).
            var lesung = await _reader.LesenAsync(ct);
            await LogQuelleAsync(run, ct);

            // 2) Empty-Guard: ungefilterte Roh-Zeilenzahlen BEIDER Views VOR jeder Mutation.
            if (lesung.ListeAnzahl == 0 || lesung.InfosAnzahl == 0)
            {
                var msg = $"Leerer View-Read (FAListe={lesung.ListeAnzahl}, FAInfos={lesung.InfosAnzahl}) — " +
                          "kein Replace, bestehende Zieltabellen bleiben unangetastet.";
                _logger.LogWarning("FaHierarchy-Sync: {Msg}", msg);
                await run.LogWarningAsync(msg, ct: ct);
                await _errorNotifier.NotifyAsync(
                    "FaHierarchy-Sync: leerer View-Read",
                    new InvalidOperationException($"FaHierarchy-Sync uebersprungen: {msg}"),
                    ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = 0,
                    ["geloescht"] = 0,
                }, messageSuffix: "[uebersprungen: leerer View-Read]", ct: ct);
                return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
            }

            var nodes = lesung.Knoten;
            var infos = lesung.Infos;

            if (dryRun)
            {
                _logger.LogInformation(
                    "FaHierarchy-Sync [DryRun]: wuerde {Nodes} Knoten + {Infos} OrderInfos ersetzen.",
                    nodes.Count, infos.Count);
                if (lesung.Positionen is { } p)
                    await run.LogInfoAsync($"[DryRun] ProductionOrderPositions: würde {p.Count} Positionen aus SData ersetzen.", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["neu"] = nodes.Count + infos.Count,
                    ["geloescht"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new FaHierarchySyncResult(nodes.Count, infos.Count, 0, 0, Skipped: false, DryRun: true);
            }

            // 4) Full-Refresh: beide Zieltabellen in EINER Transaktion ersetzen (RCSI-Primaerweg).
            //    Nie steht FaHierarchyNode (neu) neben altem FaHierarchyOrderInfo oder umgekehrt.
            await using var tx = await _ctx.Database.BeginTransactionAsync(ct);

            var deletedNodes = await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.FaHierarchyNodes", ct);
            var deletedInfos = await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.FaHierarchyOrderInfos", ct);

            await _ctx.FaHierarchyNodes.AddRangeAsync(nodes, ct);
            await _ctx.FaHierarchyOrderInfos.AddRangeAsync(infos, ct);
            await _ctx.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "FaHierarchy-Sync: {Nodes} Knoten + {Infos} OrderInfos neu ({DelN}/{DelI} geloescht).",
                nodes.Count, infos.Count, deletedNodes, deletedInfos);

            var ambiguousCount = await LogAmbiguousHauptFasAsync(run, infos, ct);

            var counts = new Dictionary<string, int>
            {
                ["neu"] = nodes.Count + infos.Count,
                ["geloescht"] = deletedNodes + deletedInfos,
                ["hauptfa_mehrdeutig"] = ambiguousCount,
            };
            // E4b: nur wenn SData gelesen wurde (Vergleich/SData) — im View-Betrieb Count-Keys und Tabelle unveraendert.
            if (lesung.Positionen is { } positionen)
                counts["positionen"] = await PositionenErsetzenAsync(run, positionen, ct);
            await run.FinishSuccessAsync(counts, ct: ct);

            return new FaHierarchySyncResult(nodes.Count, infos.Count, deletedNodes, deletedInfos, Skipped: false, DryRun: false);
        }
        catch (FaHierarchyViewNameException ex)
        {
            // KEIN SQL wird ausgefuehrt (Reader validiert vor der Verbindung). FinishFailedAsync + explizite Fehlermail (S-4).
            _logger.LogError(ex, "FaHierarchy-Sync: ungueltiger View-Name.");
            await run.LogErrorAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Ungueltiger View-Name: {ex.Message}", ct: ct);
            await _errorNotifier.NotifyAsync(
                "FaHierarchy-Sync: ungueltiger View-Name",
                new InvalidOperationException(
                    $"FaHierarchy-Sync abgebrochen — ungueltiger View-Name (kein SQL ausgefuehrt): {ex.Message}"),
                ct);
            return new FaHierarchySyncResult(0, 0, 0, 0, Skipped: true, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Unerwarteter Fehler: FinishFailedAsync + rethrow. Die Fehlermail schickt der
            // umgebende RunResilientAsync-Block im SyncWorker (mailt bei geworfener Exception).
            _logger.LogError(ex, "FaHierarchy-Sync fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>
    /// E4b (Spec 2026-11-06-sage-sdata-stammdaten-sync F6): Full-Refresh von <c>ProductionOrderPositions</c> in einer EIGENEN Transaktion
    /// NACH dem Struktur-Commit — ein Fehler hier rollt die (im Vergleich aus der View geschriebene) Struktur nicht zurueck. Leere Liste:
    /// nichts ersetzen + Warnung (nie leeren). Fehler: Protokoll-Fehler + Mail, der Lauf endet trotzdem erfolgreich (Struktur ist geschrieben).
    /// Gibt die Zahl der geschriebenen Positionen zurueck (0 bei Warnung/Fehler).
    /// </summary>
    internal async Task<int> PositionenErsetzenAsync(ISyncRun run, List<ProductionOrderPosition> positionen, CancellationToken ct)
    {
        if (positionen.Count == 0)
        {
            await run.LogWarningAsync("ProductionOrderPositions: SData liefert keine Positionen – Tabelle bleibt unverändert.", ct: ct);
            return 0;
        }
        try
        {
            await using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            var geloescht = await _ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.ProductionOrderPositions", ct);
            await _ctx.ProductionOrderPositions.AddRangeAsync(positionen, ct);
            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await run.LogInfoAsync($"ProductionOrderPositions: {positionen.Count} Positionen aus SData ersetzt ({geloescht} gelöscht).", ct: ct);
            return positionen.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ctx.ChangeTracker.Clear();
            _logger.LogError(ex, "FaHierarchy-Sync: ProductionOrderPositions nicht ersetzt.");
            var msg = $"ProductionOrderPositions nicht ersetzt (alter Stand bleibt): {ex.Message}";
            await run.LogErrorAsync(msg.Length <= 600 ? msg : msg[..600], ct: ct); // SyncLogs.Message = 1000
            await _errorNotifier.NotifyAsync("FaHierarchy-Sync: FA-Positionen nicht ersetzt", ex, ct);
            return 0;
        }
    }

    /// <summary>Info-Zeile „Quelle: View|SData|Vergleich“ (F1) + Hinweise des SData-Readers, nur wenn der Reader die Quellen-Weiche ist.</summary>
    private async Task LogQuelleAsync(ISyncRun run, CancellationToken ct)
    {
        if (_reader is not SageQuelleWeiche { GenutzteQuelle: { } quelle })
            return;
        await run.LogInfoAsync($"Quelle: {quelle}", ct: ct);
        if (_reader is FaStrukturQuelleSwitch w)
            foreach (var h in w.SdataHinweise)
                if (h.Warnung) await run.LogWarningAsync(h.Text, ct: ct);
                else await run.LogInfoAsync(h.Text, ct: ct);
    }

    /// <summary>
    /// Kombigeraete: derselbe HauptFA mit mehreren Montage-Abteilungen. Eine Eigenschaft der
    /// DATEN, nicht der Anzeige — deshalb einmal je Lauf gemeldet statt je Seitenaufruf
    /// (Rueckfrage 7). Die Kennzeichnung in der Oberflaeche bleibt davon unberuehrt.
    /// Internal statt private: <see cref="RunAsync"/> braucht eine echte SqlConnection nach
    /// Sage und ist deshalb ohne Live-DB nicht InMemory-testbar — die reine Meldelogik wird
    /// hier separat gehalten, damit sie ueber InternalsVisibleTo direkt getestet werden kann.
    /// </summary>
    internal static async Task<int> LogAmbiguousHauptFasAsync(
        ISyncRun run, IReadOnlyCollection<FaHierarchyOrderInfo> infos, CancellationToken ct)
    {
        var ambiguousHauptFas = infos.GroupBy(i => i.HauptFA)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(k => k)
            .ToList();
        if (ambiguousHauptFas.Count > 0)
            await run.LogWarningAsync(
                $"Kombigeraete: {ambiguousHauptFas.Count} HauptFA mit mehreren Auftragskoepfen " +
                $"({string.Join(", ", ambiguousHauptFas)}). Kopfdaten werden in der FA-Liste als " +
                "mehrdeutig gekennzeichnet, es wird keine Variante ausgewaehlt.", ct: ct);
        return ambiguousHauptFas.Count;
    }
}
