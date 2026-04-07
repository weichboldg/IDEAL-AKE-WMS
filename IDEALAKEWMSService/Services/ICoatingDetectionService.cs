using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDEALAKEWMSService.Services;

public interface ICoatingDetectionService
{
    /// <summary>
    /// Detects which orders have coating parts and updates HasCoatingParts.
    /// If specificOrderIds is null, all open orders in the configured window are evaluated.
    /// </summary>
    Task<SyncResult> DetectAndUpdateCoatingFlagsAsync(
        bool dryRun,
        List<int>? specificOrderIds,
        CancellationToken ct);
}
