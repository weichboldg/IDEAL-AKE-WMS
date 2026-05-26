using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Tests.Helpers;

/// <summary>
/// In-Memory <see cref="ISyncLogger"/> fuer Service-Tests. Sammelt alle Aufrufe
/// in einer Liste, sodass Tests <c>fake.Runs[0].Calls.Should()...</c> pruefen koennen.
/// </summary>
public sealed class FakeSyncLogger : ISyncLogger
{
    public List<FakeSyncRun> Runs { get; } = new();

    public Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default)
    {
        var run = new FakeSyncRun(serviceName);
        Runs.Add(run);
        return Task.FromResult<ISyncRun>(run);
    }
}

public sealed class FakeSyncRun : ISyncRun
{
    public string ServiceName { get; }
    public List<(string Level, string Message, string? Reference)> Events { get; } = new();
    public bool FinishedSuccess { get; private set; }
    public bool FinishedFailed { get; private set; }
    public IReadOnlyDictionary<string, int>? FinalCounts { get; private set; }
    public string? FinalErrorMessage { get; private set; }
    public bool Disposed { get; private set; }

    public FakeSyncRun(string serviceName)
    {
        ServiceName = serviceName;
    }

    public Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Info", message, reference));
        return Task.CompletedTask;
    }

    public Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Warning", message, reference));
        return Task.CompletedTask;
    }

    public Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default)
    {
        Events.Add(("Error", message, reference));
        return Task.CompletedTask;
    }

    public Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                                   string? messageSuffix = null,
                                   CancellationToken ct = default)
    {
        if (FinishedSuccess || FinishedFailed) return Task.CompletedTask;
        FinishedSuccess = true;
        FinalCounts = counts;
        return Task.CompletedTask;
    }

    public Task FinishFailedAsync(string errorMessage,
                                  IReadOnlyDictionary<string, int>? counts = null,
                                  CancellationToken ct = default)
    {
        if (FinishedSuccess || FinishedFailed) return Task.CompletedTask;
        FinishedFailed = true;
        FinalErrorMessage = errorMessage;
        FinalCounts = counts;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
