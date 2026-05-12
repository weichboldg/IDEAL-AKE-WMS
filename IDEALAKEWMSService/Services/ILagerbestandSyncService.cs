namespace IDEALAKEWMSService.Services;

public record LagerbestandSyncResult(
    int Tuples, int CorrectionsPlus, int CorrectionsMinus,
    int NoChange, int Skipped, int Errors,
    bool DryRun);

public interface ILagerbestandSyncService
{
    Task<LagerbestandSyncResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
