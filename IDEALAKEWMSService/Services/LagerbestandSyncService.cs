using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerbestandSyncService : ILagerbestandSyncService
{
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageBestandReader _reader;
    private readonly IStockMovementRepository _stockRepo;
    private readonly IConfiguration _config;
    private readonly ISyncErrorNotifier _errorNotifier;
    private readonly ILogger<LagerbestandSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public LagerbestandSyncService(
        ApplicationDbContext ctx,
        ISageBestandReader reader,
        IStockMovementRepository stockRepo,
        IConfiguration config,
        ISyncErrorNotifier errorNotifier,
        ILogger<LagerbestandSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _stockRepo = stockRepo;
        _config = config;
        _errorNotifier = errorNotifier;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<LagerbestandSyncResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Lagerbestand, ct);
        int tuples = 0, plus = 0, minus = 0, noChange = 0, skipped = 0, errors = 0;

        try
        {
            List<SageBestandDto> sageRows;
            try
            {
                sageRows = await _reader.GetAllAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sage-Connection fehlgeschlagen.");
                errors++;
                await run.LogErrorAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", ct: ct);
                await run.FinishFailedAsync($"Sage-Connection fehlgeschlagen: {ex.Message}", counts: new Dictionary<string, int>
                {
                    ["einbuchungen"] = plus,
                    ["ausbuchungen"] = minus,
                    ["uebersprungen"] = skipped,
                    ["fehler"] = errors,
                }, ct: ct);
                return new LagerbestandSyncResult(0, 0, 0, 0, 0, 1, dryRun);
            }

            // Roh-Zeilen (VOR Dedup) fuer den Nullsetz-Abgleich festhalten: sagePresentKeys
            // muss auch Duplikat-Keys enthalten (die sind in Sage vorhanden, nur mehrdeutig).
            var rawSageRows = sageRows;
            var sageRowCountRaw = rawSageRows.Count;

            // Sage-Duplikate erkennen: gleiche (Artikelnummer, Lagerplatz) aus mehreren Lagerorten
            var dupGroups = sageRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Artikelnummer) && !string.IsNullOrWhiteSpace(r.Lagerplatz))
                .GroupBy(r => (r.Artikelnummer!.Trim(), r.Lagerplatz!.Trim()),
                         new TupleKeyComparer())
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in dupGroups)
            {
                await run.LogWarningAsync(
                    $"Sage liefert (Artikel '{group.Key.Item1}', Lagerplatz '{group.Key.Item2}') mehrfach. Tupel uebersprungen.",
                    reference: group.Key.Item2, ct: ct);
            }

            var dupKeys = dupGroups.Select(g => g.Key).ToHashSet(new TupleKeyComparer());
            sageRows = sageRows
                .Where(r => string.IsNullOrWhiteSpace(r.Artikelnummer) || string.IsNullOrWhiteSpace(r.Lagerplatz)
                         || !dupKeys.Contains((r.Artikelnummer!.Trim(), r.Lagerplatz!.Trim())))
                .ToList();

            // Pre-loading
            var articleByNumber = await _ctx.Articles
                .ToDictionaryAsync(a => a.ArticleNumber, a => a.Id, StringComparer.OrdinalIgnoreCase, ct);
            var locationByCode = await _ctx.StorageLocations
                .ToDictionaryAsync(
                    l => l.Code,
                    l => (l.Id, l.Source, l.IsActive),
                    StringComparer.OrdinalIgnoreCase, ct);
            var wmsStock = await _stockRepo.GetCurrentStockByArticleAndLocationAsync();

            // "In-Sage-vorhanden"-Set aus den ROH-Zeilen (inkl. Duplikat-Keys + inaktive/manuelle
            // Plaetze — schaden nicht, sind ohnehin keine Nullsetz-Kandidaten).
            var sagePresentKeys = new HashSet<(int ArticleId, int StorageLocationId)>();
            foreach (var raw in rawSageRows)
            {
                if (string.IsNullOrWhiteSpace(raw.Artikelnummer) || string.IsNullOrWhiteSpace(raw.Lagerplatz))
                    continue;
                if (!articleByNumber.TryGetValue(raw.Artikelnummer, out var presentArticleId))
                    continue;
                if (!locationByCode.TryGetValue(raw.Lagerplatz, out var presentLoc))
                    continue;
                sagePresentKeys.Add((presentArticleId, presentLoc.Id));
            }

            foreach (var dto in sageRows)
            {
                tuples++;

                if (string.IsNullOrWhiteSpace(dto.Artikelnummer) || string.IsNullOrWhiteSpace(dto.Lagerplatz))
                {
                    skipped++;
                    continue;
                }

                if (!articleByNumber.TryGetValue(dto.Artikelnummer, out var articleId))
                {
                    await run.LogWarningAsync(
                        $"Artikel {dto.Artikelnummer} nicht im WMS, uebersprungen.",
                        reference: dto.Artikelnummer, ct: ct);
                    skipped++;
                    continue;
                }

                if (!locationByCode.TryGetValue(dto.Lagerplatz, out var loc))
                {
                    await run.LogWarningAsync(
                        $"Lagerplatz {dto.Lagerplatz} nicht im WMS, uebersprungen.",
                        reference: dto.Lagerplatz, ct: ct);
                    skipped++;
                    continue;
                }

                if (loc.Source != StorageLocationSource.Sage)
                {
                    await run.LogWarningAsync(
                        $"Lagerplatz {dto.Lagerplatz} ist Manual-Quelle, uebersprungen.",
                        reference: dto.Lagerplatz, ct: ct);
                    skipped++;
                    continue;
                }

                if (!loc.IsActive)
                {
                    await run.LogWarningAsync(
                        $"Lagerplatz {dto.Lagerplatz} ist deaktiviert, uebersprungen.",
                        reference: dto.Lagerplatz, ct: ct);
                    skipped++;
                    continue;
                }

                var wmsBestand = wmsStock.GetValueOrDefault((articleId, loc.Id), 0m);
                var sageBestand = dto.Bestand ?? 0m;
                var delta = sageBestand - wmsBestand;

                if (delta == 0m) { noChange++; continue; }

                if (!dryRun)
                {
                    _ctx.StockMovements.Add(new StockMovement
                    {
                        ArticleId = articleId,
                        StorageLocationId = loc.Id,
                        Quantity = Math.Abs(delta),
                        MovementType = delta > 0 ? MovementType.SageEinbuchung : MovementType.SageAusbuchung,
                        Note = $"Sage-Korrektur: WMS={wmsBestand}, Sage={sageBestand}, Diff={(delta > 0 ? "+" : "")}{delta}",
                        Timestamp = DateTime.Now,
                        UserId = null,
                        WindowsUser = SyncUser,
                        CreatedAt = DateTime.Now,
                        CreatedBy = SyncUser,
                        CreatedByWindows = Environment.MachineName
                    });
                }

                if (delta > 0) plus++; else minus++;
            }

            // ---- Nullsetzen verwaister Bestaende (in Sage verschwundene Paare) ----------
            // managedStock = wmsStock gefiltert auf Sage-Quelle + aktiv + Menge != 0.
            // (GetCurrentStockByArticleAndLocationAsync enthaelt auch Netto-0-Paare UND
            //  Umbuchungs-Quellseiten-Keys -> beides MUSS raus.)
            var locationInfoById = locationByCode.Values
                .ToDictionary(v => v.Id, v => (v.Source, v.IsActive));
            var managedStock = wmsStock
                .Where(kv => kv.Value != 0m
                          && locationInfoById.TryGetValue(kv.Key.StorageLocationId, out var li)
                          && li.Source == StorageLocationSource.Sage
                          && li.IsActive)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var maxPerRun = await ServiceSettings.GetIntSafeAsync(
                _config, "Sync:LagerbestandNullsetzenMaxPerRun", 100, ct);

            var zeroPlan = LagerbestandZeroingPlanner.Plan(
                sageRowCountRaw, sagePresentKeys, managedStock, maxPerRun);

            int nullgesetzt = 0;
            if (zeroPlan.Skipped)
            {
                var reason = zeroPlan.SkipReason ?? "unbekannt";
                await run.LogWarningAsync($"Nullsetzen uebersprungen: {reason}", ct: ct);
                _logger.LogWarning("Lagerbestand-Nullsetzen uebersprungen: {Reason}", reason);

                // Cap-Skip zusaetzlich per Fehlermail (Sage-Teil-Read-Verdacht). Guard (leer) NICHT.
                if (reason.StartsWith("Cap", StringComparison.OrdinalIgnoreCase))
                {
                    await _errorNotifier.NotifyAsync(
                        "Lagerbestand-Nullsetzen: Cap ueberschritten",
                        new InvalidOperationException(
                            $"Lagerbestand-Nullsetzen Cap ueberschritten: {reason}. " +
                            $"Sage lieferte {sageRowCountRaw} Zeilen, Cap={maxPerRun}. " +
                            $"Kein Nullsetzen geschrieben — moeglicher Sage-Teil-Read."),
                        ct);
                }
            }
            else
            {
                // Reverse-Lookups fuer lesbare Detailzeilen (Artikel-Nummer / Lagerplatz-Code).
                var articleNumberById = articleByNumber.ToDictionary(kv => kv.Value, kv => kv.Key);
                var codeById = locationByCode.ToDictionary(kv => kv.Value.Id, kv => kv.Key);
                int infoLines = 0;

                foreach (var (articleId, locId, wmsBestand) in zeroPlan.ToZero)
                {
                    if (!dryRun)
                    {
                        _ctx.StockMovements.Add(new StockMovement
                        {
                            ArticleId = articleId,
                            StorageLocationId = locId,
                            Quantity = Math.Abs(wmsBestand),
                            MovementType = wmsBestand > 0 ? MovementType.SageAusbuchung : MovementType.SageEinbuchung,
                            Note = $"Sage-Korrektur: in Sage nicht mehr vorhanden -> auf 0 gesetzt (WMS war {wmsBestand})",
                            Timestamp = DateTime.Now,
                            UserId = null,
                            WindowsUser = SyncUser,
                            CreatedAt = DateTime.Now,
                            CreatedBy = SyncUser,
                            CreatedByWindows = Environment.MachineName
                        });
                    }

                    nullgesetzt++;
                    if (infoLines < 100)
                    {
                        var articleNumber = articleNumberById.GetValueOrDefault(articleId, articleId.ToString());
                        var code = codeById.GetValueOrDefault(locId, locId.ToString());
                        await run.LogInfoAsync(
                            $"Bestand auf 0 gesetzt: {articleNumber} @ {code} (WMS war {wmsBestand})",
                            reference: code, ct: ct);
                        infoLines++;
                    }
                }

                if (nullgesetzt > 0)
                    _logger.LogInformation("Lagerbestand-Nullsetzen: {Count} Paar(e) auf 0 gesetzt.", nullgesetzt);
            }
            // ---- Ende Nullsetzen ---------------------------------------------------------

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["einbuchungen"] = plus,
                ["ausbuchungen"] = minus,
                ["nullgesetzt"] = nullgesetzt,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new LagerbestandSyncResult(tuples, plus, minus, noChange, skipped, errors, dryRun);
        }
        catch (Exception ex)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["einbuchungen"] = plus,
                ["ausbuchungen"] = minus,
                ["uebersprungen"] = skipped,
                ["fehler"] = errors,
            }, ct: ct);
            throw;
        }
    }

    private sealed class TupleKeyComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y) =>
            string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string) obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) ^
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2);
    }
}
