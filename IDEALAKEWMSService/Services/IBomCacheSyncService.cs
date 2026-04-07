using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDEALAKEWMSService.Services;

public interface IBomCacheSyncService
{
    /// <summary>
    /// Full-window sync: reads the top-N open production orders in the
    /// configured weeks-ahead window and caches their BOMs.
    /// Also cleans up orphan cache entries.
    /// </summary>
    Task<SyncResult> SyncBomCacheAsync(bool dryRun, CancellationToken ct);

    /// <summary>
    /// Narrow sync for a specific set of article numbers (e.g. newly inserted
    /// production orders). No window query, no orphan cleanup.
    /// </summary>
    Task<SyncResult> SyncSpecificArticleNumbersAsync(
        List<string> articleNumbers,
        bool dryRun,
        CancellationToken ct);
}
