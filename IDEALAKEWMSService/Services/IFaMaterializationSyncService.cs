namespace IDEALAKEWMSService.Services;

public sealed record FaMaterializationResult(
    int Created, int Marked, int Cleared, int ReparentConflicts, bool Skipped, bool DryRun);

public interface IFaMaterializationSyncService
{
    Task<FaMaterializationResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
