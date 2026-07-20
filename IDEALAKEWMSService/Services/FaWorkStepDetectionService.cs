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
            var steps = await _db.WorkSteps
                .Where(w => w.IsActive && w.SearchString != null && w.SearchString != "")
                .ToListAsync(ct);

            int added = 0, skipped = 0;
            int termsTotal = 0, termsWithHit = 0;
            var termsWithoutHit = new List<string>(); // "begriff (Code)"

            foreach (var step in steps)
            {
                ct.ThrowIfCancellationRequested();

                var terms = step.SearchString!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .Distinct()
                    .ToList();
                if (terms.Count == 0) continue;

                // Pro Begriff getroffene Artikel — fuer Counts + welcher Begriff je Artikel ausloeste.
                var stepMatchedArticles = new HashSet<string>(StringComparer.Ordinal);
                var articleToTerms = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                foreach (var term in terms)
                {
                    termsTotal++;
                    var arts = await _db.CachedBomItems
                        .Where(i => (i.Bezeichnung1 != null && i.Bezeichnung1.ToLower().Contains(term))
                                 || (i.Bezeichnung2 != null && i.Bezeichnung2.ToLower().Contains(term)))
                        .Select(i => i.CachedBomHeader!.Artikelnummer)
                        .Distinct()
                        .ToListAsync(ct);

                    if (arts.Count == 0)
                    {
                        termsWithoutHit.Add($"{term} ({step.Code})");
                        continue;
                    }
                    termsWithHit++;
                    foreach (var a in arts)
                    {
                        stepMatchedArticles.Add(a);
                        if (!articleToTerms.TryGetValue(a, out var set))
                        {
                            set = new SortedSet<string>(StringComparer.Ordinal);
                            articleToTerms[a] = set;
                        }
                        set.Add(term);
                    }
                }
                if (stepMatchedArticles.Count == 0) continue;

                var matchedFaCount = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .CountAsync(ct);
                var candidates = await _db.ProductionOrders
                    .Where(o => !o.IsDone
                             && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)
                             && o.ArticleNumber != null && stepMatchedArticles.Contains(o.ArticleNumber!))
                    .Where(o => !_db.FaWorkSteps.Any(f => f.ProductionOrderId == o.Id && f.WorkStepId == step.Id))
                    .Select(o => new { o.Id, o.OrderNumber, o.ArticleNumber })
                    .ToListAsync(ct);

                skipped += matchedFaCount - candidates.Count; // Zeile existiert bereits (aktiv oder IsRemoved)
                foreach (var cand in candidates)
                {
                    if (!dryRun)
                    {
                        _db.FaWorkSteps.Add(new FaWorkStep
                        {
                            ProductionOrderId = cand.Id,
                            WorkStepId = step.Id,
                            Source = FaWorkStepSources.Sync,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "FaWorkStepDetection",
                            CreatedByWindows = "FaWorkStepDetection",
                        });
                    }
                    added++;

                    var triggers = (cand.ArticleNumber != null && articleToTerms.TryGetValue(cand.ArticleNumber, out var ts))
                        ? string.Join(", ", ts)
                        : "";
                    await run.LogInfoAsync(
                        $"FA {cand.OrderNumber} → AG {step.Code} {step.Name} erkannt (Begriff: {triggers})",
                        reference: cand.OrderNumber, ct: ct);
                }
            }

            if (!dryRun) await _db.SaveChangesAsync(ct);

            // Lauf-Zusammenfassung: optional [DryRun] + kompakte Nicht-Treffer-Liste (1 Zeile/Lauf, Cap 50).
            var suffixParts = new List<string>();
            if (dryRun) suffixParts.Add("[DryRun]");
            if (termsWithoutHit.Count > 0)
            {
                const int cap = 50;
                var shown = termsWithoutHit.Take(cap).ToList();
                var extra = termsWithoutHit.Count - shown.Count;
                suffixParts.Add($"Ohne Treffer: {string.Join(", ", shown)}{(extra > 0 ? $" … (+{extra} weitere)" : "")}");
            }
            var messageSuffix = suffixParts.Count > 0 ? string.Join(" — ", suffixParts) : null;

            _logger.LogInformation(
                "FA-Arbeitsgang-Erkennung abgeschlossen: {Added} neu, {Skipped} uebersprungen, " +
                "{TermsTotal} Begriffe ({WithHit} mit Treffer, {NoHit} ohne){DryRun}",
                added, skipped, termsTotal, termsWithHit, termsTotal - termsWithHit, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = added,
                ["uebersprungen"] = skipped,
                ["suchbegriffe gesamt"] = termsTotal,
                ["mit treffer"] = termsWithHit,
                ["ohne treffer"] = termsTotal - termsWithHit,
            }, messageSuffix: messageSuffix, ct: ct);

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
