using System;
using System.Threading;
using System.Threading.Tasks;

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
