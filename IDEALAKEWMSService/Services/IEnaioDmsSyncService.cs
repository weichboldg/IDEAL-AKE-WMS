namespace IDEALAKEWMSService.Services;

public interface IEnaioDmsSyncService
{
    Task<SyncResult> SyncDocumentsAsync(bool dryRun, CancellationToken ct = default);
}
