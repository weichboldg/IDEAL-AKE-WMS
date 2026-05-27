using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

public class LagerbestandSyncService : ILagerbestandSyncService
{
    private const string SyncUser = "system:sync";

    private readonly ApplicationDbContext _ctx;
    private readonly ISageBestandReader _reader;
    private readonly IStockMovementRepository _stockRepo;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<LagerbestandSyncService> _logger;

    public LagerbestandSyncService(
        ApplicationDbContext ctx,
        ISageBestandReader reader,
        IStockMovementRepository stockRepo,
        ILogger<LagerbestandSyncService> logger,
        ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _reader = reader;
        _stockRepo = stockRepo;
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

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["einbuchungen"] = plus,
                ["ausbuchungen"] = minus,
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
