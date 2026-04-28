namespace IDEALAKEWMSService.Services;

public interface IBdeAutoPauseService
{
    Task<AutoPauseResult> RunAsync(CancellationToken ct);
}

public record AutoPauseResult(int CheckedCount, int PausedCount, List<string> Errors);
