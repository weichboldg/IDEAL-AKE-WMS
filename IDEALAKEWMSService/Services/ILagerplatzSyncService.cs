namespace IDEALAKEWMSService.Services;

public record LagerplatzSyncResult(int Inserted, int Updated, int Conflicts, int Deactivated, int Skipped, int Errors);

public interface ILagerplatzSyncService
{
    Task<LagerplatzSyncResult> RunAsync(CancellationToken ct = default);
}
