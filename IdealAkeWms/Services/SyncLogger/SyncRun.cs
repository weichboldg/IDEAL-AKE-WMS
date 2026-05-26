using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services.SyncLogger;

public sealed class SyncRun : ISyncRun
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger _logger;
    private readonly string _serviceName;
    private FinishMode _mode = FinishMode.None;

    private enum FinishMode { None, Success, Failed }

    internal SyncRun(IDbContextFactory<ApplicationDbContext> factory, ILogger logger, string serviceName)
    {
        _factory = factory;
        _logger = logger;
        _serviceName = serviceName;
    }

    public Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Info, message, reference, ct);

    public Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Warning, message, reference, ct);

    public Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default)
        => WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Error, message, reference, ct);

    public async Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                                         string? messageSuffix = null,
                                         CancellationToken ct = default)
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Success;

        var msg = BuildMessage("Run erfolgreich beendet", counts, messageSuffix);
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Info, msg, reference: null, ct);
    }

    public async Task FinishFailedAsync(string errorMessage,
                                        IReadOnlyDictionary<string, int>? counts = null,
                                        CancellationToken ct = default)
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Failed;

        var prefix = $"Run fehlgeschlagen: {errorMessage}";
        var msg = BuildMessage(prefix, counts, messageSuffix: null);
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Error, msg, reference: null, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mode != FinishMode.None) return;
        _mode = FinishMode.Failed; // idempotent
        await WriteEntryAsync(_factory, _logger, _serviceName, SyncLogLevel.Warning,
            message: "Run wurde unerwartet beendet (kein FinishXxx-Aufruf)", reference: null,
            ct: CancellationToken.None);
    }

    // ----- Helpers -----

    internal static async Task WriteEntryAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        ILogger logger,
        string serviceName,
        string level,
        string message,
        string? reference,
        CancellationToken ct)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(ct);
            ctx.SyncLogs.Add(new SyncLog
            {
                Service = serviceName,
                Level = level,
                Message = message,
                Reference = reference,
                Timestamp = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SyncLog write failed for {Service} (level={Level}, msg={Message})",
                serviceName, level, message);
        }
    }

    private static string BuildMessage(string prefix, IReadOnlyDictionary<string, int>? counts, string? messageSuffix)
    {
        var parts = new List<string> { prefix };
        if (counts != null && counts.Count > 0)
            parts.Add(string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}")));
        if (!string.IsNullOrWhiteSpace(messageSuffix))
            parts.Add(messageSuffix);
        return string.Join(" — ", parts);
    }
}
