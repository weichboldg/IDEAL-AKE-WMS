namespace IDEALAKEWMSService.Services;

public interface IOseonSyncService
{
    Task<SyncResult> SyncOseonProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
    Task<SyncResult> SyncWorkplacesToProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
}
