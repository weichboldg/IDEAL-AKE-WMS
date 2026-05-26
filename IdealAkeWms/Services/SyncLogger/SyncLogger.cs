using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services.SyncLogger;

public sealed class SyncLogger : ISyncLogger
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<SyncLogger> _logger;

    public SyncLogger(IDbContextFactory<ApplicationDbContext> factory, ILogger<SyncLogger> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default)
    {
        await SyncRun.WriteEntryAsync(_factory, _logger, serviceName, Models.SyncLogLevel.Info,
            message: "Run gestartet", reference: null, ct: ct);
        return new SyncRun(_factory, _logger, serviceName);
    }
}
