using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Automatische FA-zu-Arbeitsgang-Erkennung aus dem BOM-Cache.
/// BEWUSST ein eigener idempotenter Schritt nach dem BomCache-Sync — NICHT am
/// ContentHash-Insert-Pfad des <see cref="BomCacheSyncService"/> (Spec §5).
/// Nur-hinzufuegen-Semantik: bestehende FaWorkStep-Zeilen — auch manuell
/// entfernte (<c>IsRemoved=true</c>) — sperren das Re-Add und bleiben unveraendert.
/// </summary>
public class FaWorkStepDetectionService : IFaWorkStepDetectionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FaWorkStepDetectionService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaWorkStepDetectionService(
        ApplicationDbContext db,
        ILogger<FaWorkStepDetectionService> logger,
        ISyncLogger syncLogger)
    {
        _db = db;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaWorkStepDetection, ct);
        try
        {
            // 1) Aktive WorkSteps mit Suchbegriffen
            var steps = await _db.WorkSteps
                .Where(w => w.IsActive && w.SearchString != null && w.SearchString != "")
                .ToListAsync(ct);

            int added = 0, skipped = 0;
            foreach (var step in steps)
            {
                ct.ThrowIfCancellationRequested();

                var terms = step.SearchString!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (terms.Count == 0) continue;

                // 2) Artikel, deren BOM-Items einen Begriff enthalten (pro Begriff eine
                //    Query — ToLower().Contains statt EF.Functions.Like wegen InMemory-Tests).
                var matchedArticles = new HashSet<string>(StringComparer.Ordinal);
                foreach (var term in terms)
                {
                    var arts = await _db.CachedBomItems
                        .Where(i => (i.Bezeichnung1 != null && i.Bezeichnung1.ToLower().Contains(term))
                                 || (i.Bezeichnung2 != null && i.Bezeichnung2.ToLower().Contains(term)))
                        .Select(i => i.CachedBomHeader!.Artikelnummer)
                        .Distinct()
                        .ToListAsync(ct);
                    foreach (var a in arts) matchedArticles.Add(a);
                }
                if (matchedArticles.Count == 0) continue;

                // 3) Offene FAs zu diesen Artikeln ohne vorhandene Zeile (auch keine IsRemoved!)
                var matchedFaCount = await _db.ProductionOrders
                    .Where(o => !o.IsDone && o.ArticleNumber != null && matchedArticles.Contains(o.ArticleNumber!))
                    .CountAsync(ct);
                var candidates = await _db.ProductionOrders
                    .Where(o => !o.IsDone && o.ArticleNumber != null && matchedArticles.Contains(o.ArticleNumber!))
                    .Where(o => !_db.FaWorkSteps.Any(f => f.ProductionOrderId == o.Id && f.WorkStepId == step.Id))
                    .Select(o => o.Id)
                    .ToListAsync(ct);

                skipped += matchedFaCount - candidates.Count; // Zeile existiert bereits (aktiv oder IsRemoved)
                foreach (var poId in candidates)
                {
                    if (!dryRun)
                    {
                        _db.FaWorkSteps.Add(new FaWorkStep
                        {
                            ProductionOrderId = poId,
                            WorkStepId = step.Id,
                            Source = FaWorkStepSources.Sync,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "FaWorkStepDetection",
                            CreatedByWindows = "FaWorkStepDetection",
                        });
                    }
                    added++;
                }
            }

            if (!dryRun) await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "FA-Arbeitsgang-Erkennung abgeschlossen: {Added} neu, {Skipped} uebersprungen{DryRun}",
                added, skipped, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = added,
                ["uebersprungen"] = skipped,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(added, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei FA-Arbeitsgang-Erkennung.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }
}
