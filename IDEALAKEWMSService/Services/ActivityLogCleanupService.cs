using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>Reine Entscheidungslogik: berechnet den Stichtag oder "deaktiviert".</summary>
public static class ActivityLogCleanupPlanner
{
    /// <returns>Stichtag (loesche alles aelter); null = deaktiviert (retentionDays &lt;= 0).</returns>
    public static DateTime? ComputeCutoff(DateTime now, int retentionDays)
        => retentionDays <= 0 ? null : now.AddDays(-retentionDays);
}

public record ActivityLogCleanupResult(int Deleted, bool Skipped, string? SkipReason);

public interface IActivityLogCleanupService
{
    Task<ActivityLogCleanupResult> RunAsync(int retentionDays, bool dryRun, CancellationToken ct);
}

public sealed class ActivityLogCleanupService : IActivityLogCleanupService
{
    private const int BatchSize = 5000;

    private readonly ISyncLogRepository _repo;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<ActivityLogCleanupService> _logger;

    public ActivityLogCleanupService(ISyncLogRepository repo, ISyncLogger syncLogger,
        ILogger<ActivityLogCleanupService> logger)
    {
        _repo = repo;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<ActivityLogCleanupResult> RunAsync(int retentionDays, bool dryRun, CancellationToken ct)
    {
        // Stichtag aus DateTime.Now (Lokalzeit) — SyncLog.Timestamp wird als Lokalzeit gespeichert.
        var cutoff = ActivityLogCleanupPlanner.ComputeCutoff(DateTime.Now, retentionDays);
        if (cutoff is null)
        {
            _logger.LogDebug("Aktivitaetsprotokoll-Bereinigung deaktiviert (Aufbewahrung {Days} Tage <= 0).", retentionDays);
            return new ActivityLogCleanupResult(0, Skipped: true, SkipReason: "deaktiviert (Aufbewahrung <= 0)");
        }

        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.CleanupActivityLog, ct);
        try
        {
            var deleted = await _repo.DeleteOlderThanAsync(cutoff.Value, BatchSize, dryRun, ct);
            var suffix = $"Aufbewahrung {retentionDays} Tage, Stichtag {cutoff.Value:dd.MM.yyyy}"
                       + (dryRun ? " (DryRun — nichts geloescht)" : "");
            await run.FinishSuccessAsync(
                counts: new Dictionary<string, int> { ["geloescht"] = deleted },
                messageSuffix: suffix, ct: ct);
            return new ActivityLogCleanupResult(deleted, Skipped: false, SkipReason: null);
        }
        catch (Exception ex)
        {
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw; // Worker faengt es -> Serilog-Error + Fehlermail
        }
    }
}
