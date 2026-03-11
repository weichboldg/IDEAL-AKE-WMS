namespace IDEALAKEWMSService.Services;

public record SyncResult(int Inserted, int Updated, int Errors, string? ErrorDetails = null);

public interface ISageImportService
{
    Task<SyncResult> SyncProductionOrdersAsync(bool dryRun, CancellationToken ct = default);
    Task<SyncResult> SyncArticlesAsync(bool dryRun, CancellationToken ct = default);
}
