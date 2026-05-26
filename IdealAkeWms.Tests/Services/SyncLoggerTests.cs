using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IdealAkeWms.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services;

public class SyncLoggerTests
{
    [Fact]
    public async Task BeginRunAsync_writes_start_entry()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using var run = await logger.BeginRunAsync(SyncLogServices.BomCache);

        using var verify = factory.CreateDbContext();
        var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
        entries.Should().HaveCount(1);
        entries[0].Service.Should().Be(SyncLogServices.BomCache);
        entries[0].Level.Should().Be(SyncLogLevel.Info);
        entries[0].Message.Should().Be("Run gestartet");
        entries[0].Reference.Should().BeNull();
    }

    [Fact]
    public async Task LogWarningAsync_writes_warning_with_reference()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using var run = await logger.BeginRunAsync(SyncLogServices.Lagerplatz);
        await run.LogWarningAsync("Duplikat", reference: "A-001");
        await run.FinishSuccessAsync();

        using var verify = factory.CreateDbContext();
        var warning = await verify.SyncLogs.FirstAsync(e => e.Level == SyncLogLevel.Warning);
        warning.Message.Should().Be("Duplikat");
        warning.Reference.Should().Be("A-001");
        warning.Service.Should().Be(SyncLogServices.Lagerplatz);
    }

    [Fact]
    public async Task FinishSuccessAsync_renders_counts_into_message()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using (var run = await logger.BeginRunAsync(SyncLogServices.BomCache))
        {
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = 12, ["aktualisiert"] = 5, ["uebersprungen"] = 0,
            });
        }

        using var verify = factory.CreateDbContext();
        var end = await verify.SyncLogs.OrderByDescending(e => e.Id).FirstAsync();
        end.Level.Should().Be(SyncLogLevel.Info);
        end.Message.Should().Be("Run erfolgreich beendet — neu=12, aktualisiert=5, uebersprungen=0");
    }

    [Fact]
    public async Task FinishFailedAsync_writes_error_level_with_message()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using (var run = await logger.BeginRunAsync(SyncLogServices.Holiday))
        {
            await run.FinishFailedAsync("HTTP 500 von date.nager.at",
                counts: new Dictionary<string, int> { ["importiert"] = 2 });
        }

        using var verify = factory.CreateDbContext();
        var end = await verify.SyncLogs.OrderByDescending(e => e.Id).FirstAsync();
        end.Level.Should().Be(SyncLogLevel.Error);
        end.Message.Should().Be("Run fehlgeschlagen: HTTP 500 von date.nager.at — importiert=2");
    }

    [Fact]
    public async Task Dispose_without_finish_writes_unexpected_termination_warning()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using (var run = await logger.BeginRunAsync(SyncLogServices.EnaioDms))
        {
            // kein Finish-Aufruf
        }

        using var verify = factory.CreateDbContext();
        var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
        entries.Should().HaveCount(2);
        entries[1].Level.Should().Be(SyncLogLevel.Warning);
        entries[1].Message.Should().Contain("unerwartet beendet");
    }

    [Fact]
    public async Task DoubleFinish_is_ignored()
    {
        var factory = new TestApplicationDbContextFactory();
        var logger = new SyncLogger(factory, NullLogger<SyncLogger>.Instance);

        await using (var run = await logger.BeginRunAsync(SyncLogServices.OseonTracking))
        {
            await run.FinishSuccessAsync();
            await run.FinishFailedAsync("doch nicht");  // sollte ignoriert werden
        }

        using var verify = factory.CreateDbContext();
        var entries = await verify.SyncLogs.OrderBy(e => e.Id).ToListAsync();
        // Erwartet: Start + 1x Success-Finish = 2 Eintraege. Kein "Run fehlgeschlagen".
        entries.Should().HaveCount(2);
        entries.Should().NotContain(e => e.Message.StartsWith("Run fehlgeschlagen"));
    }
}
